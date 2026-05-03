namespace VoiceDiary.Views;

public partial class SettingsPage : ContentPage
{
    public SettingsPage(SettingsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
    
    private async void OnSecuritySettingsTapped(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new SecuritySettingsPage(App.Services.GetRequiredService<SecuritySettingsViewModel>()));
    }
    
    private async void OnQuickRecordSettingsTapped(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new QuickRecordSettingsPage());
    }
}
