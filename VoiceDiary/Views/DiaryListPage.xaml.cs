namespace VoiceDiary.Views;

public partial class DiaryListPage : ContentPage
{
    private readonly DiaryListViewModel _viewModel;

    public DiaryListPage(DiaryListViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (_viewModel.Entries.Count == 0)
        {
            await _viewModel.LoadEntriesCommand.ExecuteAsync(null);
        }
    }

    private async void OnEntryTapped(object sender, ItemTappedEventArgs e)
    {
        if (e.Item is DiaryEntry entry)
        {
            await _viewModel.NavigateToDetailCommand.ExecuteAsync(entry);
        }

        ((CollectionView)sender).SelectedItem = null;
    }

    private async void OnThresholdReached(object sender, ItemsViewScrolledEventArgs e)
    {
        if (e.VerticalOffset > 0 && 
            e.VerticalOffset > e.ContentSize.Height - e scrollView.Height - 100)
        {
            await _viewModel.LoadMoreEntriesCommand.ExecuteAsync(null);
        }
    }

    private void OnMenuItemClicked(object sender, MenuItemClickedEventArgs e)
    {
        switch (e.Parameter)
        {
            case "settings":
                App.NavigateToSettings();
                break;
            case "trash":
                NavigateToTrash();
                break;
        }
    }

    private async void NavigateToTrash()
    {
        await Navigation.PushAsync(new TrashPage(App.Services.GetRequiredService<TrashViewModel>()));
    }
}
