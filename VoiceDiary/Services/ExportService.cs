using System.Text;
using System.Text.Json;

namespace VoiceDiary.Services;

public enum ExportFormat
{
    Text,
    Markdown,
    Json
}

public interface IExportService
{
    Task<string> ExportToTextAsync(DiaryEntry entry);
    Task<string> ExportToMarkdownAsync(DiaryEntry entry);
    Task<string> ExportToJsonAsync(DiaryEntry entry);
    Task<string> ExportMultipleAsync(IEnumerable<DiaryEntry> entries, ExportFormat format);
    Task ShareAsync(string content, string title);
    Task<string> ExportAllToZipAsync();
    Task<bool> ImportFromZipAsync(string zipFilePath);
}

public class ExportService : IExportService
{
    private readonly IAudioCompressor _audioCompressor;
    private readonly IStorageService _storageService;
    
    public ExportService(IAudioCompressor audioCompressor, IStorageService storageService)
    {
        _audioCompressor = audioCompressor;
        _storageService = storageService;
    }
    
    public async Task<string> ExportToTextAsync(DiaryEntry entry)
    {
        var sb = new StringBuilder();
        
        sb.AppendLine($"【{entry.Title}】");
        sb.AppendLine($"日期：{entry.CreatedAt:yyyy 年 M 月 d 日 HH:mm}");
        sb.AppendLine($"时长：{FormatDuration(entry.AudioDuration)}");
        
        if (entry.Tags?.Any() == true)
            sb.AppendLine($"标签：{string.Join("、", entry.Tags)}");
        
        if (!string.IsNullOrEmpty(entry.Location))
            sb.AppendLine($"位置：{entry.Location}");
        
        if (entry.Mood != null)
            sb.AppendLine($"心情：{GetMoodEmoji(entry.Mood.Value)}");
        
        sb.AppendLine();
        sb.AppendLine("【正文内容】");
        sb.AppendLine(entry.Content);
        
        sb.AppendLine();
        sb.AppendLine("【录音文件】");
        sb.AppendLine($"录音：{Path.GetFileName(entry.AudioFilePath)}");
        sb.AppendLine($"转写：{(entry.TranscriptionStatus == TranscriptionStatus.Completed ? "Completed" : "Pending")}");
        
        return await Task.FromResult(sb.ToString());
    }
    
    public async Task<string> ExportToMarkdownAsync(DiaryEntry entry)
    {
        var sb = new StringBuilder();
        
        sb.AppendLine($"# {entry.Title}");
        sb.AppendLine();
        
        var metadata = new List<string>();
        metadata.Add($"📅 {entry.CreatedAt:yyyy 年 M 月 d 日 HH:mm}");
        metadata.Add($"⏱️ {FormatDuration(entry.AudioDuration)}");
        
        if (entry.Tags?.Any() == true)
            metadata.Add($"🏷️ {string.Join("、", entry.Tags)}");
        
        if (!string.IsNullOrEmpty(entry.Location))
            metadata.Add($"📍 {entry.Location}");
        
        if (entry.Mood != null)
            metadata.Add($"💭 {GetMoodEmoji(entry.Mood.Value)}");
        
        sb.AppendLine($"> {string.Join(" | ", metadata)}");
        sb.AppendLine();
        
        sb.AppendLine("## 正文内容");
        sb.AppendLine();
        sb.AppendLine(entry.Content);
        sb.AppendLine();
        
        sb.AppendLine("---");
        sb.AppendLine($"**录音文件**: `{Path.GetFileName(entry.AudioFilePath)}`  ");
        sb.AppendLine($"**转写状态**: {(entry.TranscriptionStatus == TranscriptionStatus.Completed ? "✅ 已完成" : "⏳ 待处理")}");
        
        return await Task.FromResult(sb.ToString());
    }
    
    public async Task<string> ExportToJsonAsync(DiaryEntry entry)
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            ReadCommentHandling = System.Text.Json.JsonCommentHandling.Skip,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
        
        return await Task.FromResult(JsonSerializer.Serialize(entry, options));
    }
    
    public async Task<string> ExportMultipleAsync(IEnumerable<DiaryEntry> entries, ExportFormat format)
    {
        var results = new List<string>();
        var entryList = entries.ToList();
        
        foreach (var entry in entryList)
        {
            var content = format switch
            {
                ExportFormat.Text => await ExportToTextAsync(entry),
                ExportFormat.Markdown => await ExportToMarkdownAsync(entry),
                ExportFormat.Json => await ExportToJsonAsync(entry),
                _ => throw new ArgumentException($"不支持的导出格式：{format}")
            };
            
            if (format == ExportFormat.Json)
                results.Add(content);
            else
                results.Add($"---\n\n{content}");
        }
        
        return format == ExportFormat.Json 
            ? $"[\n{string.Join(",\n", results)}\n]" 
            : string.Join("\n\n", results);
    }
    
    public async Task ShareAsync(string content, string title)
    {
#if ANDROID
        var intent = new Android.Content.Intent(Android.Content.Intent.ActionSend);
        intent.PutExtra(Android.Content.Intent.ExtraText, content);
        intent.PutExtra(Android.Content.Intent.ExtraSubject, title);
        intent.SetType("text/plain");
        
        var sharesheet = Android.Content.Intent.CreateChooser(intent, "分享到");
        sharesheet.AddFlags(Android.Content.ActivityFlags.NewTask);
        
        Android.Content.Intent shareIntent = sharesheet;
        Platform.CurrentActivity.StartActivity(shareIntent);
        
#elif IOS
        var items = new NSObject[] { new Foundation.NSString(content), new Foundation.NSString(title) };
        var activityController = new UIActivityViewController(items, null);
        
        var viewController = Platform.GetCurrentUIViewController();
        if (viewController != null)
        {
            viewController.PresentViewController(activityController, true, null);
        }
        
#elif WINDOWS
        // Windows 暂时不支持分享
        await Task.CompletedTask;
#endif
    }
    
    public async Task<string> ExportAllToZipAsync()
    {
        // TODO: 实现全量导出到 ZIP
        return await Task.FromResult(string.Empty);
    }
    
    public async Task<bool> ImportFromZipAsync(string zipFilePath)
    {
        // TODO: 实现从 ZIP 导入
        return await Task.FromResult(false);
    }
    
    private static string FormatDuration(int seconds)
    {
        var minutes = seconds / 60;
        var remainingSeconds = seconds % 60;
        return minutes > 0 ? $"{minutes}分{remainingSeconds}秒" : $"{remainingSeconds}秒";
    }
    
    private static string GetMoodEmoji(MoodType mood)
    {
        return mood switch
        {
            MoodType.Happy => "😊 开心",
            MoodType.Sad => "😢 难过",
            MoodType.Angry => "😠 生气",
            MoodType.Excited => "🤩 兴奋",
            MoodType.Calm => "😌 平静",
            MoodType.Anxious => "😰 焦虑",
            MoodType.Tired => "😫 疲惫",
            _ => "😐 普通"
        };
    }
}
