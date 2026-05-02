namespace VoiceDiary.Services;

public class Fts5Service
{
    private readonly IDatabaseService _databaseService;

    public Fts5Service(IDatabaseService databaseService)
    {
        _databaseService = databaseService;
    }

    public async Task InitializeAsync()
    {
        var conn = await _databaseService.GetConnectionAsync();


        await conn.ExecuteAsync(@"
            CREATE VIRTUAL TABLE IF NOT EXISTS DiaryEntry_FTS USING fts5(
                TranscribedText,
                content='DiaryEntry',
                content_rowid='rowid',
                tokenize='unicode61'
            )
        ");

        await conn.ExecuteAsync(@"
            CREATE TRIGGER IF NOT EXISTS DiaryEntry_AI AFTER INSERT ON DiaryEntry 
            WHEN NEW.IsTranscribed = 1
            BEGIN
                INSERT OR REPLACE INTO DiaryEntry_FTS(rowid, TranscribedText) 
                VALUES (NEW.rowid, NEW.TranscribedText);
            END
        ");

        await conn.ExecuteAsync(@"
            CREATE TRIGGER IF NOT EXISTS DiaryEntry_AU AFTER UPDATE ON DiaryEntry 
            WHEN NEW.IsTranscribed = 1
            BEGIN
                INSERT OR REPLACE INTO DiaryEntry_FTS(rowid, TranscribedText) 
                VALUES (NEW.rowid, NEW.TranscribedText);
            END
        ");

        await conn.ExecuteAsync(@"
            CREATE TRIGGER IF NOT EXISTS DiaryEntry_AD AFTER DELETE ON DiaryEntry 
            BEGIN
                DELETE FROM DiaryEntry_FTS WHERE rowid = OLD.rowid;
            END
        ");
    }

    public async Task RebuildIndexAsync()
    {
        var conn = await _databaseService.GetConnectionAsync();

        await conn.ExecuteAsync("DELETE FROM DiaryEntry_FTS");

        await conn.ExecuteAsync(@"
            INSERT INTO DiaryEntry_FTS(rowid, TranscribedText)
            SELECT rowid, TranscribedText FROM DiaryEntry WHERE IsTranscribed = 1
        ");
    }
}
