namespace VoiceDiary.Views;

public partial class BackupSettingsPage : ContentPage
{
    public BackupSettingsPage(BackupSettingsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
