namespace VoiceDiary.Models;

public enum AppLockTimeout
{
    Immediately = 0,
    After30Seconds = 1,
    After1Minute = 2,
    After5Minutes = 3,
    Never = 4
}

public class SecuritySettings
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }
    
    public bool IsAppLockEnabled { get; set; }
    
    public AppLockTimeout Timeout { get; set; } = AppLockTimeout.Immediately;
    
    public DateTime? LastUnlockTime { get; set; }
    
    // 预留密码字段（未来实现）
    public string? PinCode { get; set; }
}
