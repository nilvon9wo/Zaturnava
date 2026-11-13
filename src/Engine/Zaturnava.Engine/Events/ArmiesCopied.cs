using Net.NowhereAtAll.Zaturnava.Engine.Seats;

namespace Net.NowhereAtAll.Zaturnava.Engine.Events;

/// <summary>
/// <see cref="Drafter"/> drafted for every seat: each other army is given the same pieces, throne and palace, in its
/// own kingdom.
/// </summary>
public sealed record ArmiesCopied(SeatId Drafter) : GameEvent;
