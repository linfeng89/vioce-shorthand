namespace VoiceDiary.Services;

public class BiometricAuthService : IBiometricAuthService
{
    public bool IsAvailable => true;

    public async Task<bool> AuthenticateAsync(string reason)
    {
        return await Task.FromResult(true);
    }

    public Task<bool> CheckBiometricEnrollmentAsync()
    {
        return Task.FromResult(true);
    }
}
