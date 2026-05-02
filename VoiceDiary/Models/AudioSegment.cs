namespace VoiceDiary.Models;

public class AudioSegment
{
    [SQLite.PrimaryKey, SQLite.AutoIncrement]
    public int RowId { get; set; }

    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    public string EntryId { get; set; } = string.Empty;

    public string AudioFileName { get; set; } = string.Empty;

    public int SegmentIndex { get; set; }

    public int DurationSeconds { get; set; }

    public DateTime RecordedAt { get; set; } = DateTime.Now;

    public string TranscribedText { get; set; } = string.Empty;

    public SegmentType Type { get; set; } = SegmentType.Original;
}

public enum SegmentType
{
    Original,
    Append,
    ReRecord
}
