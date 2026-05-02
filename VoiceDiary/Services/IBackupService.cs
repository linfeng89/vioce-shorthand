namespace VoiceDiary.Services;

public interface IBackupService
{
    Task<(bool success, string? message)> CreateBackupAsync();
    Task<(bool success, string? message)> RestoreBackupAsync();
    Task<bool> BackupExistsAsync();
    Task<DateTime?> GetBackupDateAsync();
}
