using System.Collections.Immutable;
using Net.NowhereAtAll.Zaturnava.Engine.Actions;

namespace Net.NowhereAtAll.Zaturnava.Application.Games;

/// <summary>
/// A game as stored: its ruleset, who controls each seat (by seat number), and every action taken,
/// in order. The state is never stored; it is replayed from the actions. <see cref="Version"/>
/// counts the saves, so that two concurrent changes cannot both win. Beside them, what a lobby lists a game by,
/// kept so that it need not replay it: whether it is over, and when it last changed.
/// </summary>
public sealed record GameRecord(
    GameId Id,
    string RulesetId,
    ImmutableList<SeatController> Controllers,
    ImmutableList<ActionDescriptor> Actions,
    int Version
)
{
    /// <summary>
    /// Whether the game waits for people to take its open seats: until every seat is held, nothing is played,
    /// by a person or a bot, and no decision is timed.
    /// </summary>
    public bool IsWaiting => this.Controllers.Any(controller => controller.IsOpen);

    /// <summary>
    /// Whether the game has ended, as last saved; none, for a game saved before this was kept, until it is worked out
    /// (<see cref="GameService.Summarize"/>).
    /// </summary>
    public bool? IsOver { get; init; }

    /// <summary>When the game was last saved; none, for a game saved before this was kept.</summary>
    public DateTimeOffset? LastChanged { get; init; }

    public GameRecord Next() => this with { Version = this.Version + 1 };
}
