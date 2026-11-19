namespace Net.NowhereAtAll.Zaturnava.Application.Players;

/// <summary>
/// A bot on a leaderboard: its name, as offered, and where it stands; and its Elo rating at the game, so that it may
/// be ranked beside players (Proposed): the mean of its ratings at the game's sizes, or, rated at none,
/// <see cref="Players.Standing.FirstElo"/>.
/// </summary>
public sealed record BotLeader(string Name, BotStanding Standing)
{
    public double Elo { get; init; } = Players.Standing.FirstElo;

    /// <summary>Whether the bot plays by an AI, as its profile says, not by a plain algorithm.</summary>
    public bool UsesAi { get; init; }
}
