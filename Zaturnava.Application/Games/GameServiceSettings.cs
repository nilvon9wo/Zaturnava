namespace Net.NowhereAtAll.Zaturnava.Application.Games;

/// <summary>
/// <see cref="MaximumBotSteps"/> caps how many bot decisions one request may run, so that a game
/// of bots only cannot keep a request going forever.
/// </summary>
public sealed record GameServiceSettings(int MaximumBotSteps)
{
    public const int DefaultMaximumBotSteps = 200;

    public static readonly GameServiceSettings Default = new(DefaultMaximumBotSteps);
}
