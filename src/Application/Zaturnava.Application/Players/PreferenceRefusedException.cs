using Net.NowhereAtAll.Zaturnava.Application.Games;

namespace Net.NowhereAtAll.Zaturnava.Application.Players;

/// <summary>
/// A setting a player asked to keep (<see cref="PlayerService.Prefer"/>) was unnamed, too long or one too many.
/// </summary>
public sealed class PreferenceRefusedException()
    : GameRequestException(
        $"A setting needs a name; its name and value may be at most {PlayerService.MaximumPreferenceLength} "
            + $"characters each, and a player may keep at most {PlayerService.MostPreferences}."
    )
{
    public override GameRequestFailure Failure => GameRequestFailure.Invalid;
}
