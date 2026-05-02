using SQLite;

namespace VoiceDiary.Services;

public class SearchService : ISearchService
{
    private readonly IDatabaseService _databaseService;

    public SearchService(IDatabaseService databaseService)
    {
        _databaseService = databaseService;
    }

    public async Task<IEnumerable<DiaryEntry>> SearchAsync(string query, DateTime? startDate = null, DateTime? endDate = null)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            // 如果查询为空，返回所有条目
            return await _databaseService.GetRecentEntriesAsync(1000);
        }

        var conn = await _databaseService.GetConnectionAsync();
        
        // 使用 FTS5 全文搜索
        var sql = @"
            SELECT d.* FROM DiaryEntry d
            INNER JOIN DiaryEntry_FTS fts ON d.rowid = fts.rowid
            WHERE fts.TranscribedText MATCH ?
            AND d.IsDeleted = 0
        ";

        var parameters = new List<object> { query };

        // 添加日期范围过滤
        if (startDate.HasValue)
        {
            sql += " AND d.CreatedAt >= ?";
            parameters.Add(startDate.Value);
        }

        if (endDate.HasValue)
        {
            sql += " AND d.CreatedAt <= ?";
            parameters.Add(endDate.Value);
        }

        sql += " ORDER BY d.CreatedAt DESC";

        return await conn.QueryAsync<DiaryEntry>(sql, parameters.ToArray());
    }

    public async Task<IEnumerable<DiaryEntry>> SearchSimpleAsync(string query)
    {
        // 简单搜索：不使用分词，直接 LIKE 匹配
        var conn = await _databaseService.GetConnectionAsync();
        return await conn.Table<DiaryEntry>()
            .Where(e => !e.IsDeleted && e.TranscribedText.Contains(query))
            .OrderByDescending(e => e.CreatedAt)
            .ToListAsync();
    }

    public async Task RebuildIndexAsync()
    {
        var conn = await _databaseService.GetConnectionAsync();

        // 删除现有索引
        await conn.ExecuteAsync("DELETE FROM DiaryEntry_FTS");

        // 重建索引
        await conn.ExecuteAsync(@"
            INSERT INTO DiaryEntry_FTS(rowid, TranscribedText)
            SELECT rowid, TranscribedText 
            FROM DiaryEntry 
            WHERE IsTranscribed = 1 AND IsDeleted = 0
        ");
    }

    public async Task<int> GetIndexedCountAsync()
    {
        var conn = await _databaseService.GetConnectionAsync();
        return await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM DiaryEntry_FTS");
    }
}

public interface ISearchService
{
    Task<IEnumerable<DiaryEntry>> SearchAsync(string query, DateTime? startDate = null, DateTime? endDate = null);
    Task<IEnumerable<DiaryEntry>> SearchSimpleAsync(string query);
    Task RebuildIndexAsync();
    Task<int> GetIndexedCountAsync();
}
