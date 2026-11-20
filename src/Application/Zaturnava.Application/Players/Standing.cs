using System.ComponentModel.DataAnnotations;

namespace Net.NowhereAtAll.Zaturnava.Application.Players;

/// <summary>
/// Where a signed-in player stands at one game, whatever its size (<see cref="Game"/>, as a ruleset's game id): their
/// Elo rating, from <see cref="FirstElo"/>; their experience, gained in each game they won: their own points, and as
/// much again times their share of their team's (<see cref="StandingService"/>); the points they scored themselves,
/// in every game they finished, won or not; how many games they won, lost and drew; and their winning streaks.
/// </summary>
public sealed record Standing(PlayerId Player, string Game)
{
    /// <summary>The rating a player starts at (Proposed), as anyone not rated is taken to be.</summary>
    public const double FirstElo = 1500;

    public double Elo { get; init; } = FirstElo;

    /// <summary>Their Glicko-2 rating, counted from the same games as <see cref="Elo"/>.</summary>
    public Glicko Glicko { get; init; } = Glicko.First;

    /// <summary>Their TrueSkill rating, counted from the same games as <see cref="Elo"/>.</summary>
    public TrueSkill TrueSkill { get; init; } = TrueSkill.First;

    public int Experience { get; init; }

    /// <summary>The points they scored, in all the games of it they finished, since these were counted.</summary>
    public int Points { get; init; }

    public int Won { get; init; }

    public int Lost { get; init; }

    public int Drawn { get; init; }

    /// <summary>The games they have won in a row since they last lost, of every game.</summary>
    public Streak WinningStreak { get; init; } = Streak.None;

    /// <summary>
    /// The games they have won in a row since they last lost, of those that count toward Elo: no guest, the players
    /// the same throughout, no player at several seats.
    /// </summary>
    public Streak RatedStreak { get; init; } = Streak.None;

    /// <summary>The games they have won in a row since they last lost, of those played against people alone.</summary>
    public Streak PeopleStreak { get; init; } = Streak.None;

    /// <summary>How many games of it they finished.</summary>
    public int Played => this.Won + this.Lost + this.Drawn;

    /// <summary>
    /// Where they stand with <paramref name="other"/>'s games too, as one player (Proposed): games, experience and
    /// points added up, and the ratings averaged, each weighed by the games behind it, the surer deviation of the two
    /// kept; each streak the longer of the two.
    /// </summary>
    public Standing Merged([Required] Standing other)
    {
        int played = this.Played + other.Played;
        double Weighed(double mine, double theirs) =>
            played == 0 ? mine : ((mine * this.Played) + (theirs * other.Played)) / played;
        return this with
        {
            Elo = Weighed(this.Elo, other.Elo),
            Glicko = new Glicko(
                Weighed(this.Glicko.Rating, other.Glicko.Rating),
                Math.Min(this.Glicko.Deviation, other.Glicko.Deviation),
                Weighed(this.Glicko.Volatility, other.Glicko.Volatility)
            ),
            TrueSkill = new TrueSkill(
                Weighed(this.TrueSkill.Mean, other.TrueSkill.Mean),
                Math.Min(this.TrueSkill.Deviation, other.TrueSkill.Deviation)
            ),
            Experience = this.Experience + other.Experience,
            Points = this.Points + other.Points,
            Won = this.Won + other.Won,
            Lost = this.Lost + other.Lost,
            Drawn = this.Drawn + other.Drawn,
            WinningStreak = Streak.Longer(this.WinningStreak, other.WinningStreak),
            RatedStreak = Streak.Longer(this.RatedStreak, other.RatedStreak),
            PeopleStreak = Streak.Longer(this.PeopleStreak, other.PeopleStreak),
        };
    }
}
