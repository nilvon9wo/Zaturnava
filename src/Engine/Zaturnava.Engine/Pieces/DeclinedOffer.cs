using Net.NowhereAtAll.Zaturnava.Engine.Positions;

namespace Net.NowhereAtAll.Zaturnava.Engine.Pieces;

/// <summary>
/// A ransom declined on turn <paramref name="TurnNumber"/>, with the board as it stood then, so that a player
/// can tell whether enough has changed since to ask again.
/// </summary>
public sealed record DeclinedOffer(RansomOffer Offer, int TurnNumber, Position Board);
