using System.Collections.Immutable;
using Net.NowhereAtAll.Zaturnava.Engine.Seats;

namespace Net.NowhereAtAll.Zaturnava.Engine.Pieces;

/// <summary>
/// Prisoners a ransom sends home, one after another, waiting on where the first is to go; once all are
/// home, <see cref="Resume"/> decides again.
/// </summary>
public sealed record ReturnsDue(ImmutableList<Prisoner> Prisoners, SeatId Resume);
