namespace Net.NowhereAtAll.Zaturnava.Games.GrandDraft;

/// <summary>
/// The game a piece comes from: Western chess, Xiangqi, Janggi, Shogi or Makruk; or none of them, a fairy
/// piece (old, or made up). A bot may prefer the pieces of one.
/// </summary>
public enum Tradition
{
    Western,
    Xiangqi,
    Janggi,
    Shogi,
    Makruk,
    Fairy,
}
