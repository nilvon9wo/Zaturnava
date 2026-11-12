using System.ComponentModel.DataAnnotations;

namespace Net.NowhereAtAll.Zaturnava.Application.Bots;

/// <summary>
/// Two of the server's bots at a ruleset, the first the lesser id (ordinal), and how the first has done against
/// the second in the games the server had them play each other to rate them (<see cref="BotRatingService"/>).
/// </summary>
public sealed record BotMatchup(string Ruleset, string First, string Second)
{
    public int Won { get; init; }

    public int Lost { get; init; }

    public int Drawn { get; init; }

    public int Played => this.Won + this.Lost + this.Drawn;

    /// <summary>Whether it is the same two bots at the same ruleset, however they have done.</summary>
    public bool IsPairing([Required] BotMatchup other) =>
        (other.Ruleset, other.First, other.Second) == (this.Ruleset, this.First, this.Second);
}
