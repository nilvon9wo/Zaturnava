using Net.NowhereAtAll.Zaturnava.Engine.Seats;

namespace Net.NowhereAtAll.Zaturnava.Engine.Events;

/// <summary><see cref="Commander"/> now commands <see cref="Army"/>'s army, because of <see cref="Basis"/>.</summary>
public sealed record CommandBegun(SeatId Commander, SeatId Army, CommandBasis Basis) : GameEvent;
