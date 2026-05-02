namespace VoiceDiary.Views;

public partial class TrashPage : ContentPage
{
    public TrashPage(TrashViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is TrashViewModel viewModel)
        {
            await viewModel.LoadDeletedEntriesCommand.ExecuteAsync(null);
        }
    }
}
