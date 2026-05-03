using Android.Content;
using Android.Content.PM;
using Android.OS;

namespace VoiceDiary.Platforms.Android;

[BroadcastReceiver(Exported = true, Enabled = true)]
[IntentFilter(new[] { Intent.ActionMediaButton })]
public class MediaButtonReceiver : BroadcastReceiver
{
    private static DateTime _lastPressTime = DateTime.MinValue;
    private const int DoubleClickThresholdMs = 300;
    
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
            HandleHeadsetDoubleClick(context);
        }
    }
    
    private static void HandleHeadsetDoubleClick(Context context)
    {
        var now = DateTime.Now;
        if ((now - _lastPressTime).TotalMilliseconds < DoubleClickThresholdMs)
        {
            // 双击触发快捷录音
            StartQuickRecording(context);
            _lastPressTime = DateTime.MinValue;
        }
        else
        {
            _lastPressTime = now;
        }
    }
    
    private static void StartQuickRecording(Context context)
    {
        var serviceIntent = new Intent(context, typeof(RecordingForegroundService));
        serviceIntent.SetAction(RecordingForegroundService.ActionStartRecording);
        context.StartForegroundService(serviceIntent);
    }
}
