using Net.NowhereAtAll.Zaturnava.Application.Games;

namespace Net.NowhereAtAll.Zaturnava.Application.Players;

/// <summary>A link for a profile that is not a web address (http or https), or is too long.</summary>
public sealed class BadLinkException(string link)
    : GameRequestException(
        $"'{link}' is not a web address to link to: give one starting https:// or http://, of at most "
        + $"{PlayerService.MaximumLinkLength} characters."
    )
{
    public override GameRequestFailure Failure => GameRequestFailure.Invalid;
}
