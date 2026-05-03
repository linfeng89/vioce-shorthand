namespace VoiceDiary.Services;

public interface IQuickRecordService
{
    Task StartQuickRecordAsync();
    Task StopQuickRecordAsync();
    bool IsRecording { get; }
    event EventHandler<bool> OnRecordingStateChanged;
}

public class QuickRecordService : IQuickRecordService
{
    private readonly IAudioRecorder _audioRecorder;
    private readonly ISpeechRecognizer _speechRecognizer;
    private readonly ITranscriptionQueueService _queueService;
    private readonly IDatabaseService _databaseService;
    private readonly IAppLockManager _appLockManager;
    
    private bool _isRecording;
    
    public bool IsRecording => _isRecording;
    public event EventHandler<bool> OnRecordingStateChanged;
    
    public QuickRecordService(
        IAudioRecorder audioRecorder,
        ISpeechRecognizer speechRecognizer,
        ITranscriptionQueueService queueService,
        IDatabaseService databaseService,
        IAppLockManager appLockManager)
    {
        _audioRecorder = audioRecorder;
        _speechRecognizer = speechRecognizer;
        _queueService = queueService;
        _databaseService = databaseService;
        _appLockManager = appLockManager;
    }
    
    public async Task StartQuickRecordAsync()
    {
        if (_isRecording)
            return;
        
        // 检查是否需要验证（快捷录音通常免验证）
        if (_appLockManager.ShouldRequireAuth(AppAccessScenario.QuickRecord))
        {
            // 如果需要验证但用户选择快捷录音，跳过验证直接开始
            // 这是快捷录音的特殊逻辑
        }
        
        try
        {
            var filePath = await _audioRecorder.StartRecordingAsync();
            _isRecording = true;
            OnRecordingStateChanged?.Invoke(this, true);
            
            // 录音完成后自动处理
            _audioRecorder.RecordingStopped += async (s, e) =>
            {
                await HandleRecordingStoppedAsync(e.AudioFilePath);
            };
        }
        catch (Exception ex)
        {
            _isRecording = false;
            OnRecordingStateChanged?.Invoke(this, false);
            throw new InvalidOperationException($"启动快捷录音失败：{ex.Message}", ex);
        }
    }
    
    public async Task StopQuickRecordAsync()
    {
        if (!_isRecording)
            return;
        
        try
        {
            var filePath = await _audioRecorder.StopRecordingAsync();
            await HandleRecordingStoppedAsync(filePath!);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"停止快捷录音失败：{ex.Message}", ex);
        }
    }
    
    private async Task HandleRecordingStoppedAsync(string audioFilePath)
    {
        _isRecording = false;
        OnRecordingStateChanged?.Invoke(this, false);
        
        // 加入转录队列
        await _queueService.EnqueueAsync(audioFilePath);
    }
}
