using Net.NowhereAtAll.Zaturnava.Engine.Pieces;

namespace Net.NowhereAtAll.Zaturnava.Engine.Events;

/// <summary>The ransom was declined; every prisoner stays where it is.</summary>
public sealed record RansomDeclined(RansomOffer Offer) : GameEvent;
