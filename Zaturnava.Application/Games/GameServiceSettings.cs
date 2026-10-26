using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;

namespace Net.NowhereAtAll.Zaturnava.Application.Games;

/// <summary>
/// <see cref="MaximumBotSteps"/> caps how many bot decisions a request to create a game or take an
/// action runs before it answers. The default is none: bots then play one
/// <see cref="GameService.Advance"/> at a time, at whatever pace the clients advance them, so that
/// people can follow every turn. A host for scripts or self-play may raise it.
/// <see cref="Timers"/> gives how long each timed decision a ruleset names may take, by name;
/// <see cref="DefaultTimer"/>, any it does not name. <see cref="AwayGrace"/> is how long a person may take
/// over a decision no timer limits before the others may vote to stop waiting for them.
/// </summary>
public sealed record GameServiceSettings(
    int MaximumBotSteps,
    TimeSpan DefaultTimer,
    ImmutableDictionary<string, TimeSpan> Timers
)
{
    public const int DefaultMaximumBotSteps = 0;

    public static readonly TimeSpan DefaultTimerLength = TimeSpan.FromSeconds(30);

    public static readonly TimeSpan DefaultAwayGrace = TimeSpan.FromMinutes(2);

    public static readonly GameServiceSettings Default = new(DefaultMaximumBotSteps, DefaultTimerLength, []);

    public TimeSpan AwayGrace { get; init; } = DefaultAwayGrace;

    /// <summary>How long the decision timed by <paramref name="timer"/> may take.</summary>
    public TimeSpan LengthOf([Required] string timer) => this.Timers.GetValueOrDefault(timer, this.DefaultTimer);
}
