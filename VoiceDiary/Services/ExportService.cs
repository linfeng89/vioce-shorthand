using System.Text.Json;

namespace VoiceDiary.Services;

public class ExportService : IExportService
{
    public Task<(bool success, string? filePath)> ExportAsTxtAsync(IEnumerable<DiaryEntry> entries, string exportPath)
    {
        var content = new StringBuilder();
        content.AppendLine("========== 语音日记 exported ==========");
        content.AppendLine($"导出时间：{DateTime.Now:yyyy-MM-dd HH:mm}");
        content.AppendLine($"总条目：{entries.Count()} 条");
        content.AppendLine("------------------------------------------");

        var dateGroups = entries.GroupBy(e => e.CreatedAt.Date)
            .OrderByDescending(g => g.Key);

        foreach (var group in dateGroups)
        {
            content.AppendLine();
            content.AppendLine($"📅 {group.Key:yyyy-MM-dd}");
            content.AppendLine("------------------------------------------");

            foreach (var entry in group.OrderByDescending(e => e.CreatedAt))
            {
                content.AppendLine();
                content.AppendLine($"🌅 {GetTimePeriod(entry.CreatedAt)} {entry.CreatedAt:HH:mm} | {entry.DurationSeconds}s | 🎵 {entry.AudioFileName}");
                content.AppendLine(entry.TranscribedText);
            }
        }

        var filePath = Path.Combine(exportPath, $"diary_{DateTime.Now:yyyyMMdd_HHmmss}.txt");
        File.WriteAllText(filePath, content.ToString());
        return Task.FromResult((true, filePath));
    }

    public Task<(bool success, string? filePath)> ExportAsMarkdownAsync(IEnumerable<DiaryEntry> entries, string exportPath)
    {
        var content = new StringBuilder();
        content.AppendLine("# 语音日记导出");
        content.AppendLine();
        content.AppendLine($"**导出时间**: {DateTime.Now:yyyy-MM-dd HH:mm}");
        content.AppendLine($"**总条目**: {entries.Count()} 条");
        content.AppendLine();

        var dateGroups = entries.GroupBy(e => e.CreatedAt.Date)
            .OrderByDescending(g => g.Key);

        foreach (var group in dateGroups)
        {
            content.AppendLine($"## {group.Key:yyyy 年 MM 月 dd 日}");
            content.AppendLine();

            foreach (var entry in group.OrderByDescending(e => e.CreatedAt))
            {
                content.AppendLine($"### {GetTimePeriod(entry.CreatedAt)} {entry.CreatedAt:HH:mm}");
                content.AppendLine();
                content.AppendLine($"⏱️ 时长：{entry.DurationSeconds}s");
                content.AppendLine();
                content.AppendLine(entry.TranscribedText);
                content.AppendLine();
                content.AppendLine("---");
                content.AppendLine();
            }
        }

        var filePath = Path.Combine(exportPath, $"diary_{DateTime.Now:yyyyMMdd_HHmmss}.md");
        File.WriteAllText(filePath, content.ToString());
        return Task.FromResult((true, filePath));
    }

    public async Task<(bool success, string? filePath)> ExportAsJsonAsync(IEnumerable<DiaryEntry> entries, string exportPath)
    {
        var exportData = new
        {
            exportedAt = DateTime.Now.ToString("O"),
            totalCount = entries.Count(),
            entries = entries.Select(e => new
            {
                e.Id,
                createdAt = e.CreatedAt.ToString("O"),
                e.DurationSeconds,
                e.TranscribedText,
                e.AudioFileName,
                e.IsTranscribed,
                e.IsCompressed
            })
        };

        var options = new JsonSerializerOptions { WriteIndented = true, Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping };
        var json = JsonSerializer.Serialize(exportData, options);

        var filePath = Path.Combine(exportPath, $"diary_{DateTime.Now:yyyyMMdd_HHmmss}.json");
        await File.WriteAllTextAsync(filePath, json);
        return (true, filePath);
    }

    public Task<(bool success, string? filePath)> ExportAudioAsync(IEnumerable<DiaryEntry> entries, string exportPath)
    {
        var audioPath = Path.Combine(exportPath, "audio");
        Directory.CreateDirectory(audioPath);

        foreach (var entry in entries)
        {
            var sourceFile = Path.Combine(FileSystem.AppDataDirectory, "audio", entry.AudioFileName);
            if (File.Exists(sourceFile))
            {
                var destFile = Path.Combine(audioPath, entry.AudioFileName);
                File.Copy(sourceFile, destFile, true);
            }
        }

        return Task.FromResult((true, exportPath));
    }

    public async Task<(bool success, string? filePath)> ExportAllAsync(IEnumerable<DiaryEntry> entries, string exportPath)
    {
        var exportDir = Path.Combine(exportPath, $"VoiceDiary 导出_{DateTime.Now:yyyyMMdd_HHmmss}");
        Directory.CreateDirectory(exportDir);

        await ExportAsTxtAsync(entries, exportDir);
        await ExportAsMarkdownAsync(entries, exportDir);
        await ExportAsJsonAsync(entries, exportDir);
        await ExportAudioAsync(entries, exportDir);

        var zipPath = exportDir + ".zip";
        if (File.Exists(zipPath))
            File.Delete(zipPath);

        return (true, zipPath);
    }

    private static string GetTimePeriod(DateTime time)
    {
        return time.Hour switch
        {
            >= 6 and < 12 => "上午",
            >= 12 and < 18 => "下午",
            >= 18 and < 21 => "傍晚",
            _ => "深夜"
        };
    }
}
