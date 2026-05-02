namespace VoiceDiary.ViewModels;

public partial class TrashViewModel : BaseViewModel
{
    private readonly IDatabaseService _databaseService;
    private readonly IStorageService _storageService;

    private ObservableCollection<DiaryEntry> _deletedEntries = new();
    private bool _isBusy;

    public TrashViewModel(
        IDatabaseService databaseService,
        IStorageService storageService)
    {
        _databaseService = databaseService;
        _storageService = storageService;
    }

    public ObservableCollection<DiaryEntry> DeletedEntries
    {
        get => _deletedEntries;
        set => SetProperty(ref _deletedEntries, value);
    }

    public bool IsBusy
    {
        get => _isBusy;
        set => SetProperty(ref _isBusy, value);
    }

    public Command LoadDeletedEntriesCommand => new Command(async () => await LoadDeletedEntriesAsync());
    public Command<DiaryEntry> RecoverEntryCommand => new Command<DiaryEntry>(async (entry) => await RecoverEntryAsync(entry));
    public Command<DiaryEntry> HardDeleteEntryCommand => new Command<DiaryEntry>(async (entry) => await HardDeleteEntryAsync(entry));
    public Command RecoverAllCommand => new Command(async () => await RecoverAllAsync());
    public Command DeleteAllCommand => new Command(async () => await DeleteAllAsync());

    public async Task LoadDeletedEntriesAsync()
    {
        if (IsBusy)
            return;

        try
        {
            IsBusy = true;
            DeletedEntries.Clear();

            var entries = await _databaseService.GetDeletedEntriesAsync();
            foreach (var entry in entries)
            {
                DeletedEntries.Add(entry);
            }
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

    private async Task RecoverEntryAsync(DiaryEntry entry)
    {
        if (entry == null)
            return;

        try
        {
            await _databaseService.RecoverEntryAsync(entry);
            DeletedEntries.Remove(entry);
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("错误", $"恢复失败：{ex.Message}", "确定");
        }
    }

    private async Task HardDeleteEntryAsync(DiaryEntry entry)
    {
        if (entry == null)
            return;

        var confirm = await Shell.Current.DisplayAlert("确认删除", "此操作不可恢复，确定要永久删除吗？", "确定", "取消");
        if (!confirm)
            return;

        try
        {
            var audioPath = Path.Combine(_storageService.AppAudioPath, entry.AudioFileName);
            if (File.Exists(audioPath))
                File.Delete(audioPath);

            await _databaseService.HardDeleteEntryAsync(entry);
            DeletedEntries.Remove(entry);
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("错误", $"删除失败：{ex.Message}", "确定");
        }
    }

    private async Task RecoverAllAsync()
    {
        try
        {
            foreach (var entry in DeletedEntries.ToList())
            {
                await _databaseService.RecoverEntryAsync(entry);
            }
            DeletedEntries.Clear();
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("错误", $"批量恢复失败：{ex.Message}", "确定");
        }
    }

    private async Task DeleteAllAsync()
    {
        var confirm = await Shell.Current.DisplayAlert("确认清空", "回收站所有内容将被永久删除，确定吗？", "确定", "取消");
        if (!confirm)
            return;

        try
        {
            foreach (var entry in DeletedEntries.ToList())
            {
                var audioPath = Path.Combine(_storageService.AppAudioPath, entry.AudioFileName);
                if (File.Exists(audioPath))
                    File.Delete(audioPath);

                await _databaseService.HardDeleteEntryAsync(entry);
            }
            DeletedEntries.Clear();
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("错误", $"批量删除失败：{ex.Message}", "确定");
        }
    }
}
