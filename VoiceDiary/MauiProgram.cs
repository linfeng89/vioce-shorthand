using VoiceDiary.Services;
using VoiceDiary.Models;
using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;

namespace VoiceDiary;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

#if DEBUG
        builder.Logging.AddDebug();
#endif

        RegisterServices(builder.Services);
        RegisterViewModels(builder.Services);
        RegisterPages(builder.Services);

        return builder.Build();
    }

    private static void RegisterServices(IServiceCollection services)
    {
        services.AddSingleton<IDatabaseService, DatabaseService>();
        services.AddSingleton<IAudioRecorder, AudioRecorder>();
        services.AddSingleton<IAudioPlayer, AudioPlayer>();
        services.AddSingleton<ISpeechRecognizer, WhisperRecognizer>();
        services.AddSingleton<IAudioCompressor, AudioCompressor>();
        services.AddSingleton<IStorageService, StorageService>();
        services.AddSingleton<IBackupService, BackupService>();
        services.AddSingleton<IExportService, ExportService>();
        services.AddSingleton<IBiometricAuthService, BiometricAuthService>();
        services.AddSingleton<ITranscriptionQueueService, TranscriptionQueueService>();
        services.AddSingleton<ISearchService, SearchService>();
    }

    private static void RegisterViewModels(IServiceCollection services)
    {
        services.AddTransient<RecordViewModel>();
        services.AddTransient<DiaryListViewModel>();
        services.AddTransient<DiaryDetailViewModel>();
        services.AddTransient<SettingsViewModel>();
        services.AddTransient<TrashViewModel>();
        services.AddTransient<SearchViewModel>();
    }

    private static void RegisterPages(IServiceCollection services)
    {
        services.AddTransient<RecordPage>();
        services.AddTransient<DiaryListPage>();
        services.AddTransient<DiaryDetailPage>();
        services.AddTransient<SettingsPage>();
        services.AddTransient<TrashPage>();
        services.AddTransient<SearchPage>();
    }
}
