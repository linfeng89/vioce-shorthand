namespace VoiceDiary.Services;

public class AudioRecorder : IAudioRecorder
{
    private bool _isRecording;
    private DateTime _startTime;
    private string? _currentFilePath;

    public event EventHandler<RecordingEventArgs>? RecordingStarted;
    public event EventHandler<RecordingEventArgs>? RecordingProgressChanged;
    public event EventHandler<RecordingEventArgs>? RecordingStopped;
    public event EventHandler<RecordingEventArgs>? RecordingCancelled;

    public bool IsRecording => _isRecording;

    public TimeSpan CurrentDuration => _isRecording ? DateTime.Now - _startTime : TimeSpan.Zero;

    public Task<string> StartRecordingAsync()
    {
        if (_isRecording)
            throw new InvalidOperationException("Recording already in progress");

        _startTime = DateTime.Now;
        _isRecording = true;
        _currentFilePath = Path.Combine(FileSystem.CacheDirectory, $"recording_{DateTime.Now:yyyyMMdd_HHmmss}.wav");

        Task.Run(() => RecordAudio(_currentFilePath));

        RecordingStarted?.Invoke(this, new RecordingEventArgs
        {
            FilePath = _currentFilePath
        });

        return Task.FromResult(_currentFilePath);
    }

    public Task<(string filePath, int duration)> StopRecordingAsync()
    {
        if (!_isRecording)
            throw new InvalidOperationException("No recording in progress");

        _isRecording = false;
        var duration = (int)(DateTime.Now - _startTime).TotalSeconds;

        RecordingStopped?.Invoke(this, new RecordingEventArgs
        {
            FilePath = _currentFilePath,
            Duration = TimeSpan.FromSeconds(duration)
        });

        return Task.FromResult((_currentFilePath!, duration));
    }

    public Task CancelRecordingAsync()
    {
        if (!_isRecording)
            return Task.CompletedTask;

        _isRecording = false;
        
        if (_currentFilePath != null && File.Exists(_currentFilePath))
        {
            File.Delete(_currentFilePath);
        }

        RecordingCancelled?.Invoke(this, new RecordingEventArgs());
        return Task.CompletedTask;
    }

    public Task<double> GetPeakAmplitudeAsync()
    {
        return Task.FromResult(0.0);
    }

    private async Task RecordAudio(string filePath)
    {
        try
        {
            var recording = await AudioRecorderPlatform.Current.CreateAsync();
            if (recording == null)
            {
                RecordingCancelled?.Invoke(this, new RecordingEventArgs
                {
                    ErrorMessage = "无法访问录音设备"
                });
                return;
            }

            recording.Start();

            await Task.Run(async () =>
            {
                while (_isRecording)
                {
                    await Task.Delay(100);
                    RecordingProgressChanged?.Invoke(this, new RecordingEventArgs
                    {
                        Duration = DateTime.Now - _startTime
                    });
                }
            });

            recording.Stop();
        }
        catch (Exception ex)
        {
            _isRecording = false;
            RecordingCancelled?.Invoke(this, new RecordingEventArgs
            {
                ErrorMessage = ex.Message
            });
        }
    }
}

public static class AudioRecorderPlatform
{
    private static readonly Lazy<Func<IRecording?>> _implementation = new(() => CreateFunc());

    public static IRecording? CreateAsync() => _implementation.Value();

    private static Func<IRecording?> CreateFunc()
    {
#if __IOS__ || MACCATALYST
        return IosRecording.Create;
#elif ANDROID
        return AndroidRecording.Create;
#else
        return () => null;
#endif
    }
}

public interface IRecording
{
    void Start();
    void Stop();
}

#if __IOS__ || MACCATALYST
public class IosRecording : IRecording
{
    private AVFoundation.AVAudioRecorder? _recorder;

    public static IosRecording? Create() => new IosRecording();

    public void Start()
    {
        // iOS 录音实现
    }

    public void Stop()
    {
        _recorder?.StopRecording();
    }
}
#elif ANDROID
public class AndroidRecording : IRecording
{
    private Android.Media.MediaRecorder? _recorder;

    public static AndroidRecording? Create() => new AndroidRecording();

    public void Start()
    {
        // Android 录音实现
    }

    public void Stop()
    {
        _recorder?.Stop();
    }
}
#endif
