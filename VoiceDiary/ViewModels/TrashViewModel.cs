namespace VoiceDiary.ViewModels;

public partial class TrashViewModel : BaseViewModel
{
    private readonly ITrashService _trashService;
    private readonly IToastService _toastService;

    private ObservableCollection<DeletedEntry> _trashEntries = new();
    private bool _isEmpty = true;
    private bool _hasEntries;

    public TrashViewModel(ITrashService trashService, IToastService toastService)
    {
        _trashService = trashService;
        _toastService = toastService;
    }

    public ObservableCollection<DeletedEntry> TrashEntries
    {
        get => _trashEntries;
        set => SetProperty(ref _trashEntries, value);
    }

    public bool IsEmpty
    {
        get => _isEmpty;
        set => SetProperty(ref _isEmpty, value);
    }

    public bool HasEntries
    {
        get => _hasEntries;
        set => SetProperty(ref _hasEntries, value);
    }

    public Command LoadTrashEntriesCommand => new Command(async () => await LoadTrashEntriesAsync());
    public Command<long> RestoreEntryCommand => new Command<long>(async (entryId) => await RestoreEntryAsync(entryId));
    public Command<long> PermanentlyDeleteCommand => new Command<long>(async (entryId) => await PermanentlyDeleteAsync(entryId));
    public Command ClearTrashCommand => new Command(async () => await ClearTrashAsync());

    private async Task LoadTrashEntriesAsync()
    {
        if (IsBusy)
            return;

        try
        {
            IsBusy = true;
            TrashEntries.Clear();

            var entries = await _trashService.GetTrashEntriesAsync(30);
            
            foreach (var entry in entries)
            {
                TrashEntries.Add(entry);
            }

            IsEmpty = entries.Count == 0;
            HasEntries = entries.Count > 0;
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

    private async Task RestoreEntryAsync(long entryId)
    {
        try
        {
            await _trashService.RestoreFromTrashAsync(entryId);
            
            // 从列表移除
            var entry = TrashEntries.FirstOrDefault(e => e.EntryId == entryId);
            if (entry != null)
            {
                TrashEntries.Remove(entry);
                IsEmpty = TrashEntries.Count == 0;
                HasEntries = TrashEntries.Count > 0;
            }

            await _toastService.Show("已恢复", 2000);
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("恢复失败", ex.Message, "确定");
        }
    }

    private async Task PermanentlyDeleteAsync(long entryId)
    {
        try
        {
            await _trashService.PermanentlyDeleteAsync(entryId);
            
            // 从列表移除
            var entry = TrashEntries.FirstOrDefault(e => e.EntryId == entryId);
            if (entry != null)
            {
                TrashEntries.Remove(entry);
                IsEmpty = TrashEntries.Count == 0;
                HasEntries = TrashEntries.Count > 0;
            }

            await _toastService.Show("已永久删除", 2000);
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("删除失败", ex.Message, "确定");
        }
    }

    private async Task ClearTrashAsync()
    {
        var confirm = await Shell.Current.DisplayAlert(
            "确认清空回收站", 
            "此操作将永久删除回收站中的所有内容，确定继续吗？", 
            "清空", 
            "取消");
        
        if (!confirm)
            return;

        try
        {
            IsBusy = true;
            
            var entries = TrashEntries.ToList();
            foreach (var entry in entries)
            {
                await _trashService.PermanentlyDeleteAsync(entry.EntryId);
            }
            
            TrashEntries.Clear();
            IsEmpty = true;
            HasEntries = false;

            await _toastService.Show("回收站已清空", 2000);
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("错误", $"清空失败：{ex.Message}", "确定");
        }
        finally
        {
            IsBusy = false;
        }
    }
}
