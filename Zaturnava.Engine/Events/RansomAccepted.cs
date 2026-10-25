using Net.NowhereAtAll.Zaturnava.Engine.Pieces;

namespace Net.NowhereAtAll.Zaturnava.Engine.Events;

/// <summary>The ransom was accepted; its prisoners go home.</summary>
public sealed record RansomAccepted(RansomOffer Offer) : GameEvent;
