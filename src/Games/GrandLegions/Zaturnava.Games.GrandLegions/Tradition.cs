namespace Net.NowhereAtAll.Zaturnava.Games.GrandLegions;

/// <summary>
/// The game a piece comes from: Western chess, Xiangqi, Janggi, Shogi, Makruk, Checkers or Tafl; or none of them, a
/// fairy piece (old, or made up). A bot may prefer the pieces of one. Othello has no piece of its own: its one is the
/// Evangelist, a fairy piece played in it too (<see cref="GrandPiece.AlsoIn"/>; the user's word of 2026-11-20: an
/// Othello player whose army is all Evangelists).
/// </summary>
public enum Tradition
{
    Western,
    Xiangqi,
    Janggi,
    Shogi,
    Makruk,
    Fairy,
    Checkers,
    Tafl,
    Othello,
}
