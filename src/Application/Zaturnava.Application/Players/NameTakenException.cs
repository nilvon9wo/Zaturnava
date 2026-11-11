using Net.NowhereAtAll.Zaturnava.Application.Games;

namespace Net.NowhereAtAll.Zaturnava.Application.Players;

/// <summary>
/// Someone else already goes by the name a player or guest would take, capitals or not: a player by their username,
/// or a guest by the alias they hold (Stated).
/// </summary>
public sealed class NameTakenException(string name)
    : GameRequestException($"Someone already goes by {name}: choose another name.")
{
    public override GameRequestFailure Failure => GameRequestFailure.Conflict;
}
