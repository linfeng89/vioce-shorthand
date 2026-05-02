namespace VoiceDiary.Services;

public interface IStorageService
{
    string AppDatabasePath { get; }
    string AppAudioPath { get; }
    string BackupPath { get; }

    Task<string> GetAudioFilePathAsync(string fileName);
    Task<bool> FileExistsAsync(string fileName);
    Task<Stream> OpenFileForReadAsync(string fileName);
    Task<Stream> OpenFileForWriteAsync(string fileName);
    Task DeleteFileAsync(string fileName);
    Task<IEnumerable<string>> GetAllAudioFilesAsync();
    Task<long> GetAvailableSpaceAsync();
    Task<long> GetTotalSpaceAsync();
    Task<long> GetAppStorageSizeAsync();
}
