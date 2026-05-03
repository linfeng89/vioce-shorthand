using Android.App;
using Android.Content;
using AndroidX.Core.App;
using VoiceDiary.Platforms.Android;

namespace VoiceDiary.Services;

public interface INotificationService
{
    void ShowRecordingNotification(TimeSpan duration);
    void HideRecordingNotification();
}

public class AndroidNotificationService : INotificationService
{
    private readonly Context _context;
    private readonly NotificationManager _notificationManager;
    
    public AndroidNotificationService(Context context)
    {
        _context = context;
        _notificationManager = (NotificationManager)context.GetSystemService(Context.NotificationService);
    }
    
    public void ShowRecordingNotification(TimeSpan duration)
    {
        var intent = new Intent(_context, typeof(MainActivity));
        var pendingIntent = PendingIntent.GetActivity(_context, 0, intent, 
            PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable);
        
        var stopIntent = new Intent(_context, typeof(RecordingForegroundService));
        stopIntent.SetAction(RecordingForegroundService.ActionStopRecording);
        var stopPendingIntent = PendingIntent.GetService(_context, 1, stopIntent, 
            PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable);
        
        var durationText = $"{duration.Minutes:00}:{duration.Seconds:00}";
        
        var builder = new NotificationCompat.Builder(_context, RecordingForegroundService.ChannelId)
            .SetContentTitle("🎙️ VoiceDiary 录音中")
            .SetContentText($"已录音 {durationText}")
            .SetSmallIcon(Resource.Drawable.ic_notification)
            .SetContentIntent(pendingIntent)
            .AddAction(Resource.Drawable.ic_stop, "停止", stopPendingIntent)
            .SetOngoing(true)
            .SetPriority(NotificationCompat.PriorityHigh)
            .SetCategory(NotificationCompat.CategoryService);
        
        _notificationManager.Notify(RecordingForegroundService.NotificationId, builder.Build());
    }
    
    public void HideRecordingNotification()
    {
        _notificationManager.Cancel(RecordingForegroundService.NotificationId);
    }
}
