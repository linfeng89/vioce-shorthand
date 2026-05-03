# Sprint 5 开发计划

**Sprint**: 隐私保护（生物识别）  
**状态**: 🔄 开发中  
**日期**: 2026-05-02  
**开发者**: linfeng89

---

## 开发目标

实现基于生物识别的应用锁功能，保护用户隐私数据。

---

## 开发内容

### P0 核心功能（必须完成）

| 任务 | 优先级 | 预计工时 | 状态 | 说明 |
|------|--------|----------|------|------|
| CommunityToolkit.Maui 集成 | P0 | 2h | ⏳ | NuGet 包安装 |
| 生物识别服务接口 | P0 | 3h | ⏳ | IBiometricAuthService |
| 应用锁设置页 | P0 | 2h | ⏳ | 开关 + 超时配置 |
| 分级解锁逻辑 | P0 | 2h | ⏳ | 不同场景验证策略 |
| 锁屏 UI 浮层 | P0 | 2h | ⏳ | 覆盖在应用上的锁屏 |

### P1 增强功能

| 任务 | 优先级 | 预计工时 | 状态 | 说明 |
|------|--------|----------|------|------|
| 密码兜底机制 | P1 | 2h | ⏳ | 指纹失败 3 次切换密码 |
| 查看历史需验证 | P1 | 1h | ⏳ | 详情页生物识别 |
| 录音中免验证 | P1 | 1h | ⏳ | 录音过程不被打断 |

---

## 技术设计

### 1. CommunityToolkit.Maui 集成

**安装命令**：
```bash
dotnet add package CommunityToolkit.Maui
```

**MauiProgram.cs**：
```csharp
builder.UseMauiApp<App>()
       .UseMauiCommunityToolkit()
       .ConfigureEssentials(essentials =>
       {
           essentials.UseBiometric();
       });
```

---

### 2. 生物识别服务

**接口定义**：
```csharp
public interface IBiometricAuthService
{
    Task<bool> IsAvailableAsync();
    Task<BiometricAuthResult> AuthenticateAsync(string reason);
    event EventHandler<BiometricAuthResult> OnAuthenticationResult;
}

public enum BiometricAuthResult
{
    Success,
    Failure,
    UserFallback,
    UserCancel,
    SystemDisable,
    NotEnrolled,
    NotImplemented
}
```

**实现类**：
```csharp
public class BiometricAuthService : IBiometricAuthService
{
    public Task<bool> IsAvailableAsync()
    {
        return Task.FromResult(BiometricConstants.IsAvailable);
    }
    
    public async Task<BiometricAuthResult> AuthenticateAsync(string reason)
    {
        var request = new BiometricAuthenticationRequest(reason);
        var result = await BiometricAuthentication.AuthenticateAsync(request);
        
        if (result.Authenticated)
            return BiometricAuthResult.Success;
        
        return result.Error switch
        {
            BiometricError.None => BiometricAuthResult.Success,
            BiometricError.NotAvailable => BiometricAuthResult.NotImplemented,
            BiometricError.NoEnrollment => BiometricAuthResult.NotEnrolled,
            BiometricAuthResult.UserFallback,
            _ => BiometricAuthResult.Failure
        };
    }
}
```

---

### 3. 应用锁设置

**设置模型**：
```csharp
public class SecuritySettings
{
    public bool IsAppLockEnabled { get; set; }
    public AppLockTimeout Timeout { get; set; } = AppLockTimeout.Immediately;
    public DateTime? LastUnlockTime { get; set; }
}

public enum AppLockTimeout
{
    Immediately,      // 立即锁定
    After30Seconds,   // 30 秒后
    After1Minute,     // 1 分钟后
    After5Minutes,    // 5 分钟后
    Never             // 从不锁定
}
```

