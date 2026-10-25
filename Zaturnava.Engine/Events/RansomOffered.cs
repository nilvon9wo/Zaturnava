using Net.NowhereAtAll.Zaturnava.Engine.Pieces;

namespace Net.NowhereAtAll.Zaturnava.Engine.Events;

/// <summary>A ransom was offered; the game waits on its receiver's answer.</summary>
public sealed record RansomOffered(RansomOffer Offer) : GameEvent;
