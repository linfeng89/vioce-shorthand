namespace VoiceDiary;

public partial class App : Application
{
    private readonly IServiceProvider _serviceProvider;

    public App(IServiceProvider serviceProvider)
    {
        InitializeComponent();

        _serviceProvider = serviceProvider;

        Microsoft.Maui.Handlers.EntryHandler.Mapper.AppendToMapping("NoUnderline", (handler, view) =>
        {
#if __ANDROID__
            handler.PlatformEditText.Background = null;
#elif __IOS__
            handler.PlatformViewController.TextField.BorderStyle = UIKit.UITextFieldBorderStyle.None;
#endif
        });

        var databaseService = serviceProvider.GetRequiredService<IDatabaseService>();
        _ = databaseService.InitializeAsync();

        var speechRecognizer = serviceProvider.GetRequiredService<ISpeechRecognizer>();
        _ = speechRecognizer.InitializeAsync();

        MainPage = new NavigationPage(new RecordPage(serviceProvider.GetRequiredService<RecordViewModel>()));
    }

    public static void NavigateToDiaryList()
    {
        Current.MainPage?.Navigation.PushAsync(new DiaryListPage(Current.Services.GetRequiredService<DiaryListViewModel>()));
    }

    public static void NavigateToSettings()
    {
        Current.MainPage?.Navigation.PushAsync(new SettingsPage(Current.Services.GetRequiredService<SettingsViewModel>()));
    }
}
