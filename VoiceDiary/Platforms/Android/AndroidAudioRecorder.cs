using Android.Media;
using Java.IO;

namespace VoiceDiary.Platforms.Android;

public class AndroidAudioRecorder : IAudioRecorder
{
    private MediaRecorder? _recorder;
    private string? _currentFilePath;
    private DateTime _startTime;
    private bool _isRecording;
    private Timer? _timer;

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

        var fileName = $"recording_{DateTime.Now:yyyyMMdd_HHmmss}.wav";
        _currentFilePath = Path.Combine(FileSystem.CacheDirectory, fileName);

        try
        {
            _recorder = new MediaRecorder();
            _recorder.SetAudioSource(AudioSource.Mic);
            _recorder.SetOutputFormat(OutputFormat.Wave);
            _recorder.SetAudioEncoder(AudioEncoder.Pcm16bit);
            _recorder.SetAudioSamplingRate(16000);
            _recorder.SetAudioEncodingBitRate(256000);
            _recorder.SetOutputFile(_currentFilePath);
            _recorder.Prepare();
            _recorder.Start();

            StartTimer();

            RecordingStarted?.Invoke(this, new RecordingEventArgs
            {
                FilePath = _currentFilePath,
                Duration = TimeSpan.Zero
            });

            return Task.FromResult(_currentFilePath);
        }
        catch (Exception ex)
        {
            _isRecording = false;
            RecordingCancelled?.Invoke(this, new RecordingEventArgs
            {
                ErrorMessage = ex.Message
            });
            throw;
        }
    }

    public Task<(string filePath, int duration)> StopRecordingAsync()
    {
        if (!_isRecording)
            throw new InvalidOperationException("No recording in progress");

        try
        {
            StopTimer();

            if (_recorder != null)
            {
                _recorder.Stop();
                _recorder.Release();
                _recorder = null;
            }

            var duration = (int)(DateTime.Now - _startTime).TotalSeconds;
            var filePath = _currentFilePath;

            _isRecording = false;

            RecordingStopped?.Invoke(this, new RecordingEventArgs
            {
                FilePath = filePath,
                Duration = TimeSpan.FromSeconds(duration)
            });

            return Task.FromResult((filePath, duration));
        }
        catch (Exception ex)
        {
            Release();
            throw new InvalidOperationException($"停止录音失败：{ex.Message}", ex);
        }
    }

    public Task CancelRecordingAsync()
    {
        if (!_isRecording)
            return Task.CompletedTask;

        try
        {
            StopTimer();

            if (_recorder != null)
            {
                _recorder.Stop();
                _recorder.Release();
                _recorder = null;
            }

            if (!string.IsNullOrEmpty(_currentFilePath) && File.Exists(_currentFilePath))
            {
                File.Delete(_currentFilePath);
            }

            _isRecording = false;

            RecordingCancelled?.Invoke(this, new RecordingEventArgs());
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            Release();
            throw new InvalidOperationException($"取消录音失败：{ex.Message}", ex);
        }
    }

    public Task<double> GetPeakAmplitudeAsync()
    {
        try
        {
            var amplitude = _recorder?.MaxAmplitude ?? 0;
            var normalized = amplitude > 0 ? amplitude / 32768.0 : 0.0;
            return Task.FromResult(normalized);
        }
        catch
        {
            return Task.FromResult(0.0);
        }
    }

    private void StartTimer()
    {
        _timer = new Timer(_ =>
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                RecordingProgressChanged?.Invoke(this, new RecordingEventArgs
                {
                    Duration = DateTime.Now - _startTime
                });
            });
        }, null, TimeSpan.Zero, TimeSpan.FromMilliseconds(100));
    }

    private void StopTimer()
    {
        _timer?.Dispose();
        _timer = null;
    }

    private void Release()
    {
        StopTimer();

        if (_recorder != null)
        {
            try
            {
                _recorder.Release();
            }
            catch { }
            _recorder = null;
        }

        _isRecording = false;
    }

    public void Dispose()
    {
        Release();
    }
}
