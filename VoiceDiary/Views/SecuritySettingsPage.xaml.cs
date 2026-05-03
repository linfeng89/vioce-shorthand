namespace VoiceDiary.Views;

public partial class SecuritySettingsPage : ContentPage
{
    private readonly SecuritySettingsViewModel _viewModel;
    
    public SecuritySettingsPage(SecuritySettingsViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
    }
    
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadSettingsCommand.ExecuteAsync(null);
    }
}
