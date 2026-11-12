using System.Collections.Immutable;

namespace Net.NowhereAtAll.Zaturnava.Games.Chaturaji2027;

/// <summary>
/// What an army filled at random gains: the pieces <paramref name="Added"/>, on squares of its kingdom left free, and
/// its <paramref name="Throne"/>, as it was, or set, if it was not.
/// </summary>
public sealed record ArmyFilled(ImmutableList<ArmyPiece> Added, ArmySquare Throne);
