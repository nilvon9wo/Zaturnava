namespace Net.NowhereAtAll.Zaturnava.Application.Players;

/// <summary>How a game over ended for a seat, as an Elo rating scores it: lost 0, drawn a half, won 1.</summary>
internal enum GameResult
{
    Lost,
    Drawn,
    Won,
}
