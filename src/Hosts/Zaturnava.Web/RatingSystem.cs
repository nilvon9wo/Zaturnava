namespace Net.NowhereAtAll.Zaturnava.Web;

/// <summary>
/// A rating system a viewer may see ratings by, chosen on the leaderboard and holding for the whole lobby: the bots'
/// strengths in the New game dialog are labelled by it too (the user's request of 2026-11-20).
/// </summary>
public enum RatingSystem
{
    Elo,
    Glicko,
    TrueSkill,
}

/// <summary>Each rating system's key, as the leaderboard's view keeps it, and its name, as shown.</summary>
internal static class RatingSystems
{
    /// <summary>The name the rating system chosen is remembered by, in the leaderboard's view.</summary>
    public const string Choice = "rating";

    public const string EloKey = "elo";
    public const string GlickoKey = "glicko";
    public const string TrueSkillKey = "trueskill";

    /// <summary>The system a key names; Elo, for any other.</summary>
    public static RatingSystem Of(string? key) =>
        key switch
        {
            GlickoKey => RatingSystem.Glicko,
            TrueSkillKey => RatingSystem.TrueSkill,
            _ => RatingSystem.Elo,
        };

    /// <summary>The system the leaderboard's view keeps; Elo, if it keeps none.</summary>
    public static RatingSystem Of(ListView? view) => Of(view?[Choice]);

    public static string NameOf(RatingSystem system) =>
        system switch
        {
            RatingSystem.Glicko => "Glicko-2",
            RatingSystem.TrueSkill => "TrueSkill",
            _ => "Elo",
        };
}
