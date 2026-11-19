namespace Net.NowhereAtAll.Zaturnava.Contracts;

/// <summary>
/// Another piece a move takes along, as castling takes the Rook: from the cell <paramref name="From"/> to the cell
/// <paramref name="To"/>.
/// </summary>
public sealed record PartnerMoveDto(int From, int To);
