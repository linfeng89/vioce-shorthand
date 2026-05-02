namespace VoiceDiary.Views;

public partial class TrashPage : ContentPage
{
    private readonly TrashViewModel _viewModel;

    public TrashPage(TrashViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadTrashEntriesCommand.ExecuteAsync(null);
    }

    private async void OnSwipeToRestore(object sender, InvokedEventArgs e)
    {
        if (e.Parameter is long entryId)
        {
            try
            {
                await _viewModel.RestoreEntryCommand.ExecuteAsync(entryId);
            }
            catch (Exception ex)
            {
                await DisplayAlert("恢复失败", ex.Message, "确定");
            }
        }
    }

    private async void OnSwipeToDelete(object sender, InvokedEventArgs e)
    {
        if (e.Parameter is long entryId)
        {
            var confirm = await DisplayAlert(
                "确认永久删除", 
                "此操作不可恢复，确定要永久删除吗？", 
                "删除", 
                "取消");
            
            if (confirm)
            {
                try
                {
                    await _viewModel.PermanentlyDeleteCommand.ExecuteAsync(entryId);
                }
                catch (Exception ex)
                {
                    await DisplayAlert("删除失败", ex.Message, "确定");
                }
            }
        }
    }
}
