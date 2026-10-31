namespace Net.NowhereAtAll.Zaturnava.Games.GrandLegions;

/// <summary>Which way the kingdoms lie in their corners (specification §2).</summary>
public enum Facing
{
    /// <summary>Each kingdom a quarter turn from the last, each back rank on a different edge: the default.</summary>
    Pinwheel,

    /// <summary>Every back rank along the top or the bottom edge.</summary>
    TopAndBottom,
}
