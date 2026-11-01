namespace Net.NowhereAtAll.Zaturnava.Persistence.EfCore.Entities;

/// <summary>A stored game. Mutable, as Entity Framework needs; nothing outside this assembly sees it.</summary>
/// <remarks>
/// Its strings are unset (null) until assigned, not defaulted: a row is only ever built with every
/// column given, and a partly filled instance (as a test data template is) says nothing about the
/// columns it leaves out. The model in <see cref="GameDbContext"/> keeps those columns required.
/// </remarks>
public sealed class GameEntity
{
    public string Id { get; set; } = null!;

    public string RulesetId { get; set; } = null!;

    public int Version { get; set; }

    /// <summary>Whether the game is over, as last saved; null for a game saved before this was kept.</summary>
    public bool? IsOver { get; set; }

    /// <summary>
    /// When the game was last saved, in UTC; null for a game saved before this was kept. A date and time, not one
    /// with an offset, as SQLite can order only those.
    /// </summary>
    public DateTime? LastChanged { get; set; }

    public List<SeatEntity> Seats { get; set; } = [];

    public List<ActionEntity> Actions { get; set; } = [];
}
