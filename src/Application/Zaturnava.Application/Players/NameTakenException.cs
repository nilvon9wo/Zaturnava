using Net.NowhereAtAll.Zaturnava.Application.Games;

namespace Net.NowhereAtAll.Zaturnava.Application.Players;

/// <summary>Another player already goes by the username a player would take, capitals or not (Stated).</summary>
public sealed class NameTakenException(string name)
    : GameRequestException($"Someone already goes by {name}: choose another username.")
{
    public override GameRequestFailure Failure => GameRequestFailure.Conflict;
}
