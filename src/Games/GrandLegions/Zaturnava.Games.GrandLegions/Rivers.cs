namespace Net.NowhereAtAll.Zaturnava.Games.GrandLegions;

/// <summary>
/// The rivers on the board (specification §2): each runs along a midline, between two rows or columns of
/// cells, so no cell is on it.
/// </summary>
[Flags]
public enum Rivers
{
    None = 0,

    /// <summary>A river running north to south, parting the west half of the board from the east.</summary>
    NorthSouth = 1,

    /// <summary>A river running east to west, parting the south half of the board from the north.</summary>
    EastWest = 2,

    Both = NorthSouth | EastWest,
}
