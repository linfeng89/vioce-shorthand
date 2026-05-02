using System.Globalization;

namespace VoiceDiary.Converters;

public class HighlightConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string text && parameter is string query && !string.IsNullOrWhiteSpace(query))
        {
            // 简单高亮：查找并标记关键词
            var index = text.IndexOf(query, StringComparison.OrdinalIgnoreCase);
            if (index >= 0)
            {
                var before = text.Substring(0, index);
                var keyword = text.Substring(index, query.Length);
                var after = text.Substring(index + query.Length);
                
                return new FormattedString
                {
                    Spans =
                    {
                        new Span { Text = before },
                        new Span { Text = keyword, BackgroundColor = Colors.Yellow, FontWeight = FontWeights.Bold },
                        new Span { Text = after }
                    }
                };
            }
        }
        
        return text ?? string.Empty;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class CountToBoolConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is int count && count > 0;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
