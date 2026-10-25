namespace Net.NowhereAtAll.Zaturnava.Application.Games;

/// <summary>
/// <see cref="MaximumBotSteps"/> caps how many bot decisions a request to create a game or take an
/// action runs before it answers. The default is none: bots then play one
/// <see cref="GameService.Advance"/> at a time, at whatever pace the clients advance them, so that
/// people can follow every turn. A host for scripts or self-play may raise it.
/// </summary>
public sealed record GameServiceSettings(int MaximumBotSteps)
{
    public const int DefaultMaximumBotSteps = 0;

    public static readonly GameServiceSettings Default = new(DefaultMaximumBotSteps);
}
