namespace VoiceDiary.Services;

public interface IDatabaseService
{
    Task InitializeAsync();
    Task<SQLiteAsyncConnection> GetConnectionAsync();
    Task<IEnumerable<DiaryEntry>> GetRecentEntriesAsync(int count, int skip = 0);
    Task<DiaryEntry?> GetEntryByIdAsync(string id);
    Task<int> SaveEntryAsync(DiaryEntry entry);
    Task<int> DeleteEntryAsync(DiaryEntry entry);
    Task<IEnumerable<DiaryEntry>> SearchEntriesAsync(string query, DateTime? startDate = null, DateTime? endDate = null);
    Task<IEnumerable<DiaryEntry>> GetDeletedEntriesAsync();
    Task RecoverEntryAsync(DiaryEntry entry);
    Task HardDeleteEntryAsync(DiaryEntry entry);
    Task<int> GetTotalCountAsync();
    Task<DateTime?> GetLastSyncDateAsync();
    Task SetLastSyncDateAsync(DateTime date);
    Task<int> GetDatabaseVersionAsync();
    Task SetDatabaseVersionAsync(int version);
}
