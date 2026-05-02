namespace VoiceDiary.Services;

public interface IToastService
{
    Task<string> ShowAsync(string message, string? actionText = null, TimeSpan? timeout = null);
    void Show(string message, int durationMs = 2000);
}

public class ToastService : IToastService
{
    private TaskCompletionSource<string>? _tcs;
    
    public async Task<string> ShowAsync(string message, string? actionText = null, TimeSpan? timeout = null)
    {
        _tcs = new TaskCompletionSource<string>();
        
        var timeoutValue = timeout ?? TimeSpan.FromSeconds(3);
        
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            try
            {
                var toast = new Toast(message, actionText, timeoutValue);
                await toast.Show();
                
                // 等待用户操作或超时
                var delayTask = Task.Delay(timeoutValue);
                var completedTask = await Task.WhenAny(_tcs.Task, delayTask);
                
                if (completedTask == delayTask)
                {
                    _tcs.TrySetResult("timeout");
                }
            }
            catch (Exception ex)
            {
                _tcs.TrySetException(ex);
            }
            finally
            {
                _tcs = null;
            }
        });
        
        return await _tcs.Task;
    }
    
    public void Show(string message, int durationMs = 2000)
    {
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            try
            {
                var toast = new Toast(message, null, TimeSpan.FromMilliseconds(durationMs));
                await toast.Show();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Toast error: {ex}");
            }
        });
    }
}

// Simple Toast implementation
public class Toast
{
    private readonly string _message;
    private readonly string? _actionText;
    private readonly TimeSpan _duration;
    private Window? _window;
    
    public Toast(string message, string? actionText, TimeSpan duration)
    {
        _message = message;
        _actionText = actionText;
        _duration = duration;
    }
    
    public async Task Show()
    {
        var mainWindow = Application.Current?.Windows[0];
        if (mainWindow == null) return;
        
        _window = mainWindow;
        
        var grid = new Grid
        {
            VerticalOptions = LayoutOptions.End,
            HorizontalOptions = LayoutOptions.Center,
            Margin = new Thickness(0, 0, 0, 100),
            Padding = 20,
            BackgroundColor = Colors.Black.WithAlpha(0.8),
        };
        
        grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
        if (!string.IsNullOrEmpty(_actionText))
        {
            grid.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(10)));
            grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
        }
        
        var messageLabel = new Label
        {
            Text = _message,
            TextColor = Colors.White,
            FontSize = 14,
            VerticalOptions = LayoutOptions.Center,
        };
        
        Grid.SetColumn(messageLabel, 0);
        grid.Add(messageLabel);
        
        if (!string.IsNullOrEmpty(_actionText))
        {
            var actionButton = new Button
            {
                Text = _actionText,
                TextColor = Colors.Yellow,
                BackgroundColor = Colors.Transparent,
                FontSize = 14,
                VerticalOptions = LayoutOptions.Center,
                Padding = 10,
            };
            
            actionButton.Clicked += (s, e) =>
            {
                // 用户点击了操作按钮
                if (_tcs != null)
                {
                    _tcs.TrySetResult("action");
                }
            };
            
            Grid.SetColumn(actionButton, 2);
            grid.Add(actionButton);
        }
        
        _window.Page?.OverlayContent?.Add(grid);
        
        await Task.Delay(_duration);
        
        _window.Page?.OverlayContent?.Remove(grid);
    }
}
