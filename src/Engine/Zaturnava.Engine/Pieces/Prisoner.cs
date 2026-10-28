using Net.NowhereAtAll.Zaturnava.Engine.Seats;

namespace Net.NowhereAtAll.Zaturnava.Engine.Pieces;

/// <summary>A captured piece, and the seat that holds it prisoner.</summary>
public sealed record Prisoner(Piece Piece, SeatId Holder);
