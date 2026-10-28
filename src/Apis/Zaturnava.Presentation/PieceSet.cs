namespace Net.NowhereAtAll.Zaturnava.Presentation;

/// <summary>How the pieces are shown: by their own names and pictures, or as the chess pieces they move like.</summary>
public enum PieceSet
{
    /// <summary>Raja, Elephant, Horse, Boat and Pawn, each drawn as itself.</summary>
    Traditional,

    /// <summary>King, Rook, Knight, Boat and Pawn, drawn as chess pieces (chess has no Boat).</summary>
    Chess,
}
