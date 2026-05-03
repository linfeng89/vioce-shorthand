using System.Collections.ObjectModel;

namespace VoiceDiary.ViewModels;

public partial class BackupSettingsViewModel : BaseViewModel
{
    private readonly IBackupService _backupService;
    private readonly IToastService _toastService;
    
    [ObservableProperty]
    private bool _isCreatingBackup;
    
    [ObservableProperty]
    private bool _hasBackup;
    
    [ObservableProperty]
    private DateTime? _lastBackupDate;
    
    [ObservableProperty]
    private string _backupFileSize = "未知";
    
    [ObservableProperty]
    private int _backupCount;
    
    [ObservableProperty]
    private bool _autoBackupEnabled;
    
    [ObservableProperty]
    private string _selectedBackupInterval = "每周";
    
    [ObservableProperty]
    private bool _cloudBackupEnabled;

    public BackupSettingsViewModel(
        IBackupService backupService,
        IToastService toastService)
    {
        _backupService = backupService;
        _toastService = toastService;
        
        LoadBackupInfoCommand = new Command(async () => await LoadBackupInfoAsync());
        CreateBackupCommand = new Command(async () => await CreateBackupAsync());
        RestoreBackupCommand = new Command(async () => await RestoreBackupAsync());
        CleanupOldBackupsCommand = new Command(async () => await CleanupOldBackupsAsync());
    }

    public Command LoadBackupInfoCommand { get; }
    public Command CreateBackupCommand { get; }
    public Command RestoreBackupCommand { get; }
    public Command CleanupOldBackupsCommand { get; }

    protected override async void OnInitialized()
    {
        base.OnInitialized();
        await LoadBackupInfoAsync();
        LoadAutoBackupSettings();
    }

    private async Task LoadBackupInfoAsync()
    {
        try
        {
            IsBusy = true;
            
            var backups = await _backupService.ListAvailableBackupsAsync();
            BackupCount = backups.Count;
            
            if (backups.Any())
            {
                HasBackup = true;
                var latestBackupInfo = new FileInfo(backups.First());
                LastBackupDate = latestBackupInfo.LastWriteTime;
                BackupFileSize = FormatFileSize(latestBackupInfo.Length);
            }
            else
            {
                HasBackup = false;
                LastBackupDate = null;
                BackupFileSize = "未知";
            }
        }
        catch (Exception ex)
        {
            await _toastService.Show($"加载备份信息失败：{ex.Message}", 3000);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task CreateBackupAsync()
    {
        try
        {
            IsCreatingBackup = true;
            
            var backupPath = await _backupService.CreateFullBackupAsync();
            
            if (!string.IsNullOrEmpty(backupPath))
            {
                await _toastService.Show("备份创建成功", 2000);
                await LoadBackupInfoAsync();
            }
            else
            {
                await _toastService.Show("备份创建失败", 2000);
            }
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("备份失败", ex.Message, "确定");
        }
        finally
        {
            IsCreatingBackup = false;
        }
    }

    private async Task RestoreBackupAsync()
    {
        var confirm = await Shell.Current.DisplayAlert(
            "确认恢复",
            "恢复操作会覆盖当前所有数据，确定要继续吗？",
            "恢复",
            "取消");
        
        if (!confirm)
            return;
        
        try
        {
            IsBusy = true;
            
            var (success, message) = await _backupService.RestoreBackupAsync();
            
            if (success)
            {
                await _toastService.Show("恢复成功，请重启应用", 3000);
            }
            else
            {
                await _toastService.Show($"恢复失败：{message}", 3000);
            }
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("恢复失败", ex.Message, "确定");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task CleanupOldBackupsAsync()
    {
        var confirm = await Shell.Current.DisplayAlert(
            "确认清理",
            "将删除旧的备份文件，只保留最近 3 个。确定要继续吗？",
            "清理",
            "取消");
        
        if (!confirm)
            return;
        
        try
        {
            IsBusy = true;
            
            var backups = await _backupService.ListAvailableBackupsAsync();
            
            if (backups.Count <= 3)
            {
                await _toastService.Show("无需清理，当前备份数量 ≤ 3", 2000);
                return;
            }
            
            // 保留最近 3 个
            var backupsToDelete = backups.Skip(3).ToList();
            
            foreach (var backupPath in backupsToDelete)
            {
                await _backupService.DeleteBackupAsync(backupPath);
            }
            
            await _toastService.Show($"已清理 {backupsToDelete.Count} 个旧备份", 2000);
            await LoadBackupInfoAsync();
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("清理失败", ex.Message, "确定");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void LoadAutoBackupSettings()
    {
        AutoBackupEnabled = Preferences.Get("AutoBackupEnabled", false);
        SelectedBackupInterval = Preferences.Get("BackupInterval", "每周");
        CloudBackupEnabled = Preferences.Get("CloudBackupEnabled", false);
    }

    private void SaveAutoBackupSettings()
    {
        Preferences.Set("AutoBackupEnabled", AutoBackupEnabled);
        Preferences.Set("BackupInterval", SelectedBackupInterval);
        Preferences.Set("CloudBackupEnabled", CloudBackupEnabled);
    }

    private static string FormatFileSize(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB" };
        double len = bytes;
        int order = 0;
        
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len = len / 1024;
        }
        
        return $"{len:0.##} {sizes[order]}";
    }
}
