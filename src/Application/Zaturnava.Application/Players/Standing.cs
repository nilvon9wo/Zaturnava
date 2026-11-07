using System.ComponentModel.DataAnnotations;

namespace Net.NowhereAtAll.Zaturnava.Application.Players;

/// <summary>
/// Where a signed-in player stands at one game, whatever its size (<see cref="Game"/>, as a ruleset's game id): their
/// Elo rating, from <see cref="FirstElo"/>; their experience, gained in each game they won: their own points plus
/// their team's (all its seats', theirs too); the points they scored themselves, in every game they finished, won or
/// not; and how many games they won, lost and drew: all told, and of those against bots alone
/// (<see cref="WonAgainstBots"/>).
/// </summary>
public sealed record Standing(PlayerId Player, string Game)
{
    /// <summary>The rating a player starts at (Proposed), as anyone not rated is taken to be.</summary>
    public const double FirstElo = 1500;

    public double Elo { get; init; } = FirstElo;

    public int Experience { get; init; }

    /// <summary>The points they scored, in all the games of it they finished, since these were counted.</summary>
    public int Points { get; init; }

    public int Won { get; init; }

    public int Lost { get; init; }

    public int Drawn { get; init; }

    /// <summary>How many of the games they won were against bots alone: every seat not on their team a bot's.</summary>
    public int WonAgainstBots { get; init; }

    public int LostAgainstBots { get; init; }

    public int DrawnAgainstBots { get; init; }

    /// <summary>How many games of it they finished.</summary>
    public int Played => this.Won + this.Lost + this.Drawn;

    /// <summary>How many games of it they finished against bots alone.</summary>
    public int PlayedAgainstBots => this.WonAgainstBots + this.LostAgainstBots + this.DrawnAgainstBots;

    /// <summary>
    /// Where they stand with <paramref name="other"/>'s games too, as one player (Proposed): games, experience and
    /// points added up, and the Elo ratings averaged, each weighed by the games behind it.
    /// </summary>
    public Standing Merged([Required] Standing other)
    {
        int played = this.Played + other.Played;
        return this with
        {
            Elo = played == 0 ? this.Elo : ((this.Elo * this.Played) + (other.Elo * other.Played)) / played,
            Experience = this.Experience + other.Experience,
            Points = this.Points + other.Points,
            Won = this.Won + other.Won,
            Lost = this.Lost + other.Lost,
            Drawn = this.Drawn + other.Drawn,
            WonAgainstBots = this.WonAgainstBots + other.WonAgainstBots,
            LostAgainstBots = this.LostAgainstBots + other.LostAgainstBots,
            DrawnAgainstBots = this.DrawnAgainstBots + other.DrawnAgainstBots,
        };
    }
}
