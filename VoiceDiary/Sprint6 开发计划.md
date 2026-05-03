# Sprint 6 开发计划

**Sprint**: 快捷录音  
**状态**: 🔄 开发中  
**日期**: 2026-05-02  
**开发者**: linfeng89

---

## 开发目标

实现快捷录音功能，让用户无需打开应用即可快速开始录音。

---

## 开发内容

### P0 核心功能（必须完成）

| 任务 | 优先级 | 预计工时 | 状态 | 说明 |
|------|--------|----------|------|------|
| 通知栏常驻服务 | P0 | 3h | ⏳ | Android Foreground Service |
| 通知栏快捷按钮 | P0 | 2h | ⏳ | 开始/停止录音 |
| Android Widget | P0 | 4h | ⏳ | 1x1 桌面小组件 |
| iOS Widget | P0 | 6h | ⏳ | Home Screen Widget |
| 快捷录音逻辑 | P0 | 2h | ⏳ | 绕过锁屏验证 |
| 录音中通知更新 | P0 | 2h | ⏳ | 显示时长 + 状态 |

### P1 增强功能

| 任务 | 优先级 | 预计工时 | 状态 | 说明 |
|------|--------|----------|------|------|
| 耳机双击触发 | P1 | 4h | ⏳ | MediaButtonReceiver |
| 耳机触发配置页 | P1 | 1h | ⏳ | 开关 + 平台说明 |
| 锁屏控件（iOS） | P1 | 3h | ⏳ | 锁屏录音控制 |
| 快速回放录音 | P1 | 2h | ⏳ | 通知栏直接回放 |

---

## 技术设计

### 1. 通知栏常驻服务（Android）

#### Foreground Service

**AndroidManifest.xml**:
```xml
<uses-permission android:name="android.permission.FOREGROUND_SERVICE" />
<uses-permission android:name="android.permission.FOREGROUND_SERVICE_MICROPHONE" />
<uses-permission android:name="android.permission.POST_NOTIFICATIONS" />

<application>
    <service 
        android:name=".RecordingForegroundService"
        android:foregroundServiceType="microphone"
        android:exported="false" />
</application>
```

#### 服务实现

```csharp
[Service(
    ForegroundServiceType = ForegroundService.TypeMicrophone,
    Exported = false)]
public class RecordingForegroundService : Service
{
    private const int NotificationId = 1001;
    private const string ChannelId = "recording_channel";
    
    public override StartCommandResult OnStartCommand(Intent intent, StartCommandFlags flags, int startId)
    {
        if (intent?.Action == "ACTION_START_RECORDING")
        {
            StartRecording();
            CreateNotificationChannel();
            StartForeground(NotificationId, CreateNotification("录音中..."));
        }
        else if (intent?.Action == "ACTION_STOP_RECORDING")
        {
            StopRecording();
            StopForeground(true);
            StopSelf();
        }
        
        return StartCommandResult.Sticky;
    }
    
    private Notification CreateNotification(string text)
    {
        var intent = new Intent(this, typeof(MainActivity));
        var pendingIntent = PendingIntent.GetActivity(this, 0, intent, PendingIntentFlags.UpdateCurrent);
        
        var stopIntent = new Intent(this, typeof(RecordingForegroundService));
        stopIntent.SetAction("ACTION_STOP_RECORDING");
        var stopPendingIntent = PendingIntent.GetService(this, 1, stopIntent, PendingIntentFlags.UpdateCurrent);
        
        var builder = new NotificationCompat.Builder(this, ChannelId)
            .SetContentTitle("VoiceDiary")
            .SetContentText(text)
            .SetSmallIcon(Resource.Drawable.ic_notification)
            .SetContentIntent(pendingIntent)
            .AddAction(Resource.Drawable.ic_stop, "停止录音", stopPendingIntent)
            .SetOngoing(true);
        
        return builder.Build();
    }
}
```

---

### 2. 通知栏快捷录音

#### 通知构建器

```csharp
public class NotificationService
{
    public void ShowRecordingNotification(TimeSpan duration)
    {
        var notificationManager = (NotificationManager)App.Current.ApplicationContext.GetSystemService(Context.NotificationService);
        
        var intent = new Intent(App.Current.ApplicationContext, typeof(MainActivity));
        var pendingIntent = PendingIntent.GetActivity(App.Current.ApplicationContext, 0, intent, PendingIntentFlags.UpdateCurrent);
        
        var stopIntent = new Intent(App.Current.ApplicationContext, typeof(RecordingForegroundService));
        stopIntent.SetAction("ACTION_STOP_RECORDING");
        var stopPendingIntent = PendingIntent.GetService(App.Current.ApplicationContext, 1, stopIntent, PendingIntentFlags.UpdateCurrent);
        
        var builder = new NotificationCompat.Builder(App.Current.ApplicationContext, "recording_channel")
            .SetContentTitle("🎙️ VoiceDiary 录音中")
            .SetContentText($"{duration.Minutes:00}:{duration.Seconds:00}")
            .SetSmallIcon(Resource.Drawable.ic_notification_recording)
            .SetContentIntent(pendingIntent)
            .AddAction(Resource.Drawable.ic_stop, "停止", stopPendingIntent)
            .SetOngoing(true)
            .SetPriority(NotificationCompat.PriorityHigh);
        
        notificationManager.Notify(1001, builder.Build());
    }
}
```

