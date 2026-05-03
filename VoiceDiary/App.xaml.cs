namespace VoiceDiary;

public partial class App : Application
{
    private readonly IServiceProvider _serviceProvider;
    private CancellationTokenSource? _appCts;
    private LockScreenViewModel? _lockScreenViewModel;
    private LockScreenOverlay? _lockScreenOverlay;

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

        _appCts = new CancellationTokenSource();
        
        InitializeServicesAsync();
        
        // 初始化锁屏
        InitializeLockScreen();
        
        MainPage = new NavigationPage(new RecordPage(serviceProvider.GetRequiredService<RecordViewModel>()));
    }

    private async void InitializeServicesAsync()
    {
        try
        {
            var databaseService = _serviceProvider.GetRequiredService<IDatabaseService>();
            await databaseService.InitializeAsync();

            var ftsService = new Fts5Service(databaseService);
            await ftsService.InitializeAsync();

            var speechRecognizer = _serviceProvider.GetRequiredService<ISpeechRecognizer>();
            await speechRecognizer.InitializeAsync();

            var transcriptionQueue = _serviceProvider.GetRequiredService<ITranscriptionQueueService>();
            await transcriptionQueue.StartAsync(_appCts.Token);
            
            // 初始化回收站自动清理
            var trashService = _serviceProvider.GetRequiredService<ITrashService>();
            await trashService.AutoCleanupAsync(30);
            
            // 启动自动备份
            var automaticBackup = _serviceProvider.GetRequiredService<AutomaticBackupService>();
            automaticBackup.Start();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"服务初始化失败：{ex.Message}");
        }
    }

    protected override void OnSleep()
    {
        base.OnSleep();
        _appCts?.Cancel();
    }

    protected override async void OnResume()
    {
        base.OnResume();
        
        if (_appCts?.IsCancellationRequested == true)
        {
            _appCts = new CancellationTokenSource();
            var transcriptionQueue = _serviceProvider.GetRequiredService<ITranscriptionQueueService>();
            _ = transcriptionQueue.StartAsync(_appCts.Token);
        }
        
        // 从后台恢复时检查是否需要验证
        await CheckAndShowLockScreenAsync(AppAccessScenario.ReturnFromBackground);
    }

    protected override void OnStop()
    {
        base.OnStop();
        _appCts?.Dispose();
        
        var speechRecognizer = _serviceProvider.GetRequiredService<ISpeechRecognizer>();
        speechRecognizer.Release();
    }

    public static void NavigateToDiaryList()
    {
        Current.MainPage?.Navigation.PushAsync(new DiaryListPage(Current.Services.GetRequiredService<DiaryListViewModel>()));
    }

    public static void NavigateToSettings()
    {
        Current.MainPage?.Navigation.PushAsync(new SettingsPage(Current.Services.GetRequiredService<SettingsViewModel>()));
    }
    
    private void InitializeLockScreen()
    {
        _lockScreenViewModel = _serviceProvider.GetRequiredService<LockScreenViewModel>();
        _lockScreenOverlay = new LockScreenOverlay(_lockScreenViewModel);
        
        // 将锁屏添加到主页面
        if (MainPage is NavigationPage navPage && navPage.CurrentPage != null)
        {
            var grid = new Grid();
            grid.Children.Add(navPage.CurrentPage.Content);
            grid.Children.Add(_lockScreenOverlay);
            navPage.CurrentPage.Content = grid;
        }
    }
    
    private async Task CheckAndShowLockScreenAsync(AppAccessScenario scenario)
    {
        try
        {
            var appLockManager = _serviceProvider.GetRequiredService<IAppLockManager>();
            
            if (appLockManager.ShouldRequireAuth(scenario))
            {
                await _lockScreenViewModel!.ShowAsync();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Lock screen check error: {ex}");
        }
    }
}
