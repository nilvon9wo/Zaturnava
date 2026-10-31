namespace Net.NowhereAtAll.Zaturnava.Web;

/// <summary>How the lobby sets up a new game: at once, by how strong each bot is, or seat by seat.</summary>
internal enum SetupMode
{
    Quick,
    ByStrength,
    SeatBySeat,
}
