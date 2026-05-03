using Android.App;
using Android.Appwidget;
using Android.Content;
using Android.Widget;
using Android.OS;

namespace VoiceDiary.Platforms.Android;

[BroadcastReceiver(Exported = true)]
[IntentFilter(new[] { AppWidgetManager.ActionAppwidgetUpdate, RecordingForegroundService.ActionStartRecording })]
[MetaData(AppWidgetManager.MetadataAppWidgetProvider, 
    Resource = "@xml/quick_record_widget_info")]
public class QuickRecordWidgetProvider : AppWidgetProvider
{
    public override void OnUpdate(Context context, AppWidgetManager appWidgetManager, int[] appWidgetIds)
    {
        var me = new ComponentName(context, Java.Lang.Class.FromType(typeof(QuickRecordWidgetProvider)));
        
        foreach (var appWidgetId in appWidgetIds)
        {
            appWidgetManager.UpdateAppWidget(me, BuildWidgetRemoteViews(context, appWidgetId));
        }
        
        base.OnUpdate(context, appWidgetManager, appWidgetIds);
    }
    
    public override void OnReceive(Context context, Intent intent)
    {
        base.OnReceive(context, intent);
        
        if (intent?.Action == "ACTION_QUICK_RECORD")
        {
            // 启动快捷录音
            var serviceIntent = new Intent(context, typeof(RecordingForegroundService));
            serviceIntent.SetAction(RecordingForegroundService.ActionStartRecording);
            context.StartForegroundService(serviceIntent);
        }
    }
    
    private static RemoteViews BuildWidgetRemoteViews(Context context, int appWidgetId)
    {
        var rv = new RemoteViews(context.PackageName, Resource.Layout.widget_quick_record);
        
        var intent = new Intent(context, typeof(QuickRecordWidgetProvider));
        intent.SetAction("ACTION_QUICK_RECORD");
        var pendingIntent = PendingIntent.GetBroadcast(context, appWidgetId, intent, 
            PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable);
        rv.SetOnClickPendingIntent(Resource.Id.record_button, pendingIntent);
        
        return rv;
    }
}
