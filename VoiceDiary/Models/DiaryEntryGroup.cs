namespace VoiceDiary.Models;

public class DiaryEntryGroup
{
    public string GroupTitle { get; set; } = string.Empty;
    public DateTime GroupDate { get; set; }
    public List<DiaryEntry> Entries { get; set; } = new();
    public string? MonthIndicator { get; set; }
}

public static class DiaryGrouping
{
    public static List<DiaryEntryGroup> GroupByDate(this IEnumerable<DiaryEntry> entries)
    {
        var groups = new List<DiaryEntryGroup>();
        var now = DateTime.Now;
        string? lastMonth = null;

        foreach (var entry in entries)
        {
            var groupDate = entry.CreatedAt.Date;
            string groupTitle;
            string? monthIndicator = null;

            // 计算分组标题
            if (groupDate == now.Date)
            {
                groupTitle = "今天";
            }
            else if (groupDate == now.AddDays(-1).Date)
            {
                groupTitle = "昨天";
            }
            else if (groupDate >= now.StartOfWeek(DayOfWeek.Monday))
            {
                groupTitle = "本周";
            }
            else if (groupDate >= now.AddDays(-7).Date)
            {
                groupTitle = "上周";
            }
            else
            {
                groupTitle = groupDate.ToString("yyyy 年 M 月 d 日");
            }

            // 添加月份指示器（当月份变化时）
            var currentMonth = $"{groupDate:yyyy 年 M 月}";
            if (lastMonth != currentMonth)
            {
                monthIndicator = currentMonth;
                lastMonth = currentMonth;
            }

            // 查找或创建分组
            var existingGroup = groups.FirstOrDefault(g => g.GroupTitle == groupTitle);
            if (existingGroup == null)
            {
                existingGroup = new DiaryEntryGroup
                {
                    GroupTitle = groupTitle,
                    GroupDate = groupDate,
                    MonthIndicator = monthIndicator
                };
                groups.Add(existingGroup);
            }

            existingGroup.Entries.Add(entry);
        }

        // 按时间排序每个分组内的条目
        foreach (var group in groups)
        {
            group.Entries = group.Entries.OrderByDescending(e => e.CreatedAt).ToList();
        }

        return groups;
    }

    public static string GetTimePeriod(this DateTime time)
    {
        return time.Hour switch
        {
            >= 6 and < 12 => "🌅 上午",
            >= 12 and < 18 => "☀️ 下午",
            >= 18 and < 21 => "🌆 傍晚",
            _ => "🌙 深夜"
        };
    }

    public static DateTime StartOfWeek(this DateTime dt, DayOfWeek startOfWeek)
    {
        int diff = (7 + (dt.DayOfWeek - startOfWeek)) % 7;
        return dt.AddDays(-1 * diff).Date;
    }
}