---

### 3. Android Widget

#### Widget Provider

```csharp
[BroadcastReceiver(Exported = true)]
[IntentFilter(new[] { AppWidgetManager.ActionAppwidgetUpdate })]
[MetaData(AppWidgetManager.MetadataAppWidgetProvider, 
    Resource = "@xml/quick_record_widget_info")]
public class QuickRecordWidgetProvider : AppWidgetProvider
{
    public override void OnUpdate(Context context, AppWidgetManager appWidgetManager, int[] appWidgetIds)
    {
        var me = new ComponentName(context, Java.Lang.Class.FromType(typeof(QuickRecordWidgetProvider)));
        appWidgetManager.UpdateAppWidget(me, BuildWidgetRemoteViews(context));
    }
    
    public override void OnReceive(Context context, Intent intent)
    {
        base.OnReceive(context, intent);
        
        if (intent?.Action == "ACTION_QUICK_RECORD")
        {
            // 启动快捷录音
            var serviceIntent = new Intent(context, typeof(RecordingForegroundService));
            serviceIntent.SetAction("ACTION_START_RECORDING");
            context.StartForegroundService(serviceIntent);
        }
    }
    
    private static RemoteViews BuildWidgetRemoteViews(Context context)
    {
        var rv = new RemoteViews(context.PackageName, Resource.Layout.widget_quick_record);
        
        var intent = new Intent(context, typeof(QuickRecordWidgetProvider));
        intent.SetAction("ACTION_QUICK_RECORD");
        var pendingIntent = PendingIntent.GetBroadcast(context, 0, intent, PendingIntentFlags.UpdateCurrent);
        rv.SetOnClickPendingIntent(Resource.Id.record_button, pendingIntent);
        
        return rv;
    }
}
```

#### Widget 布局 (widget_quick_record.xml)

```xml
<?xml version="1.0" encoding="utf-8"?>
<FrameLayout xmlns:android="http://schemas.android.com/apk/res/android"
    android:layout_width="match_parent"
    android:layout_height="match_parent"
    android:background="@drawable/widget_background">
    
    <ImageButton
        android:id="@+id/record_button"
        android:layout_width="60dp"
        android:layout_height="60dp"
        android:layout_gravity="center"
        android:background="@drawable/circle_button"
        android:src="@drawable/ic_mic"
        android:contentDescription="快速录音" />
</FrameLayout>
```

#### Widget 配置 (quick_record_widget_info.xml)

```xml
<?xml version="1.0" encoding="utf-8"?>
<appwidget-provider xmlns:android="http://schemas.android.com/apk/res/android"
    android:minWidth="40dp"
    android:minHeight="40dp"
    android:updatePeriodMillis="0"
    android:initialLayout="@layout/widget_quick_record"
    android:resizeMode="none"
    android:widgetCategory="home_screen"
    android:previewImage="@drawable/widget_preview" />
```

---

### 4. iOS Widget

#### Widget Extension

```swift
import WidgetKit
import SwiftUI

@main
struct QuickRecordWidget: Widget {
    let kind: String = "QuickRecordWidget"

    var body: some WidgetConfiguration {
        StaticConfiguration(kind: kind, provider: Provider()) { entry in
            QuickRecordWidgetEntryView(entry: entry)
        }
        .configurationDisplayName("快速录音")
        .description("点击立即开始录音")
        .supportedFamilies([.systemSmall])
    }
}

struct Provider: TimelineProvider {
    func placeholder(in context: Context) -> SimpleEntry {
        SimpleEntry(date: Date(), isRecording: false)
    }

    func getSnapshot(in context: Context, completion: @escaping (SimpleEntry) -> ()) {
        let entry = SimpleEntry(date: Date(), isRecording: false)
        completion(entry)
    }

    func getTimeline(in context: Context, completion: @escaping (Timeline<Entry>) -> ()) {
        var entries: [SimpleEntry] = []
        let currentDate = Date()
        let entry = SimpleEntry(date: currentDate, isRecording: false)
        entries.append(entry)
        let timeline = Timeline(entries: entries, policy: .never)
        completion(timeline)
    }
}

struct SimpleEntry: TimelineEntry {
    let date: Date
    let isRecording: Bool
}

struct QuickRecordWidgetEntryView: View {
    var entry: Provider.Entry

    var body: some View {
        Link(destination: URL(string: "voicediary://quickrecord")!) {
            VStack {
                Image(systemName: "mic.fill")
                    .font(.system(size: 30))
                    .foregroundColor(.red)
                Text("录音")
                    .font(.caption)
            }
        }
    }
}
```

