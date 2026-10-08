namespace Application.Reports;

public static class PortfolioStatusReportPeriod
{
    // The full previous calendar month relative to referenceDateUtc - e.g. on any day in
    // February, returns [Jan 1, Jan 31]. Shared by the scheduled background sweep and the
    // manual "send now" endpoint so both report the same period by default.
    public static (DateTime Start, DateTime End) PreviousCalendarMonth(DateTime referenceDateUtc)
    {
        var firstOfThisMonth = new DateTime(referenceDateUtc.Year, referenceDateUtc.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var end = firstOfThisMonth.AddDays(-1);
        var start = new DateTime(end.Year, end.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        return (start, end);
    }
}
