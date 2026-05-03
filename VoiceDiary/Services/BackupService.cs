using System.IO.Compression;
using System.Text.Json;

namespace VoiceDiary.Services;

public interface IBackupService
{
    Task<(bool success, string? message)> CreateBackupAsync();
    Task<(bool success, string? message)> RestoreBackupAsync(string? backupFilePath = null);
    Task<bool> BackupExistsAsync();
    Task<DateTime?> GetBackupDateAsync();
    Task<string?> CreateFullBackupAsync();
    Task<bool> RestoreFromFullBackupAsync(string zipFilePath);
    Task<List<string>> ListAvailableBackupsAsync();
    Task DeleteBackupAsync(string backupPath);
}

public class BackupService : IBackupService
{
    private readonly IDatabaseService _databaseService;
    private readonly IStorageService _storageService;
    
    public BackupService(IDatabaseService databaseService, IStorageService storageService)
    {
        _databaseService = databaseService;
        _storageService = storageService;
    }
    
    public async Task<(bool success, string? message)> CreateBackupAsync()
    {
        try
        {
            var backupPath = await CreateFullBackupAsync();
            return backupPath != null 
                ? (true, $"备份成功：{Path.GetFileName(backupPath)}") 
                : (false, "备份失败");
        }
        catch (Exception ex)
        {
            return (false, $"备份失败：{ex.Message}");
        }
    }
    
    public async Task<(bool success, string? message)> RestoreBackupAsync(string? backupFilePath = null)
    {
        try
        {
            string zipPath;
            
            if (!string.IsNullOrEmpty(backupFilePath))
            {
                zipPath = backupFilePath;
                if (!File.Exists(zipPath))
                    return (false, "备份文件不存在");
            }
            else
            {
                var latestBackup = (await ListAvailableBackupsAsync()).FirstOrDefault();
                if (latestBackup == null)
                    return (false, "没有可用的备份文件");
                zipPath = latestBackup;
            }
            
            var result = await RestoreFromFullBackupAsync(zipPath);
            return result ? (true, "恢复成功") : (false, "恢复失败");
        }
        catch (Exception ex)
        {
            return (false, $"恢复失败：{ex.Message}");
        }
    }
    
    public async Task<string?> CreateFullBackupAsync()
    {
        var backupDir = Path.Combine(FileSystem.AppDataDirectory, "Backups");
        Directory.CreateDirectory(backupDir);
        
        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var backupPath = Path.Combine(backupDir, $"VoiceDiary_Backup_{timestamp}.zip");
        
        if (File.Exists(backupPath))
            File.Delete(backupPath);
        
        using var zip = ZipFile.Open(backupPath, ZipArchiveMode.Create);
        
        // 1. 导出数据库
        var entries = await _databaseService.GetAllEntriesAsync();
        var dbJson = JsonSerializer.Serialize(entries, new JsonSerializerOptions 
        { 
            WriteIndented = true,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        });
        
        var dbEntry = zip.CreateEntry("database.json", CompressionLevel.Optimal);
        await using (var writer = new StreamWriter(dbEntry.Open()))
        {
            await writer.WriteAsync(dbJson);
        }
        
        // 2. 添加音频文件
        var audioDir = zip.CreateEntry("audio/");
        int audioCount = 0;
        
        foreach (var entry in entries)
        {
            if (!string.IsNullOrEmpty(entry.AudioFilePath) && File.Exists(entry.AudioFilePath))
            {
                var audioFileName = Path.GetFileName(entry.AudioFilePath);
                var audioEntry = zip.CreateEntry($"audio/{audioFileName}", CompressionLevel.Optimal);
                
                await using (var source = File.OpenRead(entry.AudioFilePath))
                await using (var dest = audioEntry.Open())
                {
                    await source.CopyToAsync(dest);
                }
                
                audioCount++;
            }
        }
        
        // 3. 导出元数据
        var metadata = new
        {
            BackupDate = DateTime.Now,
            Version = "1.0.0",
            EntryCount = entries.Count,
            AudioCount = audioCount
        };
        
        var metadataJson = JsonSerializer.Serialize(metadata, new JsonSerializerOptions { WriteIndented = true });
        var metadataEntry = zip.CreateEntry("metadata.json", CompressionLevel.Optimal);
        await using (var writer = new StreamWriter(metadataEntry.Open()))
        {
            await writer.WriteAsync(metadataJson);
        }
        
        return backupPath;
    }
    
