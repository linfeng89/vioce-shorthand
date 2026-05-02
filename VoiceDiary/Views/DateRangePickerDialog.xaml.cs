using System;

namespace VoiceDiary.Views;

public partial class DateRangePickerDialog : ContentPage
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    
    public event EventHandler<DateRangeSelectedEventArgs>? DateRangeSelected;
    
    public DateRangePickerDialog()
    {
        InitializeComponent();
        
        // 默认开始日期为 30 天前
        StartDate = DateTime.Now.AddDays(-30);
        EndDate = DateTime.Now;
        
        StartDatePicker.Date = StartDate;
        EndDatePicker.Date = EndDate;
    }
    
    private void OnClearClicked(object sender, EventArgs e)
    {
        DateRangeSelected?.Invoke(this, new DateRangeSelectedEventArgs(null, null));
        Navigation.PopModalAsync();
    }
    
    private void OnApplyClicked(object sender, EventArgs e)
    {
        var start = StartDatePicker.Date;
        var end = EndDatePicker.Date;
        
        // 验证日期范围
        if (start > end)
        {
            ErrorLabel.Text = "开始日期不能晚于结束日期";
            ErrorLabel.IsVisible = true;
            return;
        }
        
        DateRangeSelected?.Invoke(this, new DateRangeSelectedEventArgs(start, end));
        Navigation.PopModalAsync();
    }
}

public class DateRangeSelectedEventArgs : EventArgs
{
    public DateTime? StartDate { get; }
    public DateTime? EndDate { get; }
    
    public DateRangeSelectedEventArgs(DateTime? start, DateTime? end)
    {
        StartDate = start;
        EndDate = end;
    }
}
