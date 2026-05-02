using AVFoundation;
using Foundation;

namespace VoiceDiary.Platforms.iOS;

public class IosAudioPlayer : IAudioPlayer
{
    private AVAudioPlayer? _player;
    private string? _filePath;
    private NSObject? _observer;
    private Timer? _timer;
    private bool _isPlaying;

    public event EventHandler<AudioPlaybackEventArgs>? PlaybackStarted;
    public event EventHandler<AudioPlaybackEventArgs>? PlaybackProgressChanged;
    public event EventHandler<AudioPlaybackEventArgs>? PlaybackCompleted;
    public event EventHandler<AudioPlaybackEventArgs>? PlaybackStopped;

    public bool IsPlaying => _isPlaying && _player?.Playing == true;

    public TimeSpan CurrentPosition => _player?.CurrentTime != null 
        ? TimeSpan.FromSeconds(_player.CurrentTime) 
        : TimeSpan.Zero;

    public TimeSpan Duration => _player?.Duration != null 
        ? TimeSpan.FromSeconds(_player.Duration) 
        : TimeSpan.Zero;

    public Task LoadAsync(string filePath)
    {
        return Task.Run(() =>
        {
            try
            {
                StopTimer();

                if (_player != null)
                {
                    _player.Stop();
                    _player.Dispose();
                    _player = null;
                }

                _filePath = filePath;
                var url = NSUrl.FromFilename(filePath);
                _player = AVAudioPlayer.FromUrl(url);

                if (_player == null)
                    throw new Exception("无法加载音频文件");

                _player.FinishedPlaying += (s, e) =>
                {
                    _isPlaying = false;
                    StopTimer();

                    PlaybackCompleted?.Invoke(this, new AudioPlaybackEventArgs
                    {
                        CurrentPosition = Duration,
                        Duration = Duration
                    });
                };

                _player.PrepareToPlay();
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

        _player.Play();
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
        if (_player?.Playing == true)
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
            _player.CurrentTime = 0;
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
            _player.CurrentTime = position.TotalSeconds;
        }

        return Task.CompletedTask;
    }

    public void Dispose()
    {
        StopTimer();

        if (_player != null)
        {
            _player.Stop();
            _player.Dispose();
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
