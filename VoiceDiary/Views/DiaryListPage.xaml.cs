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

    private async void OnSwipeToDelete(object sender, InvokedEventArgs e)
    {
        if (e.Parameter is DiaryEntry entry)
        {
            try
            {
                await _viewModel.DeleteEntryWithUndoAsync(entry);
            }
            catch (Exception ex)
            {
                await DisplayAlert("删除失败", ex.Message, "确定");
            }
        }
    }

    private async void OnThresholdReached(object sender, ItemsViewScrolledEventArgs e)
    {
        // 无限滚动：当滚动到接近底部时加载更多
        if (e.VerticalOffset > 0 && 
            e.VerticalOffset > e.ContentSize.Height - e.VerticalOffset - 100)
        {
            await _viewModel.LoadMoreEntriesCommand.ExecuteAsync(null);
        }
    }

    private async void OnTrashTapped(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new TrashPage(App.Services.GetRequiredService<TrashViewModel>()));
    }

    private async void OnSettingsTapped(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new SettingsPage(App.Services.GetRequiredService<SettingsViewModel>()));
    }

    private void OnSearchTapped(object sender, EventArgs e)
    {
        Navigation.PushAsync(new SearchPage(App.Services.GetRequiredService<SearchViewModel>()));
    }
}
