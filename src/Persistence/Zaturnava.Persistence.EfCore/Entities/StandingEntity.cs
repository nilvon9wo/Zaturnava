namespace Net.NowhereAtAll.Zaturnava.Persistence.EfCore.Entities;

/// <summary>Where a player stands at one game: let go with the player.</summary>
public sealed class StandingEntity
{
    public string PlayerId { get; set; } = null!;

    public string Game { get; set; } = null!;

    public double Elo { get; set; }

    public int Experience { get; set; }

    public int Points { get; set; }

    public int Won { get; set; }

    public int Lost { get; set; }

    public int Drawn { get; set; }

    /// <summary>The games won in a row since the last loss, of every game.</summary>
    public int Streak { get; set; }

    /// <summary>The opposing teams' ratings at those games, added up.</summary>
    public double StreakOpponentElo { get; set; }

    /// <summary>The games won in a row since the last loss, of those that count toward Elo.</summary>
    public int RatedStreak { get; set; }

    public double RatedStreakOpponentElo { get; set; }

    /// <summary>The games won in a row since the last loss, of those against people alone.</summary>
    public int PeopleStreak { get; set; }

    public double PeopleStreakOpponentElo { get; set; }
}
