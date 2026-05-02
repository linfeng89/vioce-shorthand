namespace VoiceDiary.Services;

public interface IAudioPlayer
{
    event EventHandler<AudioPlaybackEventArgs>? PlaybackStarted;
    event EventHandler<AudioPlaybackEventArgs>? PlaybackProgressChanged;
    event EventHandler<AudioPlaybackEventArgs>? PlaybackCompleted;
    event EventHandler<AudioPlaybackEventArgs>? PlaybackStopped;

    bool IsPlaying { get; }
    TimeSpan CurrentPosition { get; }
    TimeSpan Duration { get; }

    Task LoadAsync(string filePath);
    Task PlayAsync();
    Task PauseAsync();
    Task StopAsync();
    Task SeekAsync(TimeSpan position);
    void Dispose();
}

public class AudioPlaybackEventArgs : EventArgs
{
    public TimeSpan CurrentPosition { get; set; }
    public TimeSpan Duration { get; set; }
    public string? ErrorMessage { get; set; }
}
