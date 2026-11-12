namespace Net.NowhereAtAll.Zaturnava.Application.Games;

/// <summary>
/// The game's actions no longer replay by its ruleset's rules, which have changed since it began (a preview's
/// armies dealt anew, say): it can no longer be shown or played.
/// </summary>
public sealed class GameUnreplayableException(GameId id)
    : GameRequestException(
        $"Game '{id.Value}' began under rules that have changed since, so it can no longer be played. "
            + "Sorry: start a new one."
    )
{
    public override GameRequestFailure Failure => GameRequestFailure.Gone;
}
