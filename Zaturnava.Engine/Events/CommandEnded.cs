using Net.NowhereAtAll.Zaturnava.Engine.Seats;

namespace Net.NowhereAtAll.Zaturnava.Engine.Events;

/// <summary><see cref="Commander"/> no longer commands <see cref="Army"/>'s army.</summary>
public sealed record CommandEnded(SeatId Commander, SeatId Army) : GameEvent;
