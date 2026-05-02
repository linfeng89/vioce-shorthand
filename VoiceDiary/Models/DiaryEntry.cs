namespace VoiceDiary.Models;

public class DiaryEntry
{
    [SQLite.PrimaryKey, SQLite.AutoIncrement]
    public int RowId { get; set; }

    [SQLite.Unique]
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public DateTime UpdatedAt { get; set; } = DateTime.Now;

    public string TranscribedText { get; set; } = string.Empty;

    public string AudioFileName { get; set; } = string.Empty;

    public int DurationSeconds { get; set; }

    public bool IsTranscribed { get; set; }

    public int TranscribeAttempts { get; set; }

    public string TranscribeError { get; set; } = string.Empty;

    public bool IsCompressed { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime? DeletedAt { get; set; }

    public string? Mood { get; set; }
}
