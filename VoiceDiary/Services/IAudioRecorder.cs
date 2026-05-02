namespace VoiceDiary.Services;

public interface IAudioRecorder
{
    event EventHandler<RecordingEventArgs>? RecordingStarted;
    event EventHandler<RecordingEventArgs>? RecordingProgressChanged;
    event EventHandler<RecordingEventArgs>? RecordingStopped;
    event EventHandler<RecordingEventArgs>? RecordingCancelled;

    bool IsRecording { get; }
    TimeSpan CurrentDuration { get; }

    Task<string> StartRecordingAsync();
    Task<(string filePath, int duration)> StopRecordingAsync();
    Task CancelRecordingAsync();
    Task<double> GetPeakAmplitudeAsync();
}

public class RecordingEventArgs : EventArgs
{
    public string? FilePath { get; set; }
    public TimeSpan Duration { get; set; }
    public string? ErrorMessage { get; set; }
}
