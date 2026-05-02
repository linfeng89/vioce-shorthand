using Android.Media;
using Java.IO;

namespace VoiceDiary.Platforms.Android;

public class AndroidAudioPlayer : IAudioPlayer, MediaPlayer.IOnCompletionListener, MediaPlayer.IOnErrorListener
{
    private MediaPlayer? _player;
    private string? _filePath;
    private Timer? _timer;
    private bool _isPlaying;

    public event EventHandler<AudioPlaybackEventArgs>? PlaybackStarted;
    public event EventHandler<AudioPlaybackEventArgs>? PlaybackProgressChanged;
    public event EventHandler<AudioPlaybackEventArgs>? PlaybackCompleted;
    public event EventHandler<AudioPlaybackEventArgs>? PlaybackStopped;

    public bool IsPlaying => _isPlaying && _player?.IsPlaying == true;

    public TimeSpan CurrentPosition => _player?.CurrentPosition != null 
        ? TimeSpan.FromMilliseconds(_player.CurrentPosition) 
        : TimeSpan.Zero;

    public TimeSpan Duration => _player?.Duration != null 
        ? TimeSpan.FromMilliseconds(_player.Duration) 
        : TimeSpan.Zero;

    public Task LoadAsync(string filePath)
    {
        return Task.Run(() =>
        {
            try
            {
                if (_player != null)
                {
                    _player.Stop();
                    _player.Release();
                    _player = null;
                }

                _filePath = filePath;
                _player = new MediaPlayer();
                _player.SetDataSource(filePath);
                _player.Prepare();
                _player.SetOnCompletionListener(this);
                _player.SetOnErrorListener(this);
            }
            catch (Exception ex)
            {
                PlaybackStopped?.Invoke(this, new AudioPlaybackEventArgs
                {
                    ErrorMessage = ex.Message
                });
                throw;
            }
        });
    }

    public Task PlayAsync()
    {
        if (_player == null)
            throw new InvalidOperationException("请先加载音频文件");

        _player.Start();
        _isPlaying = true;
        StartTimer();

        PlaybackStarted?.Invoke(this, new AudioPlaybackEventArgs
        {
            CurrentPosition = CurrentPosition,
            Duration = Duration
        });

        return Task.CompletedTask;
    }

    public Task PauseAsync()
    {
        if (_player?.IsPlaying == true)
        {
            _player.Pause();
            _isPlaying = false;
            StopTimer();
        }

        return Task.CompletedTask;
    }

    public Task StopAsync()
    {
        if (_player != null)
        {
            _player.Stop();
            _player.SeekTo(0);
            _isPlaying = false;
            StopTimer();

            PlaybackStopped?.Invoke(this, new AudioPlaybackEventArgs
            {
                CurrentPosition = TimeSpan.Zero,
                Duration = Duration
            });
        }

        return Task.CompletedTask;
    }

    public Task SeekAsync(TimeSpan position)
    {
        if (_player != null)
        {
            _player.SeekTo((int)position.TotalMilliseconds);
        }

        return Task.CompletedTask;
    }

    public void OnCompletion(MediaPlayer? mp)
    {
        _isPlaying = false;
        StopTimer();

        PlaybackCompleted?.Invoke(this, new AudioPlaybackEventArgs
        {
            CurrentPosition = Duration,
            Duration = Duration
        });
    }

    public bool OnError(MediaPlayer mp, MediaError what, int extra)
    {
        _isPlaying = false;
        StopTimer();

        PlaybackStopped?.Invoke(this, new AudioPlaybackEventArgs
        {
            ErrorMessage = $"播放错误：{what}, extra: {extra}"
        });

        return true;
    }

    public void Dispose()
    {
        StopTimer();

        if (_player != null)
        {
            _player.Stop();
            _player.Release();
            _player = null;
        }

        _isPlaying = false;
    }

    private void StartTimer()
    {
        _timer = new Timer(_ =>
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                PlaybackProgressChanged?.Invoke(this, new AudioPlaybackEventArgs
                {
                    CurrentPosition = CurrentPosition,
                    Duration = Duration
                });
            });
        }, null, TimeSpan.Zero, TimeSpan.FromMilliseconds(500));
    }

    private void StopTimer()
    {
        _timer?.Dispose();
        _timer = null;
    }
}
