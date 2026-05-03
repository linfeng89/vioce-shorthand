namespace VoiceDiary.Services;

public class AutomaticBackupService
{
    private readonly IBackupService _backupService;
    private readonly IToastService _toastService;
    private Timer? _backupTimer;
    private bool _isRunning;

    public AutomaticBackupService(IBackupService backupService, IToastService toastService)
    {
        _backupService = backupService;
        _toastService = toastService;
    }

    public void Start()
    {
        var autoBackupEnabled = Preferences.Get("AutoBackupEnabled", false);
        
        if (!autoBackupEnabled)
            return;
        
        var intervalDays = GetIntervalDays();
        
        _backupTimer = new Timer(
            async _ => await PerformAutomaticBackupAsync(),
            null,
            TimeSpan.FromMinutes(1), // 1 分钟后首次执行
            TimeSpan.FromDays(intervalDays)); // 之后按间隔执行
    }

    public void Stop()
    {
        _backupTimer?.Dispose();
        _backupTimer = null;
    }

    public void Restart()
    {
        Stop();
        Start();
    }

    private async Task PerformAutomaticBackupAsync()
    {
        if (_isRunning)
            return;
        
        try
        {
            _isRunning = true;
            
            // 创建备份
            var backupPath = await _backupService.CreateFullBackupAsync();
            
            if (!string.IsNullOrEmpty(backupPath))
            {
                // 清理旧备份（保留最近 3 个）
                await CleanupOldBackupsAsync();
                
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    await _toastService.Show("自动备份已完成", 2000);
                });
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Automatic backup failed: {ex.Message}");
        }
        finally
        {
            _isRunning = false;
        }
    }

    private async Task CleanupOldBackupsAsync()
    {
        var backups = await _backupService.ListAvailableBackupsAsync();
        
        if (backups.Count <= 3)
            return;
        
        // 保留最近 3 个
        var backupsToDelete = backups.Skip(3).ToList();
        
        foreach (var backupPath in backupsToDelete)
        {
            await _backupService.DeleteBackupAsync(backupPath);
        }
    }

    private static int GetIntervalDays()
    {
        var interval = Preferences.Get("BackupInterval", "每周");
        
        return interval switch
        {
            "每天" => 1,
            "每周" => 7,
            "每月" => 30,
            _ => 7
        };
    }
}
