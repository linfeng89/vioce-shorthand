namespace VoiceDiary.Services;

public enum BiometricAuthResult
{
    Success,
    Failure,
    UserFallback,
    UserCancel,
    SystemDisable,
    NotEnrolled,
    NotImplemented
}

public interface IBiometricAuthService
{
    Task<bool> IsAvailableAsync();
    Task<BiometricAuthResult> AuthenticateAsync(string reason);
    event EventHandler<BiometricAuthResult> OnAuthenticationResult;
}

public class BiometricAuthService : IBiometricAuthService
{
    public event EventHandler<BiometricAuthResult>? OnAuthenticationResult;
    
    public Task<bool> IsAvailableAsync()
    {
        try
        {
            // CommunityToolkit.Maui 提供了 BiometricConstants
            // 但由于版本兼容性问题，我们使用简单的平台检测
            var isAvailable = 
#if ANDROID || __IOS__
                true;  // 实际应该检测硬件
#else
                false;
#endif
            return Task.FromResult(isAvailable);
        }
        catch
        {
            return Task.FromResult(false);
        }
    }
    
    public async Task<BiometricAuthResult> AuthenticateAsync(string reason)
    {
        try
        {
            // TODO: 使用 CommunityToolkit.Maui 的 BiometricAuthentication
            // 由于版本兼容，暂时返回 Success 用于开发测试
            
            // 实际实现：
            // var request = new BiometricAuthenticationRequest(reason);
            // var result = await BiometricAuthentication.AuthenticateAsync(request);
            // return result.Authenticated ? BiometricAuthResult.Success : BiometricAuthResult.Failure;
            
            // 临时实现：模拟验证成功
            await Task.Delay(1000); // 模拟验证延迟
            return BiometricAuthResult.Success;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Biometric auth error: {ex}");
            return BiometricAuthResult.Failure;
        }
    }
}
