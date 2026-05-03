namespace VoiceDiary.ViewModels;

public partial class SecuritySettingsViewModel : BaseViewModel
{
    private readonly IBiometricAuthService _biometricService;
    private readonly IAppLockManager _appLockManager;
    private SecuritySettings _settings = new();
    
    private bool _isAppLockEnabled;
    private string _selectedTimeout = "立即";
    private string _biometricStatusText = "检测中...";
    private string _biometricStatusIcon = "⏳";
    private Color _pickerTextColor = Colors.Gray;
    private string _timeoutDescription = "应用进入后台后立即锁定";
    
    public SecuritySettingsViewModel(
        IBiometricAuthService biometricService,
        IAppLockManager appLockManager)
    {
        _biometricService = biometricService;
        _appLockManager = appLockManager;
    }
    
    public bool IsAppLockEnabled
    {
        get => _isAppLockEnabled;
        set => SetProperty(ref _isAppLockEnabled, value);
    }
    
    public string SelectedTimeout
    {
        get => _selectedTimeout;
        set
        {
            if (SetProperty(ref _selectedTimeout, value))
            {
                UpdateTimeoutDescription();
            }
        }
    }
    
    public string BiometricStatusText
    {
        get => _biometricStatusText;
        set => SetProperty(ref _biometricStatusText, value);
    }
    
    public string BiometricStatusIcon
    {
        get => _biometricStatusIcon;
        set => SetProperty(ref _biometricStatusIcon, value);
    }
    
    public Color PickerTextColor
    {
        get => _pickerTextColor;
        set => SetProperty(ref _pickerTextColor, value);
    }
    
    public string TimeoutDescription
    {
        get => _timeoutDescription;
        set => SetProperty(ref _timeoutDescription, value);
    }
    
    public Command LoadSettingsCommand => new Command(async () => await LoadSettingsAsync());
    public Command TestBiometricCommand => new Command(async () => await TestBiometricAsync());
    
    private async Task LoadSettingsAsync()
    {
        try
        {
            IsBusy = true;
            
            // 加载设置
            _settings = await _appLockManager.GetSettingsAsync();
            
            IsAppLockEnabled = _settings.IsAppLockEnabled;
            
            // 设置超时选项
            SelectedTimeout = _settings.Timeout switch
            {
                AppLockTimeout.After30Seconds => "30 秒后",
                AppLockTimeout.After1Minute => "1 分钟后",
                AppLockTimeout.After5Minutes => "5 分钟后",
                AppLockTimeout.Never => "从不",
                _ => "立即"
            };
            
            // 检测生物识别状态
            await CheckBiometricStatusAsync();
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("错误", $"加载设置失败：{ex.Message}", "确定");
        }
        finally
        {
            IsBusy = false;
        }
    }
    
    partial void OnIsAppLockEnabledChanged(bool value)
    {
        PickerTextColor = value ? Colors.Black : Colors.Gray;
        OnPropertyChanged(nameof(PickerTextColor));
    }
    
    private void UpdateTimeoutDescription()
    {
        TimeoutDescription = SelectedTimeout switch
        {
            "30 秒后" => "应用进入后台 30 秒后锁定",
            "1 分钟后" => "应用进入后台 1 分钟后锁定",
            "5 分钟后" => "应用进入后台 5 分钟后锁定",
            "从不" => "应用不会自动锁定",
            _ => "应用进入后台立即锁定"
        };
    }
    
    private async Task CheckBiometricStatusAsync()
    {
        try
        {
            var isAvailable = await _biometricService.IsAvailableAsync();
            
            if (isAvailable)
            {
                BiometricStatusIcon = "✅";
                BiometricStatusText = "生物识别可用";
            }
            else
            {
                BiometricStatusIcon = "⚠️";
                BiometricStatusText = "设备不支持生物识别";
            }
        }
        catch (Exception ex)
        {
            BiometricStatusIcon = "❌";
            BiometricStatusText = "检测失败";
            Console.WriteLine($"Biometric check error: {ex}");
        }
    }
    
    private async Task TestBiometricAsync()
    {
        try
        {
            IsBusy = true;
            
            var result = await _biometricService.AuthenticateAsync("测试生物识别验证");
            
            switch (result)
            {
                case BiometricAuthResult.Success:
                    await Shell.Current.DisplayAlert("成功", "验证成功！", "确定");
                    break;
                case BiometricAuthResult.Failure:
                    await Shell.Current.DisplayAlert("失败", "验证失败，请重试", "确定");
                    break;
                case BiometricAuthResult.UserCancel:
                    await Shell.Current.DisplayAlert("取消", "用户取消了验证", "确定");
                    break;
                case BiometricAuthResult.NotEnrolled:
                    await Shell.Current.DisplayAlert("提示", "设备未录入生物特征", "确定");
                    break;
                case BiometricAuthResult.NotImplemented:
                    await Shell.Current.DisplayAlert("提示", "设备不支持生物识别", "确定");
                    break;
                default:
                    await Shell.Current.DisplayAlert("错误", $"验证失败：{result}", "确定");
                    break;
            }
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("错误", ex.Message, "确定");
        }
        finally
        {
            IsBusy = false;
        }
    }
}
