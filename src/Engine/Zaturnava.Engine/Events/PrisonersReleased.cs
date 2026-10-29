using Net.NowhereAtAll.Zaturnava.Engine.Pieces;

namespace Net.NowhereAtAll.Zaturnava.Engine.Events;

/// <summary>
/// Prisoners of the releasing seat's own team go home, given back with no one's consent asked: its own, or an
/// ally's whose throne no enemy holds.
/// </summary>
public sealed record PrisonersReleased(RansomOffer Offer) : GameEvent;
