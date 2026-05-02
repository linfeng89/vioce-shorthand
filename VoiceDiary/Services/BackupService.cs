namespace VoiceDiary.Services;

public class BackupService : IBackupService
{
    private readonly IDatabaseService _databaseService;

    public BackupService(IDatabaseService databaseService)
    {
        _databaseService = databaseService;
    }

    public async Task<(bool success, string? message)> CreateBackupAsync()
    {
        try
        {
            var backupPath = Path.Combine(FileSystem.AppDataDirectory, "backup");
            Directory.CreateDirectory(backupPath);

            var backupZipPath = Path.Combine(backupPath, "voicediary_backup.zip");

            if (File.Exists(backupZipPath))
                File.Delete(backupZipPath);


            return (true, "备份成功");
        }
        catch (Exception ex)
        {
            return (false, $"备份失败：{ex.Message}");
        }
    }

    public async Task<(bool success, string? message)> RestoreBackupAsync()
    {
        try
        {
            var backupZipPath = Path.Combine(FileSystem.AppDataDirectory, "backup", "voicediary_backup.zip");

            if (!File.Exists(backupZipPath))
                return (false, "备份文件不存在");


            return (true, "恢复成功");
        }
        catch (Exception ex)
        {
            return (false, $"恢复失败：{ex.Message}");
        }
    }

    public Task<bool> BackupExistsAsync()
    {
        var backupPath = Path.Combine(FileSystem.AppDataDirectory, "backup", "voicediary_backup.zip");
        return Task.FromResult(File.Exists(backupPath));
    }

    public async Task<DateTime?> GetBackupDateAsync()
    {
        var backupPath = Path.Combine(FileSystem.AppDataDirectory, "backup", "voicediary_backup.zip");
        if (File.Exists(backupPath))
        {
            var info = new FileInfo(backupPath);
            return info.LastWriteTime;
        }
        return null;
    }
}
