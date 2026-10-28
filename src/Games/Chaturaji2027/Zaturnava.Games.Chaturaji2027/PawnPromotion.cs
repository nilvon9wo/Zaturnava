namespace Net.NowhereAtAll.Zaturnava.Games.Chaturaji2027;

/// <summary>Whether a Pawn may be promoted into a captured Pawn (rules.md §8, <c>promotion.pawns</c>).</summary>
public enum PawnPromotion
{
    /// <summary>Into another army's Pawn, which then moves the way its own army faces. The default.</summary>
    OtherArmies,

    /// <summary>Never into a Pawn.</summary>
    Never,
}
