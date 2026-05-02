using SQLite;

namespace VoiceDiary.Services;

public class DatabaseService : IDatabaseService
{
    private SQLiteAsyncConnection? _connection;
    private bool _initialized;
    private const int CurrentDatabaseVersion = 1;

    public async Task InitializeAsync()
    {
        if (_initialized)
            return;

        var databasePath = Path.Combine(
            FileSystem.AppDataDirectory,
            "voicediary.db3");

        _connection = new SQLiteAsyncConnection(databasePath, SQLiteOpenFlags.Create | SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.FullMutex);

        await CreateTablesAsync();
        await CreateIndexesAsync();
        await CreateTriggersAsync();
        await MigrateDatabaseAsync();
        
        _initialized = true;
    }

    public Task<SQLiteAsyncConnection> GetConnectionAsync()
    {
        if (_connection == null)
            throw new InvalidOperationException("Database not initialized. Call InitializeAsync first.");
        
        return Task.FromResult(_connection);
    }

    private async Task CreateTablesAsync()
    {
        await _connection!.CreateTableAsync<DiaryEntry>();
        await _connection.CreateTableAsync<AudioSegment>();
        await _connection.CreateTableAsync<AppSettings>();
    }

    private async Task CreateIndexesAsync()
    {
        var conn = await GetConnectionAsync();
        
        await conn.ExecuteAsync(@"
            CREATE INDEX IF NOT EXISTS IX_DiaryEntry_CreatedAt ON DiaryEntry(CreatedAt);
            CREATE INDEX IF NOT EXISTS IX_DiaryEntry_IsDeleted ON DiaryEntry(IsDeleted);
            CREATE INDEX IF NOT EXISTS IX_DiaryEntry_IsTranscribed ON DiaryEntry(IsTranscribed);
            CREATE INDEX IF NOT EXISTS IX_AudioSegment_EntryId ON AudioSegment(EntryId);
        ");

        // 创建 FTS5 全文搜索索引
        await CreateFtsIndexAsync();
    }

    private async Task CreateFtsIndexAsync()
    {
        var conn = await GetConnectionAsync();

        // 创建 FTS5 虚拟表
        await conn.ExecuteAsync(@"
            CREATE VIRTUAL TABLE IF NOT EXISTS DiaryEntry_FTS USING fts5(
                TranscribedText,
                content='DiaryEntry',
                content_rowid='rowid',
                tokenize='unicode61'
            )
        ");

        // 创建触发器自动维护索引
        await conn.ExecuteAsync(@"
            CREATE TRIGGER IF NOT EXISTS DiaryEntry_AI AFTER INSERT ON DiaryEntry 
            WHEN NEW.IsTranscribed = 1 AND NEW.IsDeleted = 0
            BEGIN
                INSERT OR REPLACE INTO DiaryEntry_FTS(rowid, TranscribedText) 
                VALUES (NEW.rowid, NEW.TranscribedText);
            END
        ");

        await conn.ExecuteAsync(@"
            CREATE TRIGGER IF NOT EXISTS DiaryEntry_AU AFTER UPDATE ON DiaryEntry 
            WHEN NEW.IsTranscribed = 1 AND NEW.IsDeleted = 0
            BEGIN
                INSERT OR REPLACE INTO DiaryEntry_FTS(rowid, TranscribedText) 
                VALUES (NEW.rowid, NEW.TranscribedText);
            END
        ");

        await conn.ExecuteAsync(@"
            CREATE TRIGGER IF NOT EXISTS DiaryEntry_AD AFTER DELETE ON DiaryEntry 
            OR WHEN OLD.IsDeleted = 1
            BEGIN
                DELETE FROM DiaryEntry_FTS WHERE rowid = OLD.rowid;
            END
        ");

        // 初始化现有数据
        await conn.ExecuteAsync(@"
            INSERT OR IGNORE INTO DiaryEntry_FTS(rowid, TranscribedText)
            SELECT rowid, TranscribedText FROM DiaryEntry 
            WHERE IsTranscribed = 1 AND IsDeleted = 0
        ");
    }

    private async Task CreateTriggersAsync()
    {
        await _connection!.ExecuteAsync(@"
            CREATE TRIGGER IF NOT EXISTS UpdateDiaryEntryUpdatedAt 
            AFTER UPDATE ON DiaryEntry
            BEGIN
                UPDATE DiaryEntry SET UpdatedAt = datetime('now') WHERE Id = NEW.Id;
            END;
        ");
    }

    private async Task MigrateDatabaseAsync()
    {
        var currentVersion = await GetDatabaseVersionAsync();

        if (currentVersion == 0)
        {
            await SetDatabaseVersionAsync(CurrentDatabaseVersion);
            return;
        }

        if (currentVersion < CurrentDatabaseVersion)
        {
            for (var version = currentVersion; version < CurrentDatabaseVersion; version++)
            {
                await ApplyMigrationAsync(version + 1);
            }
            await SetDatabaseVersionAsync(CurrentDatabaseVersion);
        }
    }

    private async Task ApplyMigrationAsync(int targetVersion)
    {
        var conn = await GetConnectionAsync();
        
        switch (targetVersion)
        {
            case 1:
                // 初始版本，无需迁移
                break;
            // 未来版本迁移在此添加
            // case 2:
            //     await conn.ExecuteAsync("ALTER TABLE DiaryEntry ADD COLUMN NewColumn TEXT;");
            //     break;
        }
    }

    public async Task<IEnumerable<DiaryEntry>> GetRecentEntriesAsync(int count, int skip = 0)
    {
        var conn = await GetConnectionAsync();
        return await conn
            .Table<DiaryEntry>()
            .Where(e => !e.IsDeleted)
            .OrderByDescending(e => e.CreatedAt)
            .Skip(skip)
            .Take(count)
            .ToListAsync();
    }

    public async Task<DiaryEntry?> GetEntryByIdAsync(string id)
    {
        var conn = await GetConnectionAsync();
        return await conn.Table<DiaryEntry>().FirstOrDefaultAsync(e => e.Id == id);
    }

    public async Task<int> SaveEntryAsync(DiaryEntry entry)
    {
        var conn = await GetConnectionAsync();
        if (entry.RowId == 0)
            return await conn.InsertAsync(entry);
        
        entry.UpdatedAt = DateTime.Now;
        return await conn.UpdateAsync(entry);
    }

    public async Task<int> DeleteEntryAsync(DiaryEntry entry)
    {
        var conn = await GetConnectionAsync();
        entry.IsDeleted = true;
        entry.DeletedAt = DateTime.Now;
        return await conn.UpdateAsync(entry);
    }

    public async Task<IEnumerable<DiaryEntry>> SearchEntriesAsync(string query, DateTime? startDate = null, DateTime? endDate = null)
    {
        var conn = await GetConnectionAsync();
        var sql = @"
            SELECT d.* FROM DiaryEntry d
            INNER JOIN DiaryEntry_FTS fts ON d.rowid = fts.rowid
            WHERE fts.TranscribedText MATCH ?
            AND d.IsDeleted = 0
        ";

        var parameters = new List<object> { query };

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

    public async Task<IEnumerable<DiaryEntry>> GetDeletedEntriesAsync()
    {
        var conn = await GetConnectionAsync();
        return await conn
            .Table<DiaryEntry>()
            .Where(e => e.IsDeleted)
            .OrderByDescending(e => e.DeletedAt)
            .ToListAsync();
    }

    public async Task RecoverEntryAsync(DiaryEntry entry)
    {
        var conn = await GetConnectionAsync();
        entry.IsDeleted = false;
        entry.DeletedAt = null;
        await conn.UpdateAsync(entry);
    }

    public async Task HardDeleteEntryAsync(DiaryEntry entry)
    {
        var conn = await GetConnectionAsync();
        await conn.DeleteAsync(entry);
    }

    public async Task<int> GetTotalCountAsync()
    {
        var conn = await GetConnectionAsync();
        return await conn.Table<DiaryEntry>().CountAsync(e => !e.IsDeleted);
    }

    public async Task<DateTime?> GetLastSyncDateAsync()
    {
        var conn = await GetConnectionAsync();
        var settings = await conn.Table<AppSettings>().FirstOrDefaultAsync(s => s.Key == "LastSyncDate");
        return settings?.Value != null ? DateTime.Parse(settings.Value) : null;
    }

    public async Task SetLastSyncDateAsync(DateTime date)
    {
        var conn = await GetConnectionAsync();
        var settings = await conn.Table<AppSettings>().FirstOrDefaultAsync(s => s.Key == "LastSyncDate");
        
        if (settings == null)
        {
            await conn.InsertAsync(new AppSettings
            {
                Key = "LastSyncDate",
                Value = date.ToString("O")
            });
        }
        else
        {
            settings.Value = date.ToString("O");
            await conn.UpdateAsync(settings);
        }
    }

    public async Task<int> GetDatabaseVersionAsync()
    {
        var conn = await GetConnectionAsync();
        var result = await conn.ExecuteScalarAsync<int>("PRAGMA user_version");
        return result;
    }

    public async Task SetDatabaseVersionAsync(int version)
    {
        var conn = await GetConnectionAsync();
        await conn.ExecuteAsync($"PRAGMA user_version = {version}");
    }
}

public class AppSettings
{
    [SQLite.PrimaryKey, SQLite.AutoIncrement]
    public int Id { get; set; }
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
}
