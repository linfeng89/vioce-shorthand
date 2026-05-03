namespace VoiceDiary.ViewModels;

public partial class DiaryDetailViewModel : BaseViewModel
{
    private readonly IStorageService _storageService;
    private readonly IDatabaseService _databaseService;
    private readonly IAudioPlayer _audioPlayer;
    private readonly ITrashService _trashService;
    private readonly IToastService _toastService;

    private DiaryEntry? _entry;
    private bool _isPlaying;
    private TimeSpan _currentPosition;
    private TimeSpan _duration;
    private bool _isEditing;
    private string _originalText;

    public DiaryDetailViewModel(
        IStorageService storageService,
        IDatabaseService databaseService,
        IAudioPlayer audioPlayer,
        ITrashService trashService,
        IToastService toastService)
    {
        _storageService = storageService;
        _databaseService = databaseService;
        _audioPlayer = audioPlayer;
        _trashService = trashService;
        _toastService = toastService;

        _audioPlayer.PlaybackProgressChanged += OnPlaybackProgressChanged;
        _audioPlayer.PlaybackCompleted += OnPlaybackCompleted;
        _audioPlayer.PlaybackStopped += OnPlaybackStopped;
        
        _originalText = string.Empty;
    }

    ~DiaryDetailViewModel()
    {
        _audioPlayer.PlaybackProgressChanged -= OnPlaybackProgressChanged;
        _audioPlayer.PlaybackCompleted -= OnPlaybackCompleted;
        _audioPlayer.PlaybackStopped -= OnPlaybackStopped;
    }

    public DiaryEntry? Entry
    {
        get => _entry;
        set => SetProperty(ref _entry, value);
    }

    public bool IsPlaying
    {
        get => _isPlaying;
        set => SetProperty(ref _isPlaying, value);
    }

    public TimeSpan CurrentPosition
    {
        get => _currentPosition;
        set => SetProperty(ref _currentPosition, value);
    }

    public TimeSpan Duration
    {
        get => _duration;
        set => SetProperty(ref _duration, value);
    }
    
    public bool IsEditing
    {
        get => _isEditing;
        set => SetProperty(ref _isEditing, value);
    }
    
    public bool IsNotEditing => !_isEditing;
    
    public string TranscribedText
    {
        get => Entry?.TranscribedText ?? string.Empty;
        set
        {
            if (Entry != null)
            {
                Entry.TranscribedText = value;
                OnPropertyChanged();
            }
        }
    }
    
    public string PlayPauseIcon => IsPlaying ? "⏸️" : "▶️";
    
    public string EditIcon => IsEditing ? "✏️" : "✏️";

    public Command PlayPauseCommand => new Command(async () => await PlayPauseAsync());
    public Command SaveTextCommand => new Command(async () => await SaveTextAsync());
    public Command RetranscribeCommand => new Command(async () => await RetranscribeAsync());
    public Command ToggleEditCommand => new Command(() => ToggleEdit());
    public Command CancelEditCommand => new Command(() => CancelEdit());
    public Command SaveEditCommand => new Command(async () => await SaveEditAsync());
    public Command DeleteCommand => new Command(async () => await DeleteAsync());
    public Command SeekCommand => new Command<double>(async (position) => await SeekAsync(position));

    private async Task PlayPauseAsync()
    {
        if (Entry == null)
            return;

        try
        {
            var audioPath = await _storageService.GetAudioFilePathAsync(Entry.AudioFileName);
            if (!File.Exists(audioPath))
            {
                await Shell.Current.DisplayAlert("错误", "音频文件不存在", "确定");
                return;
            }

            if (IsPlaying)
            {
                await _audioPlayer.PauseAsync();
                IsPlaying = false;
            }
            else
            {
                await _audioPlayer.LoadAsync(audioPath);
                await _audioPlayer.PlayAsync();
                IsPlaying = true;
                Duration = _audioPlayer.Duration;
            }
            
            OnPropertyChanged(nameof(PlayPauseIcon));
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("错误", $"播放失败：{ex.Message}", "确定");
        }
    }

