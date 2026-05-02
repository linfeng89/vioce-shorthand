namespace VoiceDiary.ViewModels;

public partial class DiaryDetailViewModel : BaseViewModel
{
    private readonly IStorageService _storageService;
    private readonly IDatabaseService _databaseService;

    private DiaryEntry? _entry;
    private bool _isPlaying;
    private TimeSpan _currentPosition;
    private TimeSpan _duration;
    private string _editedText;

    public DiaryDetailViewModel(
        IStorageService storageService,
        IDatabaseService databaseService)
    {
        _storageService = storageService;
        _databaseService = databaseService;
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

    public string EditedText
    {
        get => _editedText;
        set => SetProperty(ref _editedText, value);
    }

    public Command PlayPauseCommand => new Command(async () => await PlayPauseAsync());
    public Command SaveTextCommand => new Command(async () => await SaveTextAsync());
    public Command RetranscribeCommand => new Command(async () => await RetranscribeAsync());

    private async Task PlayPauseAsync()
    {
        if (Entry == null)
            return;

        var audioPath = await _storageService.GetAudioFilePathAsync(Entry.AudioFileName);
        if (!File.Exists(audioPath))
        {
            await Shell.Current.DisplayAlert("错误", "音频文件不存在", "确定");
            return;
        }

        IsPlaying = !IsPlaying;
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
}
