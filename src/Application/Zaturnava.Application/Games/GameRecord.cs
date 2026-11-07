using System.Collections.Immutable;
using Net.NowhereAtAll.Zaturnava.Application.Players;
using Net.NowhereAtAll.Zaturnava.Engine.Actions;
using Net.NowhereAtAll.Zaturnava.Engine.Seats;

namespace Net.NowhereAtAll.Zaturnava.Application.Games;

/// <summary>
/// A game as stored: its ruleset, who controls each seat (by seat number), and every action taken,
/// in order. The state is never stored; it is replayed from the actions. <see cref="Version"/>
/// counts the saves, so that two concurrent changes cannot both win. Beside them, what a lobby lists a game by,
/// kept so that it need not replay it: whether it is over, when it was created and when it last changed, and who is
/// away.
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

    /// <summary>When the game was created; none, for a game created before this was kept.</summary>
    public DateTimeOffset? Created { get; init; }

    /// <summary>When the game was last saved; none, for a game saved before this was kept.</summary>
    public DateTimeOffset? LastChanged { get; init; }

    /// <summary>
    /// The seats away, as last saved, in seat order: a person's (or no one's yet) whose turns have passed while they
    /// were away, until they say they are back; none, for a game not saved since this was kept.
    /// </summary>
    public ImmutableList<SeatId> Away { get; init; } = [];

    /// <summary>
    /// Whether any seat has changed hands since the game began (a bot taking over from someone away, a person from a
    /// bot, a seat said anew to be another player's): from then on it counts toward no one's Elo (Stated).
    /// </summary>
    public bool HasChangedHands { get; init; }

    public GameRecord Next() => this with { Version = this.Version + 1 };

    /// <summary>
    /// Whether <paramref name="player"/> claimed a seat of the game signed in, whether they or their AI play it.
    /// </summary>
    public bool IsClaimedBy(PlayerId player) =>
        this.Controllers.Any(controller => controller is HumanSeat human && human.Claimant == player);
}
