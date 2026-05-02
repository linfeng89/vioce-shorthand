namespace VoiceDiary.ViewModels;

public partial class SearchViewModel : BaseViewModel
{
    private readonly ISearchService _searchService;

    private ObservableCollection<DiaryEntry> _searchResults = new();
    private string _searchQuery = string.Empty;
    private bool _isSearching;
    private string? _searchHint;
    private DateTime? _startDate;
    private DateTime? _endDate;

    public SearchViewModel(ISearchService searchService)
    {
        _searchService = searchService;
        _searchHint = "搜索日记内容...";
    }

    public ObservableCollection<DiaryEntry> SearchResults
    {
        get => _searchResults;
        set => SetProperty(ref _searchResults, value);
    }

    public string SearchQuery
    {
        get => _searchQuery;
        set => SetProperty(ref _searchQuery, value);
    }

    public string? SearchHint
    {
        get => _searchHint;
        set => SetProperty(ref _searchHint, value);
    }

    public bool IsSearching
    {
        get => _isSearching;
        set => SetProperty(ref _isSearching, value);
    }

    public DateTime? StartDate
    {
        get => _startDate;
        set => SetProperty(ref _startDate, value);
    }

    public DateTime? EndDate
    {
        get => _endDate;
        set => SetProperty(ref _endDate, value);
    }

    public Command SearchCommand => new Command(async () => await PerformSearchAsync());
    public Command ClearSearchCommand => new Command(() => ClearSearch());

    private async Task PerformSearchAsync()
    {
        if (string.IsNullOrWhiteSpace(SearchQuery))
        {
            ClearSearch();
            return;
        }

        try
        {
            IsSearching = true;
            SearchResults.Clear();

            var results = await _searchService.SearchAsync(SearchQuery, StartDate, EndDate);
            
            foreach (var entry in results)
            {
                SearchResults.Add(entry);
            }

            SearchHint = results.Any() 
                ? $"找到 {results.Count()} 条结果" 
                : "没有找到匹配的日记";
        }
        catch (Exception ex)
        {
            SearchHint = $"搜索失败：{ex.Message}";
        }
        finally
        {
            IsSearching = false;
        }
    }

    private void ClearSearch()
    {
        SearchQuery = string.Empty;
        SearchResults.Clear();
        SearchHint = "搜索日记内容...";
        StartDate = null;
        EndDate = null;
    }

    public void ApplyDateFilter(DateTime? start, DateTime? end)
    {
        StartDate = start;
        EndDate = end;
        if (!string.IsNullOrWhiteSpace(SearchQuery))
        {
            _ = PerformSearchAsync();
        }
    }
}
