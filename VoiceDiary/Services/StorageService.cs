namespace VoiceDiary.Services;

public class StorageService : IStorageService
{
    private string? _appDatabasePath;
    private string? _appAudioPath;
    private string? _backupPath;

    public string AppDatabasePath => _appDatabasePath ??= Path.Combine(FileSystem.AppDataDirectory, "database");

    public string AppAudioPath => _appAudioPath ??= Path.Combine(FileSystem.AppDataDirectory, "audio");

    public string BackupPath => _backupPath ??= Path.Combine(FileSystem.AppDataDirectory, "backup");

    public StorageService()
    {
        Directory.CreateDirectory(AppDatabasePath);
        Directory.CreateDirectory(AppAudioPath);
        Directory.CreateDirectory(BackupPath);
    }

    public Task<string> GetAudioFilePathAsync(string fileName)
    {
        return Task.FromResult(Path.Combine(AppAudioPath, fileName));
    }

    public Task<bool> FileExistsAsync(string fileName)
    {
        return Task.FromResult(File.Exists(Path.Combine(AppAudioPath, fileName)));
    }

    public Task<Stream> OpenFileForReadAsync(string fileName)
    {
        var path = Path.Combine(AppAudioPath, fileName);
        return Task.FromResult<Stream>(File.OpenRead(path));
    }

    public Task<Stream> OpenFileForWriteAsync(string fileName)
    {
        var path = Path.Combine(AppAudioPath, fileName);
        return Task.FromResult<Stream>(File.Create(path));
    }

    public Task DeleteFileAsync(string fileName)
    {
        var path = Path.Combine(AppAudioPath, fileName);
        if (File.Exists(path))
            File.Delete(path);
        return Task.CompletedTask;
    }

    public Task<IEnumerable<string>> GetAllAudioFilesAsync()
    {
        var files = Directory.GetFiles(AppAudioPath, "*.wav")
            .Concat(Directory.GetFiles(AppAudioPath, "*.m4a"));
        return Task.FromResult(files.Select(Path.GetFileName));
    }

    public async Task<long> GetAvailableSpaceAsync()
    {
        try
        {
            var driveInfo = new DriveInfo(Path.GetPathRoot(AppAudioPath));
            return driveInfo.AvailableFreeSpace;
        }
        catch
        {
            return 0;
        }
    }

    public async Task<long> GetTotalSpaceAsync()
    {
        try
        {
            var driveInfo = new DriveInfo(Path.GetPathRoot(AppAudioPath));
            return driveInfo.TotalSize;
        }
        catch
        {
            return 0;
        }
    }

    public Task<long> GetAppStorageSizeAsync()
    {
        var size = DirectorySize(AppAudioPath) + DirectorySize(AppDatabasePath);
        return Task.FromResult(size);
    }

    private static long DirectorySize(string directory)
    {
        try
        {
            return Directory.EnumerateFiles(directory, "*", SearchOption.AllDirectories)
                .Sum(f => new FileInfo(f).Length);
        }
        catch
        {
            return 0;
        }
    }
}
