namespace Infrastructure.Common.Options;

public sealed class ReportSchedulingOptions
{
    public const string SectionName = "ReportScheduling";

    // Day of the local (Europe/Madrid) calendar month the monthly status report is sent on.
    public int DayOfMonth { get; set; } = 1;

    // How often the background service checks whether today is the send day.
    // Independent of DayOfMonth: the report always covers the full previous calendar month.
    public int PollIntervalMinutes { get; set; } = 60;
}
