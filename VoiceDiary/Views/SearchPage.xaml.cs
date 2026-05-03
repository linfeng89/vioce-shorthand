namespace VoiceDiary.Views;

public partial class SearchPage : ContentPage
{
    private readonly SearchViewModel _viewModel;

    public SearchPage(SearchViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
        
        // 页面加载时加载搜索历史
        Loaded += async (s, e) => await _viewModel.LoadHistoryCommand.ExecuteAsync(null);
    }

    private async void OnSearchTextChanged(object sender, TextChangedEventArgs e)
    {
        // 实时更新搜索结果（可添加防抖）
        if (!string.IsNullOrWhiteSpace(e.NewTextValue) && e.NewTextValue.Length >= 2)
        {
            // 简单防抖：延迟 300ms
            await Task.Delay(300);
            
            if (SearchEntry.Text == e.NewTextValue)
            {
                _viewModel.SearchCommand.Execute(null);
            }
        }
    }

    private async void OnDateFilterClicked(object sender, EventArgs e)
    {
        var result = await DisplayActionSheet("选择日期范围", "取消", null, 
            "全部时间",
            "今天",
            "本周",
            "本月",
            "自定义");

        var now = DateTime.Now;
        DateTime? start = null;
        DateTime? end = null;

        switch (result)
        {
            case "取消":
                return;
            case "全部时间":
                // 清除筛选
                break;
            case "今天":
                start = now.Date;
                end = now;
                break;
            case "本周":
                start = now.StartOfWeek(DayOfWeek.Monday);
                end = now;
                break;
            case "本月":
                start = new DateTime(now.Year, now.Month, 1);
                end = now;
                break;
            case "自定义":
                // 显示自定义日期选择器
                await ShowCustomDateRangePicker();
                return;
        }

        _viewModel.ApplyDateFilter(start, end);
    }
    
    private async Task ShowCustomDateRangePicker()
    {
        var dialog = new DateRangePickerDialog();
        dialog.DateRangeSelected += (s, e) =>
        {
            _viewModel.ApplyDateFilter(e.StartDate, e.EndDate);
        };
        
        await Navigation.PushModalAsync(dialog);
    }
}
