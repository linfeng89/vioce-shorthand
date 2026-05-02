namespace VoiceDiary.Views;

public partial class DiaryDetailPage : ContentPage
{
    public DiaryDetailPage(DiaryDetailViewModel viewModel, DiaryEntry entry)
    {
        InitializeComponent();
        viewModel.Entry = entry;
        BindingContext = viewModel;
    }

    private async void OnPlayButtonClicked(object sender, EventArgs e)
    {
        var viewModel = (DiaryDetailViewModel)BindingContext;
        await viewModel.PlayPauseCommand.ExecuteAsync(null);
    }
}
