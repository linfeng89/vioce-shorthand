namespace VoiceDiary.ViewModels;

public partial class DiaryListViewModel : BaseViewModel
{
    private readonly IDatabaseService _databaseService;
    private readonly IStorageService _storageService;

    private ObservableCollection<DiaryEntry> _entries = new();
    private bool _isBusy;
    private bool _hasMore = true;
    private int _currentSkip = 0;
    private const int PageSize = 30;
    private string? _currentMonth;

    public DiaryListViewModel(
        IDatabaseService databaseService,
        IStorageService storageService)
    {
        _databaseService = databaseService;
        _storageService = storageService;
    }

    public ObservableCollection<DiaryEntry> Entries
    {
        get => _entries;
        set => SetProperty(ref _entries, value);
    }

    public bool IsBusy
    {
        get => _isBusy;
        set => SetProperty(ref _isBusy, value);
    }

    public string? CurrentMonth
    {
        get => _currentMonth;
        set => SetProperty(ref _currentMonth, value);
    }

    public Command LoadEntriesCommand => new Command(async () => await LoadEntriesAsync());
    public Command LoadMoreEntriesCommand => new Command(async () => await LoadMoreEntriesAsync());
    public Command<DiaryEntry> NavigateToDetailCommand => new Command<DiaryEntry>(async (entry) => await NavigateToDetail(entry));
    public Command GoToTrashCommand => new Command(async () => await GoToTrashAsync());
    public Command GoToSettingsCommand => new Command(async () => await GoToSettingsAsync());

    public async Task LoadEntriesAsync()
    {
        if (IsBusy)
            return;

        try
        {
            IsBusy = true;
            Entries.Clear();
            _currentSkip = 0;
            _hasMore = true;

            var entries = await _databaseService.GetRecentEntriesAsync(PageSize, _currentSkip);
            var entryList = entries.ToList();

            if (entryList.Count < PageSize)
                _hasMore = false;

            foreach (var entry in entryList)
            {
                Entries.Add(entry);
            }

            _currentSkip += entryList.Count;
            UpdateCurrentMonth();
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("错误", $"加载失败：{ex.Message}", "确定");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task LoadMoreEntriesAsync()
    {
        if (IsBusy || !_hasMore)
            return;

        try
        {
            IsBusy = true;

            var entries = await _databaseService.GetRecentEntriesAsync(PageSize, _currentSkip);
            var entryList = entries.ToList();

            if (entryList.Count < PageSize)
                _hasMore = false;

            foreach (var entry in entryList)
            {
                Entries.Add(entry);
            }

            _currentSkip += entryList.Count;
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("错误", $"加载更多失败：{ex.Message}", "确定");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task NavigateToDetail(DiaryEntry entry)
    {
        if (entry == null)
            return;

        await Shell.Current.Navigation.PushAsync(
            new DiaryDetailPage(
                App.Current.Services.GetRequiredService<DiaryDetailViewModel>(),
                entry));
    }

    private async Task GoToTrashAsync()
    {
        await Shell.Current.NotificationAsync(
            new TrashPage(App.Current.Services.GetRequiredService<TrashViewModel>()));
    }

    private async Task GoToSettingsAsync()
    {
        await Shell.Current.Navigation.PushAsync(
            new SettingsPage(App.Current.Services.GetRequiredService<SettingsViewModel>()));
    }

    private void UpdateCurrentMonth()
    {
        if (Entries.Count == 0)
        {
            CurrentMonth = null;
            return;
        }

        var firstEntry = Entries[0];
        var now = DateTime.Now;
        var entryDate = firstEntry.CreatedAt;

        if (entryDate.Date == now.Date)
            CurrentMonth = "今天";
        else if (entryDate.Date == now.AddDays(-1).Date)
            CurrentMonth = "昨天";
        else if (entryDate >= now.StartOfWeek(DayOfWeek.Monday))
            CurrentMonth = "本周";
        else
            CurrentMonth = $"{entryDate:yyyy 年 M 月}";
    }
}

public static class DateTimeExtensions
{
    public static DateTime StartOfWeek(this DateTime dt, DayOfWeek startOfWeek)
    {
        int diff = (7 + (dt.DayOfWeek - startOfWeek)) % 7;
        return dt.AddDays(-1 * diff).Date;
    }
}
