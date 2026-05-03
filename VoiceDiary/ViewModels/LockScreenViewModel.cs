namespace VoiceDiary.ViewModels;

public partial class LockScreenViewModel : BaseViewModel
{
    private readonly IBiometricAuthService _biometricService;
    private readonly IAppLockManager _appLockManager;
    
    private bool _isLockScreenVisible;
    private bool _isAuthenticating;
    private bool _canCancel = true;
    private bool _showPasswordOption;
    private string _passwordInput = string.Empty;
    private int _failedAttempts;
    
    public event EventHandler<bool>? LockStateChanged;
    
    public LockScreenViewModel(
        IBiometricAuthService biometricService,
        IAppLockManager appLockManager)
    {
        _biometricService = biometricService;
        _appLockManager = appLockManager;
    }
    
    public bool IsLockScreenVisible
    {
        get => _isLockScreenVisible;
        set => SetProperty(ref _isLockScreenVisible, value);
    }
    
    public bool IsAuthenticating
    {
        get => _isAuthenticating;
        set => SetProperty(ref _isAuthenticating, value);
    }
    
    public bool IsNotAuthenticating => !_isAuthenticating;
    
    public bool CanCancel
    {
        get => _canCancel;
        set => SetProperty(ref _canCancel, value);
    }
    
    public bool ShowPasswordOption
    {
        get => _showPasswordOption;
        set => SetProperty(ref _showPasswordOption, value);
    }
    
    public string PasswordInput
    {
        get => _passwordInput;
        set => SetProperty(ref _passwordInput, value);
    }
    
    public Command AuthenticateCommand => new Command(async () => await AuthenticateAsync());
    public Command CancelCommand => new Command(() => Cancel());
    public Command UsePasswordCommand => new Command(() => UsePassword());
    public Command SubmitPasswordCommand => new Command(async () => await SubmitPasswordAsync());
    
    public async Task ShowAsync()
    {
        IsAuthenticating = false;
        IsLockScreenVisible = true;
        
        // 自动开始验证
        await AuthenticateAsync();
    }
    
    private async Task AuthenticateAsync()
    {
        if (IsAuthenticating)
            return;
        
        try
        {
            IsAuthenticating = true;
            OnPropertyChanged(nameof(IsNotAuthenticating));
            
            var result = await _biometricService.AuthenticateAsync("验证身份以访问 VoiceDiary");
            
            if (result == BiometricAuthResult.Success)
            {
                // 验证成功
                await _appLockManager.RecordSuccessfulAuthAsync();
                IsLockScreenVisible = false;
                LockStateChanged?.Invoke(this, true);
            }
            else
            {
                // 验证失败
                IsAuthenticating = false;
                OnPropertyChanged(nameof(IsNotAuthenticating));
                
                _failedAttempts++;
                
                // 3 次失败后显示密码选项
                if (_failedAttempts >= 3)
                {
                    ShowPasswordOption = true;
                }
                
                await Shell.Current.DisplayAlert("验证失败", 
                    result switch
                    {
                        BiometricAuthResult.Failure => "生物识别验证失败，请重试",
                        BiometricAuthResult.UserCancel => "验证已取消",
                        BiometricAuthResult.NotEnrolled => "设备未录入生物特征，请在系统设置中添加",
                        BiometricAuthResult.NotImplemented => "设备不支持生物识别",
                        _ => "验证失败，请重试"
                    }, 
                    "确定");
                
                // 保持锁屏状态
                IsLockScreenVisible = true;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Authenticate error: {ex}");
            IsAuthenticating = false;
            OnPropertyChanged(nameof(IsNotAuthenticating));
        }
    }
    
    private void UsePassword()
    {
        ShowPasswordOption = true;
    }
    
    private async Task SubmitPasswordAsync()
    {
        if (string.IsNullOrWhiteSpace(PasswordInput))
        {
            await Shell.Current.DisplayAlert("错误", "请输入密码", "确定");
            return;
        }
        
        // TODO: 验证密码（未来实现）
        // 临时实现：密码为 123456
        
        if (PasswordInput == "123456")
        {
            await _appLockManager.RecordSuccessfulAuthAsync();
            IsLockScreenVisible = false;
            PasswordInput = string.Empty;
            ShowPasswordOption = false;
            _failedAttempts = 0;
            LockStateChanged?.Invoke(this, true);
        }
        else
        {
            await Shell.Current.DisplayAlert("错误", "密码错误", "确定");
            PasswordInput = string.Empty;
        }
    }
    
    private void Cancel()
    {
        IsLockScreenVisible = false;
        LockStateChanged?.Invoke(this, false);
    }
}
