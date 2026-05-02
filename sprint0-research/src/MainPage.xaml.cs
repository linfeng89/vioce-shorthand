using VoiceDiary.Sprint0.Services;

namespace VoiceDiary.Sprint0;

public partial class MainPage : ContentPage
{
    private readonly SpeechRecognizerService _recognizerService;
    private readonly PerformanceTestService _testService;

    public MainPage(SpeechRecognizerService recognizerService, PerformanceTestService testService)
    {
        InitializeComponent();
        _recognizerService = recognizerService;
        _testService = testService;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            await DisplayAlert("提示", "建议首次启动时先执行模型加载测试", "OK");
        });
    }

    private async void OnModelLoadTestClicked(object sender, EventArgs e)
    {
        await RunTestAsync("模型加载", async () =>
        {
            return await _testService.TestModelLoadingAsync();
        });
    }

    private async void OnTranscriptionTestClicked(object sender, EventArgs e)
    {
        await RunMultipleTestsAsync(async () =>
        {
            return await _testService.TestTranscriptionAsync();
        });
    }

    private async void OnLowMemoryTestClicked(object sender, EventArgs e)
    {
        await RunTestAsync("低内存释放", async () =>
        {
            return await _testService.TestLowMemoryHandlingAsync();
        });
    }

    private async Task RunTestAsync(string testName, Func<Task<PerfResult>> testFunc)
    {
        try
        {
            // 清空结果
            ResultsStack.Clear();
            ResultsStack.Add(new Label { Text = $"运行 {testName}...", TextColor = Colors.Blue });

            // 执行测试
            var result = await testFunc();

            // 显示结果
            ResultsStack.Clear();
            ResultsStack.Add(CreateResultView(testName, result));
        }
        catch (Exception ex)
        {
            ResultsStack.Clear();
            ResultsStack.Add(new Label
            {
                Text = $"❌ {testName} 失败\n{ex.Message}",
                TextColor = Colors.Red
            });
        }
    }

    private async Task RunMultipleTestsAsync(Func<Task<List<PerfResult>>> testFunc)
    {
        try
        {
            ResultsStack.Clear();
            ResultsStack.Add(new Label { Text = "运行转写性能测试...", TextColor = Colors.Blue });

            var results = await testFunc();

            ResultsStack.Clear();

            foreach (var result in results)
            {
                ResultsStack.Add(CreateResultView(result.Name, result));
                ResultsStack.Add(new BoxView { HeightRequest = 1, Color = Colors.LightGray });
            }
        }
        catch (Exception ex)
        {
            ResultsStack.Clear();
            ResultsStack.Add(new Label
            {
                Text = $"❌ 测试失败\n{ex.Message}",
                TextColor = Colors.Red
            });
        }
    }

    private View CreateResultView(string name, PerfResult result)
    {
        var stack = new VerticalStackLayout { Spacing = 5 };

        var title = result.Success ? "✅" : "❌";
        title += $" {name}";

        stack.Add(new Label
        {
            Text = title,
            FontAttributes = result.Success ? FontAttributes.Bold : FontAttributes.None,
            TextColor = result.Success ? Colors.Green : Colors.Red
        });

        stack.Add(new Label
        {
            Text = result.Details,
            TextColor = Colors.Black,
            FontSize = 14
        });

        return stack;
    }
}
