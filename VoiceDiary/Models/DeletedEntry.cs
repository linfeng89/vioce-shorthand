namespace VoiceDiary.Models;

public class DeletedEntry
{
    [PrimaryKey, AutoIncrement]
    public long Id { get; set; }
    public long EntryId { get; set; }
    public string AudioFilePath { get; set; } = string.Empty;
    public string TranscribedText { get; set; } = string.Empty;
    public DateTime DeletedAt { get; set; }
    public DateTime OriginalCreatedAt { get; set; }
}
