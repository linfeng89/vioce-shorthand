namespace VoiceDiary.ViewModels;

public partial class SettingsViewModel : BaseViewModel
{
    private readonly IBackupService _backupService;
    private readonly IStorageService _storageService;
    private readonly IDatabaseService _databaseService;

    private bool _biometricLockEnabled;
    private string _autoLockTime = "1 分钟";
    private string _storageInfo = "计算中...";
    private bool _desktopWidgetEnabled = true;
    private bool _headphoneTriggerEnabled;

    public SettingsViewModel(
        IBackupService backupService,
        IStorageService storageService,
        IDatabaseService databaseService)
    {
        _backupService = backupService;
        _storageService = storageService;
        _databaseService = databaseService;

        _ = LoadStorageInfoAsync();
    }

    public bool BiometricLockEnabled
    {
        get => _biometricLockEnabled;
        set => SetProperty(ref _biometricLockEnabled, value);
    }

    public string AutoLockTime
    {
        get => _autoLockTime;
        set => SetProperty(ref _autoLockTime, value);
    }

    public string StorageInfo
    {
        get => _storageInfo;
        set => SetProperty(ref _storageInfo, value);
    }

    public bool DesktopWidgetEnabled
    {
        get => _desktopWidgetEnabled;
        set => SetProperty(ref _desktopWidgetEnabled, value);
    }

    public bool HeadphoneTriggerEnabled
    {
        get => _headphoneTriggerEnabled;
        set => SetProperty(ref _headphoneTriggerEnabled, value);
    }

    public Command BackupCommand => new Command(async () => await CreateBackupAsync());
    public Command RestoreBackupCommand => new Command(async () => await RestoreBackupAsync());
    public Command LoadStorageInfoCommand => new Command(async () => await LoadStorageInfoAsync());

    private async Task CreateBackupAsync()
    {
        try
        {
            var (success, message) = await _backupService.CreateBackupAsync();
            await Shell.Current.DisplayAlert("备份", message ?? "备份成功", "确定");
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("备份失败", ex.Message, "确定");
        }
    }

    private async Task RestoreBackupAsync()
    {
        var confirm = await Shell.Current.DisplayAlert("确认恢复", "当前数据将被替换，是否继续？", "确定", "取消");
        if (!confirm)
            return;

        try
        {
            var (success, message) = await _backupService.RestoreBackupAsync();
            await Shell.Current.DisplayAlert("恢复", message ?? "恢复成功", "确定");
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("恢复失败", ex.Message, "确定");
        }
    }

    private async Task LoadStorageInfoAsync()
    {
        try
        {
            var size = await _storageService.GetAppStorageSizeAsync();
            var available = await _storageService.GetAvailableSpaceAsync();
            StorageInfo = $"{FormatSize(size)} / 可用 {FormatSize(available)}";
        }
        catch (Exception)
        {
            StorageInfo = "无法获取存储信息";
        }
    }

    private static string FormatSize(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB" };
        int order = 0;
        double size = bytes;
        while (size >= 1024 && order < sizes.Length - 1)
        {
            order++;
            size /= 1024;
        }
        return $"{size:0.##} {sizes[order]}";
    }
}
