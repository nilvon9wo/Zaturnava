using System.ComponentModel.DataAnnotations;

namespace Net.NowhereAtAll.Zaturnava.Application.Players;

/// <summary>
/// A winning streak (Stated, 2026-11-12): how many games in a row a player has won since they last lost
/// (<paramref name="Wins"/>), and the opposing teams' ratings at those games, added up
/// (<paramref name="OpponentElo"/>), so that the mean shows whom the streak was won against
/// (<see cref="MeanOpponentElo"/>). A draw neither adds to it nor ends it (Proposed).
/// </summary>
public sealed record Streak(int Wins, double OpponentElo)
{
    /// <summary>No streak: no win since the last loss, if any.</summary>
    public static Streak None { get; } = new(0, 0);

    /// <summary>The mean rating of the opposing teams the streak was won against; none, for no streak.</summary>
    public double? MeanOpponentElo => this.Wins == 0 ? null : this.OpponentElo / this.Wins;

    /// <summary>The longer of two streaks; the first, if neither is.</summary>
    public static Streak Longer([Required] Streak first, [Required] Streak second) =>
        second.Wins > first.Wins ? second : first;

    /// <summary>
    /// The streak one more game on, against opposing teams rated, on the mean, <paramref name="opponents"/>.
    /// </summary>
    internal Streak After(GameResult result, double opponents) =>
        result switch
        {
            GameResult.Won => new Streak(this.Wins + 1, this.OpponentElo + opponents),
            GameResult.Lost => None,
            _ => this,
        };
}
