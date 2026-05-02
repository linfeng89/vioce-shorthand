using AVFoundation;
using AudioToolbox;
using Foundation;

namespace VoiceDiary.Platforms.iOS;

public class IosAudioRecorder : IAudioRecorder
{
    private AVAudioRecorder? _recorder;
    private string? _currentFilePath;
    private DateTime _startTime;
    private bool _isRecording;
    private Timer? _timer;
    private NSObject? _interruptionObserver;

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
            var url = NSUrl.FromFilename(_currentFilePath);

            var settings = new AudioSettings
            {
                SampleRate = 16000.0f,
                Channels = 1,
                LinearPcmBitDepth = 16,
                LinearPcmIsBigEndian = false,
                LinearPcmIsFloat = false,
                AudioFormatType = AudioFormatType.LinearPCM
            };

            _recorder = AVAudioRecorder.Create(url, settings, out var error);

            if (error != null)
                throw new Exception($"创建录音器失败：{error.LocalizedDescription}");

            _recorder.PrepareToRecord();
            _recorder.Record();

            RegisterInterruptionObserver();
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
            RemoveInterruptionObserver();

            if (_recorder != null)
            {
                _recorder.Stop();
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
            RemoveInterruptionObserver();

            if (_recorder != null)
            {
                _recorder.Stop();
                _recorder.DeleteRecording();
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
            var level = _recorder?.UpdateMeters();
            if (level == true)
            {
                var averagePower = _recorder.GetAveragePower(0);
                var normalized = Math.Pow(10, averagePower / 20.0);
                return Task.FromResult(Math.Min(normalized * 10, 1.0));
            }
            return Task.FromResult(0.0);
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
                _recorder?.UpdateMeters();
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

    private void RegisterInterruptionObserver()
    {
        _interruptionObserver = NSNotificationCenter.DefaultCenter.AddObserver(
            AVAudioSession.InterruptionNotification,
            notification =>
            {
                var type = notification.UserInfo?.ValueForKey(AVAudioSession.InterruptionTypeKey) as NSNumber;
                if (type?.Int32Value == (int)AVAudioSessionInterruptionType.Began)
                {
                    // 音频中断开始（如来电）
                    Console.WriteLine("录音被中断");
                }
                else if (type?.Int32Value == (int)AVAudioSessionInterruptionType.Ended)
                {
                    // 音频中断结束
                    Console.WriteLine("中断结束，可恢复录音");
                }
            });
    }

    private void RemoveInterruptionObserver()
    {
        if (_interruptionObserver != null)
        {
            NSNotificationCenter.DefaultCenter.RemoveObserver(_interruptionObserver);
            _interruptionObserver = null;
        }
    }

    private void Release()
    {
        StopTimer();
        RemoveInterruptionObserver();

        if (_recorder != null)
        {
            try
            {
                _recorder.Dispose();
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
