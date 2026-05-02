namespace VoiceDiary.ViewModels;

public partial class SearchViewModel : BaseViewModel
{
    private readonly ISearchService _searchService;
    private readonly IDatabaseService _databaseService;

    private ObservableCollection<DiaryEntry> _searchResults = new();
    private ObservableCollection<SearchHistoryItem> _searchHistory = new();
    private string _searchQuery = string.Empty;
    private bool _isSearching;
    private string? _searchHint;
    private DateTime? _startDate;
    private DateTime? _endDate;
    private bool _showHistory;

    public SearchViewModel(ISearchService searchService, IDatabaseService databaseService)
    {
        _searchService = searchService;
        _databaseService = databaseService;
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
    
    public ObservableCollection<SearchHistoryItem> SearchHistory
    {
        get => _searchHistory;
        set => SetProperty(ref _searchHistory, value);
    }
    
    public bool ShowHistory
    {
        get => _showHistory;
        set => SetProperty(ref _showHistory, value);
    }
    
    public Command LoadHistoryCommand => new Command(async () => await LoadHistoryAsync());
    public Command<SearchHistoryItem> SelectHistoryCommand => new Command<SearchHistoryItem>(async (item) => await SelectHistoryAsync(item));
    public Command ClearHistoryCommand => new Command(async () => await ClearHistoryAsync());

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
            SearchCommand.Execute(null);
        }
    }
    
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
            var resultList = results.ToList();
            
            foreach (var entry in resultList)
            {
                SearchResults.Add(entry);
            }

            SearchHint = resultList.Any() 
                ? $"找到 {resultList.Count} 条结果" 
                : "没有找到匹配的日记";
            
            // 保存搜索历史
            await SaveSearchHistoryAsync(SearchQuery, resultList.Count);
            
            // 隐藏历史记录
            ShowHistory = false;
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
    
    private async Task LoadHistoryAsync()
    {
        try
        {
            var db = await _databaseService.GetConnectionAsync();
            var history = await db.Table<SearchHistory>()
                .OrderByDescending(h => h.SearchedAt)
                .Take(50)
                .ToListAsync();
            
            SearchHistory.Clear();
            foreach (var item in history)
            {
                SearchHistory.Add(new SearchHistoryItem
                {
                    Query = item.Query,
                    SearchedAt = item.SearchedAt,
                    ResultCount = item.ResultCount
                });
            }
            
            // 显示历史记录
            ShowHistory = SearchHistory.Count > 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Load history error: {ex}");
        }
    }
    
    private async Task SelectHistoryAsync(SearchHistoryItem? item)
    {
        if (item == null)
            return;
        
        SearchQuery = item.Query;
        ShowHistory = false;
        await PerformSearchAsync();
    }
    
    private async Task ClearHistoryAsync()
    {
        var confirm = await Shell.Current.DisplayAlert(
            "确认清空",
            "确定要清空搜索历史吗？",
            "清空",
            "取消");
        
        if (!confirm)
            return;
        
        try
        {
            var db = await _databaseService.GetConnectionAsync();
            await db.ExecuteAsync("DELETE FROM SearchHistory");
            SearchHistory.Clear();
            ShowHistory = false;
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("错误", ex.Message, "确定");
        }
    }
    
    private async Task SaveSearchHistoryAsync(string query, int resultCount)
    {
        try
        {
            var db = await _databaseService.GetConnectionAsync();
            
            // 检查是否已存在相同查询
            var existing = await db.Table<SearchHistory>()
                .FirstOrDefaultAsync(h => h.Query == query);
            
            if (existing != null)
            {
                // 更新现有记录
                existing.SearchedAt = DateTime.Now;
                existing.ResultCount = resultCount;
                await db.UpdateAsync(existing);
            }
            else
            {
                // 插入新记录
                await db.InsertAsync(new SearchHistory
                {
                    Query = query,
                    SearchedAt = DateTime.Now,
                    ResultCount = resultCount
                });
            }
            
            // 清理 30 天前的记录，最多保留 50 条
            await db.ExecuteAsync(@"
                DELETE FROM SearchHistory 
                WHERE SearchedAt < datetime('now', '-30 days')
                OR rowid NOT IN (
                    SELECT rowid FROM SearchHistory 
                    ORDER BY SearchedAt DESC 
                    LIMIT 50
                )
            ");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Save history error: {ex}");
        }
    }
}

// Search History Item for UI
public class SearchHistoryItem
{
    public string Query { get; set; } = string.Empty;
    public DateTime SearchedAt { get; set; }
    public int ResultCount { get; set; }
    public string DisplayText => $"{Query} ({SearchedAt:MM-dd HH:mm})";
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