    public async Task<bool> RestoreFromFullBackupAsync(string zipFilePath)
    {
        if (!File.Exists(zipFilePath))
            throw new FileNotFoundException("备份文件不存在", zipFilePath);
        
        using var zip = ZipFile.OpenRead(zipFilePath);
        
        // 1. 恢复数据库
        var dbEntry = zip.GetEntry("database.json");
        if (dbEntry == null)
            throw new InvalidDataException("备份中未找到数据库文件");
        
        await using (var reader = new StreamReader(dbEntry.Open()))
        {
            var dbJson = await reader.ReadToEndAsync();
            var entries = JsonSerializer.Deserialize<List<DiaryEntry>>(dbJson);
            
            if (entries == null)
                throw new InvalidDataException("数据库格式无效");
            
            // 清空现有数据
            await ClearAllDataAsync();
            
            // 导入新数据
            foreach (var entry in entries)
            {
                await _databaseService.InsertEntryAsync(entry);
            }
        }
        
        // 2. 恢复音频文件
        var audioEntries = zip.Entries.Where(e => e.FullName.StartsWith("audio/") && !e.FullName.EndsWith("/"));
        
        foreach (var audioEntry in audioEntries)
        {
            var audioFileName = Path.GetFileName(audioEntry.FullName);
            var audioPath = await _storageService.GetAudioFilePathAsync(audioFileName);
            
            // 确保目录存在
            Directory.CreateDirectory(Path.GetDirectoryName(audioPath)!);
            
            await using (var source = audioEntry.Open())
            await using (var dest = File.OpenWrite(audioPath))
            {
                await source.CopyToAsync(dest);
            }
        }
        
        return true;
    }
    
    public async Task<List<string>> ListAvailableBackupsAsync()
    {
        var backupDir = Path.Combine(FileSystem.AppDataDirectory, "Backups");
        if (!Directory.Exists(backupDir))
            return new List<string>();
        
        var backups = Directory.GetFiles(backupDir, "*.zip")
            .Select(f => new FileInfo(f))
            .Where(f => f.Length > 0)
            .OrderByDescending(f => f.LastWriteTime)
            .Select(f => f.FullName)
            .ToList();
        
        return backups;
    }
    
    public async Task DeleteBackupAsync(string backupPath)
    {
        if (File.Exists(backupPath))
        {
            await Task.Run(() => File.Delete(backupPath));
        }
    }
    
    public Task<bool> BackupExistsAsync()
    {
        var backupDir = Path.Combine(FileSystem.AppDataDirectory, "Backups");
        if (!Directory.Exists(backupDir))
            return Task.FromResult(false);
        
        return Task.FromResult(Directory.GetFiles(backupDir, "*.zip").Length > 0);
    }
    
    public async Task<DateTime?> GetBackupDateAsync()
    {
        var backups = await ListAvailableBackupsAsync();
        var latestBackup = backups.FirstOrDefault();
        
        if (latestBackup != null && File.Exists(latestBackup))
        {
            var info = new FileInfo(latestBackup);
            return info.LastWriteTime;
        }
        
        return null;
    }
    
    private async Task ClearAllDataAsync()
    {
        var db = await _databaseService.GetConnectionAsync();
        
        // 删除所有日记条目
        var entries = await db.Table<DiaryEntry>().ToListAsync();
        foreach (var entry in entries)
        {
            await db.DeleteAsync(entry);
        }
    }
}
