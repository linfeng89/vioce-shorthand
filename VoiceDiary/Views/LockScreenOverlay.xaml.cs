namespace VoiceDiary.Views;

public partial class LockScreenOverlay : ContentView
{
    private readonly LockScreenViewModel _viewModel;
    
    public LockScreenOverlay(LockScreenViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
    }
}
