namespace VoiceDiary.Services;

public interface ITrashService
{
    Task MoveToTrashAsync(DiaryEntry entry);
    Task RestoreFromTrashAsync(long entryId);
    Task PermanentlyDeleteAsync(long entryId);
    Task<List<DeletedEntry>> GetTrashEntriesAsync(int days = 30);
    Task AutoCleanupAsync(int retentionDays = 30);
    Task<int> GetTrashCountAsync();
}

public class TrashService : ITrashService
{
    private readonly IDatabaseService _databaseService;
    private readonly ISearchService _searchService;
    
    public TrashService(IDatabaseService databaseService, ISearchService searchService)
    {
        _databaseService = databaseService;
        _searchService = searchService;
    }
    
    public async Task MoveToTrashAsync(DiaryEntry entry)
    {
        var db = _databaseService.GetConnection();
        
        // 创建删除记录
        var deletedEntry = new DeletedEntry
        {
            EntryId = entry.Id,
            AudioFilePath = entry.AudioFilePath,
            TranscribedText = entry.TranscribedText,
            DeletedAt = DateTime.Now,
            OriginalCreatedAt = entry.CreatedAt
        };
        
        await db.InsertAsync(deletedEntry);
        
        // 软删除原条目
        entry.IsDeleted = true;
        entry.DeletedAt = DateTime.Now;
        await db.UpdateAsync(entry);
        
        // 从搜索索引移除
        await _searchService.RemoveFromIndexAsync(entry.Id);
    }
    
    public async Task RestoreFromTrashAsync(long entryId)
    {
        var db = _databaseService.GetConnection();
        
        // 查找删除记录
        var deletedEntry = await db.Table<DeletedEntry>()
            .FirstOrDefaultAsync(d => d.EntryId == entryId);
        
        if (deletedEntry == null)
        {
            throw new Exception("删除记录不存在");
        }
        
        // 恢复原条目
        var entry = await db.Table<DiaryEntry>()
            .FirstOrDefaultAsync(e => e.Id == entryId);
        
        if (entry != null)
        {
            entry.IsDeleted = false;
            entry.DeletedAt = null;
            await db.UpdateAsync(entry);
            
            // 重新加入搜索索引
            if (entry.IsTranscribed && !string.IsNullOrEmpty(entry.TranscribedText))
            {
                await _searchService.AddToIndexAsync(entry);
            }
        }
        
        // 删除删除记录
        await db.DeleteAsync(deletedEntry);
    }
    
    public async Task PermanentlyDeleteAsync(long entryId)
    {
        var db = _databaseService.GetConnection();
        
        // 查找删除记录
        var deletedEntry = await db.Table<DeletedEntry>()
            .FirstOrDefaultAsync(d => d.EntryId == entryId);
        
        if (deletedEntry != null)
        {
            // 删除音频文件
            if (File.Exists(deletedEntry.AudioFilePath))
            {
                File.Delete(deletedEntry.AudioFilePath);
            }
            
            // 从数据库删除原条目
            var entry = await db.Table<DiaryEntry>()
                .FirstOrDefaultAsync(e => e.Id == entryId);
            
            if (entry != null)
            {
                await db.DeleteAsync(entry);
            }
            
            // 从搜索索引移除
            await _searchService.RemoveFromIndexAsync(entryId);
            
            // 删除删除记录
            await db.DeleteAsync(deletedEntry);
        }
    }
    
    public async Task<List<DeletedEntry>> GetTrashEntriesAsync(int days = 30)
    {
        var db = _databaseService.GetConnection();
        var cutoffDate = DateTime.Now.AddDays(-days);
        
        return await db.Table<DeletedEntry>()
            .Where(d => d.DeletedAt >= cutoffDate)
            .OrderByDescending(d => d.DeletedAt)
            .ToListAsync();
    }
    
    public async Task AutoCleanupAsync(int retentionDays = 30)
    {
        var db = _databaseService.GetConnection();
        var cutoffDate = DateTime.Now.AddDays(-retentionDays);
        
        // 查找过期的删除记录
        var expiredEntries = await db.Table<DeletedEntry>()
            .Where(d => d.DeletedAt < cutoffDate)
            .ToListAsync();
        
        foreach (var entry in expiredEntries)
        {
            try
            {
                await PermanentlyDeleteAsync(entry.EntryId);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Auto cleanup error for entry {entry.EntryId}: {ex}");
            }
        }
    }
    
    public async Task<int> GetTrashCountAsync()
    {
        var db = _databaseService.GetConnection();
        var cutoffDate = DateTime.Now.AddDays(-30);
        
        return await db.Table<DeletedEntry>()
            .CountAsync(d => d.DeletedAt >= cutoffDate);
    }
}