**设置页面 UI**：
```xml
<ContentPage Title="安全设置">
    <VerticalStackLayout Padding="20" Spacing="20">
        
        <!-- 应用锁开关 -->
        <SwitchCell Title="启用应用锁" 
                    IsOn="{Binding IsAppLockEnabled}"
                    OnChanged="OnAppLockToggled"/>
        
        <!-- 超时设置（启用后显示） -->
        <PickerCell Title="自动锁定时间"
                    SelectedIndex="{Binding TimeoutIndex}"
                    IsEnabled="{Binding IsAppLockEnabled}">
            <PickerCell.Items>
                <x:String>立即</x:String>
                <x:String>30 秒后</x:String>
                <x:String>1 分钟后</x:String>
                <x:String>5 分钟后</x:String>
                <x:String>从不</x:String>
            </PickerCell.Items>
        </PickerCell>
        
        <!-- 测试生物识别 -->
        <Button Text="测试生物识别"
                Command="{Binding TestBiometricCommand}"
                IsEnabled="{Binding IsAppLockEnabled}"/>
    </VerticalStackLayout>
</ContentPage>
```

---

### 4. 分级解锁策略

**解锁管理器**：
```csharp
public class AppLockManager
{
    private readonly SecuritySettings _settings;
    private DateTime? _lastUnlockTime;
    
    public bool ShouldRequireAuth(AppAccessScenario scenario)
    {
        if (!_settings.IsAppLockEnabled)
            return false;
        
        // 快捷入口免验证
        if (scenario == AppAccessScenario.QuickRecord)
            return false;
        
        // 录音中免验证
        if (scenario == AppAccessScenario.RecordingInBackground)
            return false;
        
        // 检查超时时间
        if (_lastUnlockTime == null)
            return true;
        
        var elapsed = DateTime.Now - _lastUnlockTime.Value;
        
        return _settings.Timeout switch
        {
            AppLockTimeout.Immediately => true,
            AppLockTimeout.After30Seconds => elapsed > TimeSpan.FromSeconds(30),
            AppLockTimeout.After1Minute => elapsed > TimeSpan.FromMinutes(1),
            AppLockTimeout.After5Minutes => elapsed > TimeSpan.FromMinutes(5),
            AppLockTimeout.Never => false,
            _ => true
        };
    }
    
    public void RecordSuccessfulAuth()
    {
        _lastUnlockTime = DateTime.Now;
    }
}

public enum AppAccessScenario
{
    AppLaunch,
    ReturnFromBackground,
    ViewDiaryDetail,
    PlaybackAudio,
    QuickRecord,
    RecordingInBackground
}
```

---

### 5. 锁屏 UI

**锁屏浮层**：
```xml
<ContentView x:Class="VoiceDiary.Views.LockScreenOverlay"
             IsVisible="{Binding IsLockScreenVisible}">
    
    <Frame BackgroundColor="#80000000"
           HorizontalOptions="Fill"
           VerticalOptions="Fill">
        
        <VerticalStackLayout HorizontalOptions="Center"
                           VerticalOptions="Center"
                           Spacing="30">
            
            <!-- 锁图标 -->
            <Label Text="🔒"
                   FontSize="64"
                   HorizontalOptions="Center"/>
            
            <!-- 提示文字 -->
            <Label Text="需要验证身份"
                   FontSize="20"
                   TextColor="White"
                   HorizontalOptions="Center"/>
            
            <!-- 验证按钮 -->
            <Button Text="使用指纹/面部识别"
                    BackgroundColor="#007AFF"
                    TextColor="White"
                    WidthRequest="250"
                    HeightRequest="50"
                    CornerRadius="25"
                    Command="{Binding AuthenticateCommand}"/>
            
            <!-- 密码选项（备用） -->
            <Button Text="使用密码"
                    BackgroundColor="Transparent"
                    TextColor="White"
                    IsVisible="{Binding ShowPasswordOption}"
                    Command="{Binding UsePasswordCommand}"/>
        </VerticalStackLayout>
    </Frame>
</ContentView>
```

