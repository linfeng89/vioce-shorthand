namespace VoiceDiary.Services;

public class AudioRecorder : IAudioRecorder
{
    private readonly IAudioRecorder _platformRecorder;

    public AudioRecorder()
    {
#if ANDROID
        _platformRecorder = new VoiceDiary.Platforms.Android.AndroidAudioRecorder();
#elif __IOS__
        _platformRecorder = new VoiceDiary.Platforms.iOS.IosAudioRecorder();
#else
        throw new PlatformNotSupportedException("当前平台不支持录音功能");
#endif
    }

    public event EventHandler<RecordingEventArgs>? RecordingStarted
    {
        add => _platformRecorder.RecordingStarted += value;
        remove => _platformRecorder.RecordingStarted -= value;
    }

    public event EventHandler<RecordingEventArgs>? RecordingProgressChanged
    {
        add => _platformRecorder.RecordingProgressChanged += value;
        remove => _platformRecorder.RecordingProgressChanged -= value;
    }

    public event EventHandler<RecordingEventArgs>? RecordingStopped
    {
        add => _platformRecorder.RecordingStopped += value;
        remove => _platformRecorder.RecordingStopped -= value;
    }

    public event EventHandler<RecordingEventArgs>? RecordingCancelled
    {
        add => _platformRecorder.RecordingCancelled += value;
        remove => _platformRecorder.RecordingCancelled -= value;
    }

    public bool IsRecording => _platformRecorder.IsRecording;

    public TimeSpan CurrentDuration => _platformRecorder.CurrentDuration;

    public Task<string> StartRecordingAsync() => _platformRecorder.StartRecordingAsync();

    public Task<(string filePath, int duration)> StopRecordingAsync() => _platformRecorder.StopRecordingAsync();

    public Task CancelRecordingAsync() => _platformRecorder.CancelRecordingAsync();

    public Task<double> GetPeakAmplitudeAsync() => _platformRecorder.GetPeakAmplitudeAsync();
}
