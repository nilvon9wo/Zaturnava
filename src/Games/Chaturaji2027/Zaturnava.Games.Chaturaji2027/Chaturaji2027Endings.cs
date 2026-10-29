namespace Net.NowhereAtAll.Zaturnava.Games.Chaturaji2027;

/// <summary>The ways a game ends (rules.md §10), as the codes its outcome's <c>Ending</c> carries.</summary>
public static class Chaturaji2027Endings
{
    /// <summary>Only one team has pieces left, and wins outright.</summary>
    public const string OneTeamLeft = "one-team-left";

    /// <summary>Only Rajas are left: an honourable peace, a draw.</summary>
    public const string OnlyRajasLeft = "only-rajas-left";

    /// <summary>Every player still playing agreed to end it; points decide.</summary>
    public const string Agreement = "agreement";

    /// <summary>A whole team gave up its turns; points decide.</summary>
    public const string TeamGaveUp = "team-gave-up";

    /// <summary>No one rolled or moved for some rounds; points decide.</summary>
    public const string Idle = "idle";
}
