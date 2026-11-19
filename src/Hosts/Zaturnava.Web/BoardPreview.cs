using System.Collections.Immutable;
using Net.NowhereAtAll.Zaturnava.Contracts;

namespace Net.NowhereAtAll.Zaturnava.Web;

/// <summary>
/// What placing something on a square the player hovers over would put there, drawn there as a preview, greyed and
/// blinking, until they click or move on (the user's report of 2026-11-15, I): <paramref name="Piece"/>, on its cell;
/// a palace's <see cref="Lines"/>; or a throne of the seat <see cref="ThroneOf"/>. A move taking another piece along,
/// as castling takes the Rook, shows that piece where it goes too (<see cref="Partner"/>).
/// </summary>
public sealed record BoardPreview(PieceDto? Piece = null)
{
    public ImmutableList<LineDto> Lines { get; init; } = [];

    public PieceDto? Partner { get; init; }

    public int? ThroneOf { get; init; }
}
