using Android.App;
using Android.Content;
using Android.OS;

namespace VoiceDiary.Platforms.Android;

[Service(
    ForegroundServiceType = ForegroundService.TypeMicrophone,
    Exported = false)]
public class RecordingForegroundService : Service
{
    public const string ActionStartRecording = "com.voicediary.START_RECORDING";
    public const string ActionStopRecording = "com.voicediary.STOP_RECORDING";
    public const string ActionUpdateDuration = "com.voicediary.UPDATE_DURATION";
    
    public const int NotificationId = 1001;
    public const string ChannelId = "recording_channel";
    
    private bool _isRecording;
    private string _durationText = "00:00";
    
    public override void OnCreate()
    {
        base.OnCreate();
        CreateNotificationChannel();
    }
    
    public override StartCommandResult OnStartCommand(Intent intent, StartCommandFlags flags, int startId)
    {
        if (intent == null)
            return StartCommandResult.Sticky;
        
        switch (intent.Action)
        {
            case ActionStartRecording:
                StartRecording();
                break;
            case ActionStopRecording:
                StopRecording();
                break;
            case ActionUpdateDuration:
                var duration = intent.GetStringExtra("duration") ?? "00:00";
                UpdateNotification(duration);
                break;
        }
        
        return StartCommandResult.Sticky;
    }
    
    private void StartRecording()
    {
        if (_isRecording)
            return;
        
        _isRecording = true;
        
        // 发送广播启动实际录音
        var broadcastIntent = new Intent("com.voicediary.START_RECORDING");
        SendBroadcast(broadcastIntent);
        
        StartForeground(NotificationId, CreateNotification("开始录音..."));
    }
    
    private void StopRecording()
    {
        _isRecording = false;
        StopForeground(true);
        StopSelf();
    }
    
    private void UpdateNotification(string duration)
    {
        _durationText = duration;
        var notification = CreateNotification($"录音 {_durationText}");
        var notificationManager = (NotificationManager)GetSystemService(NotificationService);
        notificationManager.Notify(NotificationId, notification);
    }
    
    private Notification CreateNotification(string contentText)
    {
        var intent = new Intent(this, typeof(MainActivity));
        var pendingIntent = PendingIntent.GetActivity(this, 0, intent, 
            PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable);
        
        var stopIntent = new Intent(this, typeof(RecordingForegroundService));
        stopIntent.SetAction(ActionStopRecording);
        var stopPendingIntent = PendingIntent.GetService(this, 1, stopIntent, 
            PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable);
        
        var builder = new NotificationCompat.Builder(this, ChannelId)
            .SetContentTitle("🎙️ VoiceDiary")
            .SetContentText(contentText)
            .SetSmallIcon(Resource.Drawable.ic_notification)
            .SetContentIntent(pendingIntent)
            .AddAction(Resource.Drawable.ic_stop, "停止", stopPendingIntent)
            .SetOngoing(true)
            .SetPriority(NotificationCompat.PriorityHigh)
            .SetCategory(NotificationCompat.CategoryService);
        
        return builder.Build();
    }
    
    private void CreateNotificationChannel()
    {
        var channel = new NotificationChannel(
            ChannelId,
            "录音服务",
            NotificationImportance.Low)
        {
            Description = "用于显示录音状态",
            LockscreenVisibility = NotificationVisibility.Secret
        };
        
        var notificationManager = (NotificationManager)GetSystemService(NotificationService);
        notificationManager.CreateNotificationChannel(channel);
    }
    
    public override IBinder? OnBind(Intent? intent)
    {
        return null;
    }
}