#### App 快捷方式

```swift
// AppDelegate.swift
func application(_ app: UIApplication, open url: URL, options: [UIApplication.OpenURLOptionsKey : Any] = [:]) -> Bool {
    if url.scheme == "voicediary", url.host == "quickrecord" {
        // 启动快捷录音
        startQuickRecording()
    }
    return true
}
```

---

### 5. 耳机双击触发

#### MediaButtonReceiver (Android)

```csharp
[BroadcastReceiver(Exported = true, Enabled = true)]
[IntentFilter(new[] { Intent.ActionMediaButton })]
public class MediaButtonReceiver : BroadcastReceiver
{
    public override void OnReceive(Context context, Intent intent)
    {
        if (intent?.Action != Intent.ActionMediaButton)
            return;
        
        var keyEvent = intent.GetParcelableExtra(Intent.ExtraKeyEvent) as KeyEvent;
        if (keyEvent == null)
            return;
        
        if (keyEvent.KeyCode == KeyCode.Headsethook && 
            keyEvent.Action == KeyEventActions.Down)
        {
            // 双击检测逻辑
            HandleHeadsetDoubleClick(context);
        }
    }
    
    private static DateTime _lastPressTime = DateTime.MinValue;
    
    private static void HandleHeadsetDoubleClick(Context context)
    {
        var now = DateTime.Now;
        if ((now - _lastPressTime).TotalMilliseconds < 300)
        {
            // 双击触发
            StartQuickRecording(context);
            _lastPressTime = DateTime.MinValue;
        }
        else
        {
            _lastPressTime = now;
        }
    }
}
```

---

## 验收标准

### 通知栏录音
- [ ] 通知栏常驻录音按钮
- [ ] 点击开始录音
- [ ] 录音中通知显示时长
- [ ] 停止按钮可用
- [ ] 通知不能滑动删除

### Android Widget
- [ ] 可添加到桌面
- [ ] 点击开始录音
- [ ] 录音中 Widget 显示状态
- [ ] 1x1 尺寸正常

### iOS Widget
- [ ] 可添加到主屏幕
- [ ] 点击启动应用并录音
- [ ] Widget 显示正常

### 耳机触发
- [ ] 双击耳机触发录音
- [ ] 配置页开关可用
- [ ] 平台兼容性良好

---

## 开发步骤

### Day 1：通知栏服务

**上午**：
- [ ] AndroidManifest 配置
- [ ] Foreground Service 实现
- [ ] 通知渠道创建
- [ ] 通知构建器

**下午**：
- [ ] 录音中通知更新
- [ ] 停止录音逻辑
- [ ] 通知栏测试

### Day 2：Widget 开发

**上午**：
- [ ] Android Widget 布局
- [ ] Widget Provider 实现
- [ ] 点击启动录音

**下午**：
- [ ] iOS Widget Extension
- [ ] 快捷方式 URL Scheme
- [ ] Widget 测试

### Day 3：耳机触发 + 优化

**上午**：
- [ ] MediaButtonReceiver
- [ ] 双击检测逻辑
- [ ] 配置页面

**下午**：
- [ ] 整体测试
- [ ] 性能优化
- [ ] 文档编写

---

## 依赖项

```
通知栏常驻
└── Foreground Service
    └── 录音服务集成
    └── 通知更新

Widget
├── Android: AppWidgetProvider
│   └── RemoteViews
│   └── PendingIntent
│
└── iOS: WidgetKit
    └── SwiftUI
    └── URL Scheme

耳机触发
└── MediaButtonReceiver (Android)
    └── 双击检测
    └── 配置开关
```

---

## 风险评估

| 风险 | 影响 | 概率 | 应对 |
|------|------|------|------|
| iOS 后台限制 | 高 | 高 | 使用 Widget 而非后台 |
| Android 杀后台 | 中 | 中 | 前台服务保活 |
| 耳机型号兼容 | 中 | 中 | 说明支持型号 |
| Widget 刷新频率 | 低 | 低 | 按需刷新 |

---

## 交付物

- ✅ RecordingForegroundService.cs
- ✅ NotificationService.cs
- ✅ QuickRecordWidgetProvider.cs (Android)
- ✅ Widget 布局文件
- ✅ iOS Widget Extension (Swift)
- ✅ MediaButtonReceiver.cs
- ✅ Settings 配置页更新

---

**文档版本**: v1.0  
**创建时间**: 2026-05-02  
**最后更新**: 2026-05-02
