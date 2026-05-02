namespace VoiceDiary.Services;

public interface IExportService
{
    Task<(bool success, string? filePath)> ExportAsTxtAsync(IEnumerable<DiaryEntry> entries, string exportPath);
    Task<(bool success, string? filePath)> ExportAsMarkdownAsync(IEnumerable<DiaryEntry> entries, string exportPath);
    Task<(bool success, string? filePath)> ExportAsJsonAsync(IEnumerable<DiaryEntry> entries, string exportPath);
    Task<(bool success, string? filePath)> ExportAudioAsync(IEnumerable<DiaryEntry> entries, string exportPath);
    Task<(bool success, string? filePath)> ExportAllAsync(IEnumerable<DiaryEntry> entries, string exportPath);
}
