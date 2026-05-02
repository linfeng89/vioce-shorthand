using Microsoft.Extensions.DependencyInjection;

namespace VoiceDiary.Services;

public static class ServiceExtensions
{
    public static void AddDatabaseService(this IServiceCollection services)
    {
        services.AddSingleton<IDatabaseService, DatabaseService>();
    }

    public static void AddAudioServices(this IServiceCollection services)
    {
        services.AddSingleton<IAudioRecorder, AudioRecorder>();
        services.AddSingleton<ISpeechRecognizer, WhisperRecognizer>();
        services.AddSingleton<IAudioCompressor, AudioCompressor>();
    }

    public static void AddStorageServices(this IServiceCollection services)
    {
        services.AddSingleton<IStorageService, StorageService>();
        services.AddSingleton<IBackupService, BackupService>();
        services.AddSingleton<IExportService, ExportService>();
    }

    public static void AddPlatformServices(this IServiceCollection services)
    {
        services.AddSingleton<IBiometricAuthService, BiometricAuthService>();
    }
}
