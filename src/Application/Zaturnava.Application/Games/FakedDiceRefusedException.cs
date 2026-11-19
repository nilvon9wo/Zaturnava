namespace Net.NowhereAtAll.Zaturnava.Application.Games;

/// <summary>
/// Dice the player chose were refused: they are taken only on a server offering scenarios (a development server), in a
/// game started from one, for a roll, each a face the die has.
/// </summary>
public sealed class FakedDiceRefusedException(string message) : GameRequestException(message)
{
    public override GameRequestFailure Failure => GameRequestFailure.Invalid;
}
