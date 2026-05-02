namespace VoiceDiary.Views;

public partial class RecordPage : ContentPage
{
    private readonly RecordViewModel _viewModel;
    private readonly PanGestureRecognizer _panGesture;
    private double _startY;
    private double _currentY;
    private bool _isPressed;
    private DateTime _pressStartTime;

    public RecordPage(RecordViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;

        _panGesture = new PanGestureRecognizer();
        _panGesture.PanUpdated += OnPanUpdated;
        RecordButton.GestureRecognizers.Add(_panGesture);

        var touchGesture = new TouchGestureRecognizer();
        touchGesture.TouchStarted += OnTouchStarted;
        touchGesture.TouchEnded += OnTouchEnded;
        RecordButton.GestureRecognizers.Add(touchGesture);
    }

    private void OnTouchStarted(object? sender, EventArgs e)
    {
        _isPressed = true;
        _pressStartTime = DateTime.Now;
        _viewModel.StartRecordingCommand.Execute(null);
    }

    private async void OnTouchEnded(object? sender, EventArgs e)
    {
        if (!_isPressed)
            return;

        _isPressed = false;
        var duration = DateTime.Now - _pressStartTime;

        if (duration.TotalSeconds < 0.3)
        {
            _viewModel.CancelRecordingCommand.Execute(null);
            return;
        }

        if (_viewModel.IsLocked)
            return;

        await Task.Delay(100);

        if (!_viewModel.IsCancelling && _viewModel.IsRecording)
        {
            _viewModel.StopRecordingCommand.Execute(null);
        }
        else if (_viewModel.IsCancelling)
        {
            _viewModel.CancelRecordingCommand.Execute(null);
        }
    }

    private void OnPanUpdated(object? sender, PanUpdatedEventArgs e)
    {
        switch (e.StatusType)
        {
            case GestureStatus.Started:
                _startY = e.TotalY;
                break;

            case GestureStatus.Running:
                _currentY = e.TotalY;
                var deltaY = _currentY - _startY;
                _viewModel.HandlePanUpdated((float)deltaY, (float)_currentY);
                
                RecordButton.TranslationY = _currentY < -50 ? -50 : _currentY;
                break;

            case GestureStatus.Completed:
                RecordButton.TranslationY = 0;
                _startY = 0;
                _currentY = 0;
                break;
        }
    }

    private void OnStopButtonClicked(object sender, EventArgs e)
    {
        if (_viewModel.IsLocked && _viewModel.IsRecording)
        {
            _viewModel.StopRecordingCommand.Execute(null);
        }
    }

    private void OnCancelButtonClicked(object sender, EventArgs e)
    {
        if (_viewModel.IsCancelling || _viewModel.IsLocked)
        {
            _viewModel.CancelRecordingCommand.Execute(null);
        }
    }
}

public class TouchGestureRecognizer : GestureRecognizer
{
    public event EventHandler? TouchStarted;
    public event EventHandler? TouchEnded;

    protected override async Task OnTouchAction(Element sender, TouchActionEventArgs args)
    {
        if (args.Type == TouchActionType.Pressed)
        {
            TouchStarted?.Invoke(this, EventArgs.Empty);
            await Task.Yield();
        }
        else if (args.Type == TouchActionType.Released)
        {
            TouchEnded?.Invoke(this, EventArgs.Empty);
            await Task.Yield();
        }
    }
}
