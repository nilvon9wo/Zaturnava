using Net.NowhereAtAll.Zaturnava.Engine.Seats;

namespace Net.NowhereAtAll.Zaturnava.Application.Games;

/// <summary>
/// The game paused for <paramref name="Seat"/>, a person's, to see the leader face-off it may now call, for
/// <paramref name="Left"/> more: no one else may act, and no bot decides, meanwhile (<see cref="FaceOffOpening"/>).
/// </summary>
public sealed record FaceOffPause(SeatId Seat, TimeSpan Left);
