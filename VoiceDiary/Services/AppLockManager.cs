namespace VoiceDiary.Services;

public enum AppAccessScenario
{
    AppLaunch,
    ReturnFromBackground,
    ViewDiaryDetail,
    PlaybackAudio,
    QuickRecord,
    RecordingInBackground
}

public interface IAppLockManager
{
    bool ShouldRequireAuth(AppAccessScenario scenario);
    void RecordSuccessfulAuth();
    Task<SecuritySettings> GetSettingsAsync();
    Task SaveSettingsAsync(SecuritySettings settings);
}

public class AppLockManager : IAppLockManager
{
    private readonly IDatabaseService _databaseService;
    private SecuritySettings? _settings;
    
    public AppLockManager(IDatabaseService databaseService)
    {
        _databaseService = databaseService;
    }
    
    public async Task<SecuritySettings> GetSettingsAsync()
    {
        if (_settings != null)
            return _settings;
        
        var db = await _databaseService.GetConnectionAsync();
        _settings = await db.Table<SecuritySettings>().FirstOrDefaultAsync();
        
        if (_settings == null)
        {
            _settings = new SecuritySettings();
            // 不自动保存，只在首次读取时返回默认值
        }
        
        return _settings;
    }
    
    public async Task SaveSettingsAsync(SecuritySettings settings)
    {
        var db = await _databaseService.GetConnectionAsync();
        
        if (settings.Id == 0)
        {
            await db.InsertAsync(settings);
        }
        else
        {
            await db.UpdateAsync(settings);
        }
        
        _settings = settings;
    }
    
    public bool ShouldRequireAuth(AppAccessScenario scenario)
    {
        if (_settings == null || !_settings.IsAppLockEnabled)
            return false;
        
        // 快捷入口免验证
        if (scenario == AppAccessScenario.QuickRecord)
            return false;
        
        // 录音中免验证
        if (scenario == AppAccessScenario.RecordingInBackground)
            return false;
        
        // 从未解锁过，需要验证
        if (_settings.LastUnlockTime == null)
            return true;
        
        // 检查超时时间
        var elapsed = DateTime.Now - _settings.LastUnlockTime.Value;
        
        return _settings.Timeout switch
        {
            AppLockTimeout.Immediately => true,
            AppLockTimeout.After30Seconds => elapsed > TimeSpan.FromSeconds(30),
            AppLockTimeout.After1Minute => elapsed > TimeSpan.FromMinutes(1),
            AppLockTimeout.After5Minutes => elapsed > TimeSpan.FromMinutes(5),
            AppLockTimeout.Never => false,
            _ => true
        };
    }
    
    public async Task RecordSuccessfulAuthAsync()
    {
        if (_settings != null)
        {
            _settings.LastUnlockTime = DateTime.Now;
            await SaveSettingsAsync(_settings);
        }
    }
}
