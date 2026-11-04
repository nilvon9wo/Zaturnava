using System.Globalization;

namespace Net.NowhereAtAll.Zaturnava.Presentation;

/// <summary>How long ago something happened, roughly: "just now", "5 min ago", "2 days ago".</summary>
public static class TimeAgo
{
    public static string Since(DateTimeOffset time, DateTimeOffset now)
    {
        TimeSpan age = now - time;
        return age switch
        {
            { TotalMinutes: < 1 } => "just now",
            { TotalHours: < 1 } => $"{Whole(age.TotalMinutes)} min ago",
            { TotalDays: < 1 } => $"{Whole(age.TotalHours)} h ago",
            { TotalDays: < 2 } => "1 day ago",
            _ => $"{Whole(age.TotalDays)} days ago",
        };
    }

    private static string Whole(double amount) => ((int)amount).ToString(CultureInfo.InvariantCulture);
}
