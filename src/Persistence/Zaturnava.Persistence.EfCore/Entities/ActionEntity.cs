namespace Net.NowhereAtAll.Zaturnava.Persistence.EfCore.Entities;

/// <summary>One action of a stored game; <see cref="Arguments"/> holds its arguments as JSON.</summary>
/// <remarks>
/// Its strings are unset (null) until assigned, not defaulted: a row is only ever built with every
/// column given, and a partly filled instance (as a test data template is) says nothing about the
/// columns it leaves out. The model in <see cref="GameDbContext"/> keeps those columns required.
/// </remarks>
public sealed class ActionEntity
{
    public string Id { get; set; } = null!;

    public string GameId { get; set; } = null!;

    public int Sequence { get; set; }

    public string Kind { get; set; } = null!;

    public string Arguments { get; set; } = null!;
}
