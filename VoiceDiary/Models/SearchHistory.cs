using SQLite;

namespace VoiceDiary.Models;

[Table("SearchHistory")]
public class SearchHistory
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }
    
    [MaxLength(200)]
    public string Query { get; set; } = string.Empty;
    
    public DateTime SearchedAt { get; set; } = DateTime.Now;
    
    public int ResultCount { get; set; }
}
