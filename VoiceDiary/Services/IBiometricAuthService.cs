namespace VoiceDiary.Services;

public interface IBiometricAuthService
{
    bool IsAvailable { get; }
    Task<bool> AuthenticateAsync(string reason);
    Task<bool> CheckBiometricEnrollmentAsync();
}
