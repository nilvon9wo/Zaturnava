namespace Net.NowhereAtAll.Zaturnava.Persistence.EfCore.Entities;

/// <summary>
/// One thing said at a table, known by its first game (<see cref="TableId"/>); <see cref="Channel"/> is whom to,
/// by name.
/// </summary>
/// <remarks>
/// Its strings are unset (null) until assigned, not defaulted, as <see cref="ActionEntity"/>'s are. The model in
/// <see cref="GameDbContext"/> keeps those columns required.
/// </remarks>
public sealed class ChatEntity
{
    public string Id { get; set; } = null!;

    public string TableId { get; set; } = null!;

    public int Number { get; set; }

    public int Seat { get; set; }

    public string Channel { get; set; } = null!;

    public string Text { get; set; } = null!;

    public DateTimeOffset At { get; set; }
}
