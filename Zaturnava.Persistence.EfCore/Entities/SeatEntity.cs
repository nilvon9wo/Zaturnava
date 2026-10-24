namespace Net.NowhereAtAll.Zaturnava.Persistence.EfCore.Entities;

/// <summary>Who controls one seat of a stored game.</summary>
/// <remarks>
/// Its strings are unset (null) until assigned, not defaulted: a row is only ever built with every
/// column given, and a partly filled instance (as a test data template is) says nothing about the
/// columns it leaves out. The model in <see cref="GameDbContext"/> keeps those columns required.
/// </remarks>
public sealed class SeatEntity
{
    public string Id { get; set; } = null!;

    public string GameId { get; set; } = null!;

    public int Seat { get; set; }

    public string Kind { get; set; } = null!;

    public string? Detail { get; set; }
}
