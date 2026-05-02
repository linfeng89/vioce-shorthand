namespace VoiceDiary.Services;

public interface ITranscriptionQueueService
{
    Task StartAsync(CancellationToken cancellationToken);
    Task EnqueueAsync(DiaryEntry entry);
    Task PrioritizeAsync(string entryId);
    void Stop();
    int PendingCount { get; }
    bool IsProcessing { get; }
}

public class TranscriptionQueueService : ITranscriptionQueueService
{
    private readonly IDatabaseService _databaseService;
    private readonly ISpeechRecognizer _speechRecognizer;
    private readonly IStorageService _storageService;
    private readonly ILogger<TranscriptionQueueService>? _logger;
    
    private readonly PriorityQueue<DiaryEntry, DateTime> _queue = new();
    private CancellationTokenSource? _cts;
    private Task? _processorTask;
    private bool _isProcessing;

    public int PendingCount => _queue.Count;
    public bool IsProcessing => _isProcessing;

    public TranscriptionQueueService(
        IDatabaseService databaseService,
        ISpeechRecognizer speechRecognizer,
        IStorageService storageService,
        ILogger<TranscriptionQueueService>? logger = null)
    {
        _databaseService = databaseService;
        _speechRecognizer = speechRecognizer;
        _storageService = storageService;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (_processorTask != null)
            return;

        await InitializeQueueAsync();
        
        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _processorTask = ProcessQueueAsync(_cts.Token);
        
        _logger?.LogInformation("转写队列服务已启动");
    }

    public void Stop()
    {
        _cts?.Cancel();
        _processorTask = null;
        _isProcessing = false;
        _logger?.LogInformation("转写队列服务已停止");
    }

    public Task EnqueueAsync(DiaryEntry entry)
    {
        _queue.Enqueue(entry, entry.CreatedAt);
        _logger?.LogDebug("日记 {EntryId} 已加入转写队列", entry.Id);
        return Task.CompletedTask;
    }

    public async Task PrioritizeAsync(string entryId)
    {
        var entry = await _databaseService.GetEntryByIdAsync(entryId);
        if (entry == null)
            return;

        var newTime = DateTime.MinValue;
        _queue.Enqueue(entry, newTime);
        _logger?.LogInformation("日记 {EntryId} 已优先转写", entryId);
    }

    private async Task InitializeQueueAsync()
    {
        var entries = await _databaseService.GetConnectionAsync()
            .then(conn => conn.Table<DiaryEntry>()
                .Where(e => !e.IsTranscribed && !e.IsDeleted)
                .OrderBy(e => e.CreatedAt)
                .ToListAsync());

        foreach (var entry in entries)
        {
            _queue.Enqueue(entry, entry.CreatedAt);
        }

        _logger?.LogInformation("初始化转写队列，共 {Count} 个待转写条目", _queue.Count);
    }

    private async Task ProcessQueueAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            if (_queue.Count == 0)
            {
                await Task.Delay(5000, cancellationToken);
                await CheckForNewEntriesAsync();
                continue;
            }

            if (!_queue.TryDequeue(out var entry, out _))
                continue;

            _isProcessing = true;
            await ProcessEntryAsync(entry, cancellationToken);
            _isProcessing = false;
        }
    }

    private async Task ProcessEntryAsync(DiaryEntry entry, CancellationToken cancellationToken)
    {
        try
        {
            var audioPath = Path.Combine(_storageService.AppAudioPath, entry.AudioFileName);
            
            if (!File.Exists(audioPath))
            {
                _logger?.LogWarning("音频文件不存在：{Path}", audioPath);
                entry.TranscribeAttempts++;
                entry.TranscribeError = "音频文件不存在";
                await _databaseService.SaveEntryAsync(entry);
                return;
            }

            _logger?.LogInformation("开始转写日记 {EntryId}", entry.Id);
            
            var text = await _speechRecognizer.RecognizeAsync(audioPath);
            
            if (!string.IsNullOrEmpty(text))
            {
                entry.TranscribedText = text;
                entry.IsTranscribed = true;
                entry.TranscribeError = string.Empty;
                await _databaseService.SaveEntryAsync(entry);
                _logger?.LogInformation("日记 {EntryId} 转写完成", entry.Id);
            }
            else
            {
                throw new Exception("转写结果为空");
            }
        }
        catch (Exception ex)
        {
            entry.TranscribeAttempts++;
            entry.TranscribeError = ex.Message;
            
            if (entry.TranscribeAttempts >= 3)
            {
                _logger?.LogError("日记 {EntryId} 转写失败（已达最大重试次数）: {Error}", entry.Id, ex.Message);
            }
            else
            {
                _logger?.LogWarning("日记 {EntryId} 转写失败（第 {Attempt} 次尝试）: {Error}", entry.Id, entry.TranscribeAttempts, ex.Message);
                _queue.Enqueue(entry, DateTime.Now);
            }
            
            await _databaseService.SaveEntryAsync(entry);
        }
    }

    private async Task CheckForNewEntriesAsync()
    {
        var entries = await _databaseService.GetConnectionAsync()
            .then(conn => conn.Table<DiaryEntry>()
                .Where(e => !e.IsTranscribed && !e.IsDeleted)
                .OrderBy(e => e.CreatedAt)
                .ToListAsync());

        foreach (var entry in entries)
        {
            if (!_queue.Contains(entry))
            {
                _queue.Enqueue(entry, entry.CreatedAt);
                _logger?.LogDebug("发现新条目 {EntryId}，已加入队列", entry.Id);
            }
        }
    }
}

public static class TaskExtensions
{
    public static async Task<T> Then<T>(this Task<T> task, Func<T, T> continuation)
    {
        var result = await task;
        return continuation(result);
    }
}
