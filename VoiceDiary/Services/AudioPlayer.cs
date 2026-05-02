namespace VoiceDiary.Services;

public class AudioPlayer : IAudioPlayer
{
    private readonly IAudioPlayer _platformPlayer;

    public AudioPlayer()
    {
#if ANDROID
        _platformPlayer = new VoiceDiary.Platforms.Android.AndroidAudioPlayer();
#elif __IOS__
        _platformPlayer = new VoiceDiary.Platforms.iOS.IosAudioPlayer();
#else
        throw new PlatformNotSupportedException("当前平台不支持音频播放");
#endif
    }

    public event EventHandler<AudioPlaybackEventArgs>? PlaybackStarted
    {
        add => _platformPlayer.PlaybackStarted += value;
        remove => _platformPlayer.PlaybackStarted -= value;
    }

    public event EventHandler<AudioPlaybackEventArgs>? PlaybackProgressChanged
    {
        add => _platformPlayer.PlaybackProgressChanged += value;
        remove => _platformPlayer.PlaybackProgressChanged -= value;
    }

    public event EventHandler<AudioPlaybackEventArgs>? PlaybackCompleted
    {
        add => _platformPlayer.PlaybackCompleted += value;
        remove => _platformPlayer.PlaybackCompleted -= value;
    }

    public event EventHandler<AudioPlaybackEventArgs>? PlaybackStopped
    {
        add => _platformPlayer.PlaybackStopped += value;
        remove => _platformPlayer.PlaybackStopped -= value;
    }

    public bool IsPlaying => _platformPlayer.IsPlaying;
    public TimeSpan CurrentPosition => _platformPlayer.CurrentPosition;
    public TimeSpan Duration => _platformPlayer.Duration;

    public Task LoadAsync(string filePath) => _platformPlayer.LoadAsync(filePath);
    public Task PlayAsync() => _platformPlayer.PlayAsync();
    public Task PauseAsync() => _platformPlayer.PauseAsync();
    public Task StopAsync() => _platformPlayer.StopAsync();
    public Task SeekAsync(TimeSpan position) => _platformPlayer.SeekAsync(position);
    public void Dispose() => _platformPlayer.Dispose();
}