    private void OnPlaybackProgressChanged(object? sender, AudioPlaybackEventArgs e)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            CurrentPosition = e.CurrentPosition;
            OnPropertyChanged(nameof(CurrentPosition));
        });
    }

    private void OnPlaybackCompleted(object? sender, AudioPlaybackEventArgs e)
    {
        IsPlaying = false;
        CurrentPosition = TimeSpan.Zero;
    }

    private void OnPlaybackStopped(object? sender, AudioPlaybackEventArgs e)
    {
        IsPlaying = false;
    }

    private async Task SaveTextAsync()
    {
        if (Entry == null)
            return;

        Entry.TranscribedText = EditedText;
        await _databaseService.SaveEntryAsync(Entry);

        await Shell.Current.DisplayAlert("成功", "修改已保存", "确定");
    }

    private async Task RetranscribeAsync()
    {
        if (Entry == null)
            return;

        var audioPath = await _storageService.GetAudioFilePathAsync(Entry.AudioFileName);
        if (!File.Exists(audioPath))
        {
            await Shell.Current.DisplayAlert("错误", "音频文件不存在", "确定");
            return;
        }

        await Shell.Current.DisplayAlert("提示", "已加入转写队列", "确定");
    }
    
    private void ToggleEdit()
    {
        if (IsEditing)
        {
            // 当前是编辑模式，切换到查看模式
            IsEditing = false;
        }
        else
        {
            // 切换到编辑模式，保存原文本
            _originalText = Entry?.TranscribedText ?? string.Empty;
            IsEditing = true;
        }
        
        OnPropertyChanged(nameof(IsNotEditing));
    }
    
    private void CancelEdit()
    {
        if (Entry != null)
        {
            Entry.TranscribedText = _originalText;
        }
        IsEditing = false;
        OnPropertyChanged(nameof(IsNotEditing));
        OnPropertyChanged(nameof(Entry.TranscribedText));
    }
    
    private async Task SaveEditAsync()
    {
        if (Entry == null)
            return;
        
        try
        {
            // 保存到数据库
            await _databaseService.UpdateEntryAsync(Entry);
            
            // 更新 FTS 索引
            if (Entry.IsTranscribed && !string.IsNullOrEmpty(Entry.TranscribedText))
            {
                var searchService = App.Services.GetRequiredService<ISearchService>();
                await searchService.AddToIndexAsync(Entry);
            }
            
            IsEditing = false;
            OnPropertyChanged(nameof(IsNotEditing));
            
            await _toastService.Show("保存成功", 2000);
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("保存失败", ex.Message, "确定");
        }
    }
    
    private async Task DeleteAsync()
    {
        if (Entry == null)
            return;
        
        var confirm = await Shell.Current.DisplayAlert(
            "确认删除",
            "确定要删除这篇日记吗？",
            "删除",
            "取消");
        
        if (!confirm)
            return;
        
        try
        {
            // 移动到回收站
            await _trashService.MoveToTrashAsync(Entry);
            
            // 显示撤销 Toast
            var result = await _toastService.ShowAsync("已删除", "撤销", TimeSpan.FromSeconds(3));
            
            if (result == "action")
            {
                // 用户点击撤销
                await _trashService.RestoreFromTrashAsync(Entry.Id);
                await _toastService.Show("已恢复", 2000);
            }
            else
            {
                // 返回到列表页
                await Shell.Current.Navigation.PopAsync();
            }
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("删除失败", ex.Message, "确定");
        }
    }
    
    private async Task SeekAsync(double position)
    {
        if (Entry == null)
            return;
        
        try
        {
            var audioPath = await _storageService.GetAudioFilePathAsync(Entry.AudioFileName);
            if (!File.Exists(audioPath))
                return;
            
            await _audioPlayer.SeekToAsync(TimeSpan.FromSeconds(position));
            CurrentPosition = TimeSpan.FromSeconds(position);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Seek error: {ex}");
        }
    }
}
