namespace SampleBot;

/// <summary>
/// The bot's mind: given the game as it stands and its own seat, which of the legal actions to take. This is
/// the one part of the sample to replace with an AI of your own; everything else (the API, the loop) stays.
/// </summary>
/// <remarks>
/// Two ways to put an AI here:
/// <list type="bullet">
/// <item>
/// A language model: send it the view (or a summary of it: the board, the scores, the dice) and the legal actions,
/// numbered, and ask for one number back. Check that it is one of them, and fall back on <see cref="Greedy"/> if
/// not, so that a bad answer never stalls the game. Keep the model's key in the environment, never in code.
/// </item>
/// <item>
/// A model of your own, as the server's bot Athena is: a small neural network that judges a position. Score the
/// position each legal action leads to and take the best. The server's game histories (GET api/history) are data
/// to train it on; docs/bots/self-play.md describes playing thousands of games offline, against the server's bots.
/// </item>
/// </list>
/// Either way, choose only among <see cref="GameView.LegalActions"/>: the server refuses anything else.
/// </remarks>
internal interface IChoosing
{
    GameAction Choose(GameView view, int seat);
}
