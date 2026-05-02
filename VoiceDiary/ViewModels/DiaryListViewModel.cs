namespace VoiceDiary.ViewModels;

public partial class DiaryListViewModel : BaseViewModel
{
    private readonly IDatabaseService _databaseService;
    private readonly IStorageService _storageService;
    private readonly IToastService _toastService;
    private readonly ITrashService _trashService;

    private ObservableCollection<DiaryEntry> _entries = new();
    private bool _isBusy;
    private bool _hasMore = true;
    private int _currentSkip = 0;
    private const int PageSize = 30;
    private string? _currentMonth;

    public DiaryListViewModel(
        IDatabaseService databaseService,
        IStorageService storageService,
        IToastService toastService,
        ITrashService trashService)
    {
        _databaseService = databaseService;
        _storageService = storageService;
        _toastService = toastService;
        _trashService = trashService;
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
    
    public async Task DeleteEntryWithUndoAsync(DiaryEntry entry)
    {
        try
        {
            // 移动到回收站
            await _trashService.MoveToTrashAsync(entry);
            
            // 从 UI 移除
            Entries.Remove(entry);
            UpdateCurrentMonth();
            
            // 显示 Toast 带撤销
            var result = await _toastService.ShowAsync("已删除", "撤销", TimeSpan.FromSeconds(3));
            
            if (result == "action")
            {
                // 用户点击撤销
                await _trashService.RestoreFromTrashAsync(entry.Id);
                
                // 重新添加到列表（按时间顺序）
                Entries.Insert(0, entry);
                UpdateCurrentMonth();
                
                await _toastService.Show("已恢复", 2000);
            }
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("删除失败", ex.Message, "确定");
        }
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
