namespace VoiceDiary.ViewModels;

public class RecordViewModel : BaseViewModel
{
    private readonly IAudioRecorder _audioRecorder;
    private readonly ISpeechRecognizer _speechRecognizer;
    private readonly IDatabaseService _databaseService;
    private readonly IStorageService _storageService;

    private bool _isRecording;
    private TimeSpan _recordingDuration;
    private string _recordingStatus;
    private bool _isLocked;
    private bool _isCancelling;

    public RecordViewModel(
        IAudioRecorder audioRecorder,
        ISpeechRecognizer speechRecognizer,
        IDatabaseService databaseService,
        IStorageService storageService)
    {
        _audioRecorder = audioRecorder;
        _speechRecognizer = speechRecognizer;
        _databaseService = databaseService;
        _storageService = storageService;

        _recordingStatus = "按住录音";
    }

    public bool IsRecording
    {
        get => _isRecording;
        set => SetProperty(ref _isRecording, value);
    }

    public TimeSpan RecordingDuration
    {
        get => _recordingDuration;
        set => SetProperty(ref _recordingDuration, value);
    }

    public string RecordingStatus
    {
        get => _recordingStatus;
        set => SetProperty(ref _recordingStatus, value);
    }

    public bool IsLocked
    {
        get => _isLocked;
        set => SetProperty(ref _isLocked, value);
    }

    public bool IsCancelling
    {
        get => _isCancelling;
        set => SetProperty(ref _isCancelling, value);
    }

    public Command StartRecordingCommand => new Command(async () => await StartRecordingAsync());
    public Command StopRecordingCommand => new Command(async () => await StopRecordingAsync());
    public Command CancelRecordingCommand => new Command(async () => await CancelRecordingAsync());
    public Command LockRecordingCommand => new Command(() => IsLocked = true);

    private async Task StartRecordingAsync()
    {
        if (IsRecording)
            return;

        try
        {
            await _audioRecorder.StartRecordingAsync();
            IsRecording = true;
            RecordingStatus = "松开停止 • 上滑锁定";
            StartRecordingTimer();
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("错误", $"无法开始录音：{ex.Message}", "确定");
        }
    }

    private async Task StopRecordingAsync()
    {
        if (!IsRecording)
            return;

        try
        {
            var (filePath, duration) = await _audioRecorder.StopRecordingAsync();
            IsRecording = false;
            IsLocked = false;

            if (duration < 1)
            {
                RecordingStatus = "录音时间过短";
                await Task.Delay(1000);
                RecordingStatus = "按住录音";
                return;
            }

            await SaveRecordingAsync(filePath, duration);
            RecordingStatus = "按住录音";
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("错误", $"保存录音失败：{ex.Message}", "确定");
        }
    }

    private async Task CancelRecordingAsync()
    {
        if (!IsRecording)
            return;

        await _audioRecorder.CancelRecordingAsync();
        IsRecording = false;
        IsLocked = false;
        RecordingStatus = "按住录音";
    }

    private void StartRecordingTimer()
    {
        Device.StartTimer(TimeSpan.FromMilliseconds(100), () =>
        {
            if (IsRecording)
            {
                RecordingDuration = _audioRecorder.CurrentDuration;
                return true;
            }
            return false;
        });
    }

    private async Task SaveRecordingAsync(string filePath, int duration)
    {
        var fileName = GenerateFileName();

        var entry = new DiaryEntry
        {
            AudioFileName = fileName,
            DurationSeconds = duration,
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now,
            IsTranscribed = false,
            TranscribeAttempts = 0
        };

        await _databaseService.SaveEntryAsync(entry);

        var destPath = Path.Combine(_storageService.AppAudioPath, fileName);
        File.Copy(filePath, destPath, true);

        await QueueTranscriptionAsync(entry);
    }

    private string GenerateFileName()
    {
        var now = DateTime.Now;
        var counter = 1;
        string fileName;

        do
        {
            fileName = $"{now:yyyyMMdd_HHmm}_{counter:D3}.wav";
            counter++;
        } while (File.Exists(Path.Combine(_storageService.AppAudioPath, fileName)));

        return fileName;
    }

    private async Task QueueTranscriptionAsync(DiaryEntry entry)
    {
        await Task.Run(async () =>
        {
            try
            {
                if (!_speechRecognizer.IsReady)
                    await _speechRecognizer.InitializeAsync();

                var audioPath = Path.Combine(_storageService.AppAudioPath, entry.AudioFileName);
                var text = await _speechRecognizer.RecognizeAsync(audioPath);

                if (!string.IsNullOrEmpty(text))
                {
                    entry.TranscribedText = text;
                    entry.IsTranscribed = true;
                    await _databaseService.SaveEntryAsync(entry);
                }
            }
            catch (Exception ex)
            {
                entry.TranscribeAttempts++;
                entry.TranscribeError = ex.Message;
                await _databaseService.SaveEntryAsync(entry);
            }
        });
    }

    public void HandlePanUpdated(float deltaY, float totalY)
    {
        const double lockThreshold = -50;
        const double cancelThreshold = -200;

        if (IsLocked)
            return;

        if (totalY < cancelThreshold)
        {
            IsCancelling = true;
            RecordingStatus = "松开取消录音";
        }
        else if (deltaY < lockThreshold)
        {
            IsLocked = true;
            RecordingStatus = "锁定中 • 点击停止";
            HapticFeedback.Perform(HapticFeedbackType.Click);
        }
        else
        {
            IsCancelling = false;
            if (IsRecording)
                RecordingStatus = "松开停止 • 上滑锁定";
        }
    }
}
