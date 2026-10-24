using System.Collections.Immutable;
using Net.NowhereAtAll.Zaturnava.Engine.Actions;

namespace Net.NowhereAtAll.Zaturnava.Application.Games;

/// <summary>
/// A game as stored: its ruleset, who controls each seat (by seat number), and every action taken,
/// in order. The state is never stored; it is replayed from the actions. <see cref="Version"/>
/// counts the saves, so that two concurrent changes cannot both win.
/// </summary>
public sealed record GameRecord(
    GameId Id,
    string RulesetId,
    ImmutableList<SeatController> Controllers,
    ImmutableList<ActionDescriptor> Actions,
    int Version
)
{
    public GameRecord Next() => this with { Version = this.Version + 1 };
}