---

### 6. 全局集成

**App.xaml.cs**：
```csharp
public partial class App : Application
{
    private readonly IAppLockManager _appLockManager;
    private readonly IBiometricAuthService _biometricService;
    
    protected override async void OnStart()
    {
        base.OnStart();
        
        // 检查是否需要验证
        await CheckAndShowLockScreen(AppAccessScenario.AppLaunch);
    }
    
    protected override async void OnResume()
    {
        base.OnResume();
        
        // 从后台恢复时检查
        await CheckAndShowLockScreen(AppAccessScenario.ReturnFromBackground);
    }
    
    private async Task CheckAndShowLockScreen(AppAccessScenario scenario)
    {
        if (_appLockManager.ShouldRequireAuth(scenario))
        {
            var result = await _biometricService.AuthenticateAsync("验证身份以访问 VoiceDiary");
            
            if (result == BiometricAuthResult.Success)
            {
                _appLockManager.RecordSuccessfulAuth();
            }
            else
            {
                // 验证失败，显示错误或退出
                await Shell.Current.DisplayAlert("验证失败", "无法验证身份", "确定");
            }
        }
    }
}
```

---

## 验收标准

### 核心功能
- [ ] 可以启用/禁用应用锁
- [ ] 支持指纹识别
- [ ] 支持面部识别（如设备支持）
- [ ] 超时时间可配置
- [ ] 锁屏 UI 显示正常

### 分级解锁
- [ ] App 启动时验证（如启用）
- [ ] 切后台回来按超时策略验证
- [ ] 查看日记详情时验证
- [ ] 快捷录音免验证
- [ ] 录音中切后台免验证

### 用户体验
- [ ] 验证流程流畅
- [ ] 错误提示友好
- [ ] 密码兜底可用
- [ ] 设置简单明了

---

## 开发步骤

### Day 1：基础服务 + 设置页

**上午**：
- [ ] 安装 CommunityToolkit.Maui
- [ ] 实现 IBiometricAuthService
- [ ] 实现 AppLockManager
- [ ] 单元测试

**下午**：
- [ ] 创建设置页面 UI
- [ ] 实现 SecuritySettings 存储
- [ ] 绑定 ViewModel
- [ ] 测试设置保存/加载

### Day 2：锁屏 UI + 集成

**上午**：
- [ ] 创建锁屏浮层 UI
- [ ] 实现验证逻辑
- [ ] 集成到 App.xaml.cs
- [ ] 测试启动验证

**下午**：
- [ ] 实现分级解锁策略
- [ ] 密码兜底机制
- [ ] 整体测试和优化
- [ ] 文档编写

---

## 依赖项

```CommunityToolkit.Maui
└── BiometricAuthentication
    └── IBiometricAuthService
        └── AppLockManager
            ├── SecuritySettings (SQLite 存储)
            ├── App.xaml.cs (全局集成)
            └── LockScreenOverlay (UI)
```

---

## 风险评估

| 风险 | 影响 | 概率 | 应对 |
|------|------|------|------|
| iOS/Android API 差异 | 高 | 中 | CommunityToolkit 已封装 |
| 设备不支持生物识别 | 中 | 低 | 提供密码兜底 |
| 验证失败率高 | 中 | 低 | 多次尝试 + 密码备选 |
| 性能影响 | 低 | 低 | 异步验证，不阻塞 UI |

---

## 交付物

- ✅ CommunityToolkit.Maui 集成
- ✅ IBiometricAuthService.cs
- ✅ AppLockManager.cs
- ✅ SecuritySettings.cs
- ✅ SecuritySettingsPage.xaml/ViewModel
- ✅ LockScreenOverlay.xaml/ViewModel
- ✅ App.xaml.cs 全局集成

---

**文档版本**: v1.0  
**创建时间**: 2026-05-02  
**最后更新**: 2026-05-02
