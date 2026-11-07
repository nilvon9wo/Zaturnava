using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using Net.NowhereAtAll.Zaturnava.Engine.Seats;

namespace Net.NowhereAtAll.Zaturnava.Application.Games;

/// <summary>
/// A game as a lobby lists it, read as stored, without replaying it: who controls each seat, how many actions have
/// been taken, whether it is over, when it was created and last changed (none, for a game from before these
/// were kept), and who is away.
/// </summary>
public sealed record GameListing(
    GameId Id,
    string RulesetId,
    int Version,
    ImmutableList<SeatController> Controllers,
    int Moves,
    bool IsOver
)
{
    public DateTimeOffset? Created { get; init; }

    public DateTimeOffset? LastChanged { get; init; }

    /// <summary>The seats away, as <see cref="GameRecord.Away"/> says.</summary>
    public ImmutableList<SeatId> Away { get; init; } = [];

    /// <summary>The game as a lobby lists it, from the game as stored.</summary>
    public static GameListing Of([Required] GameRecord game) =>
        new(game.Id, game.RulesetId, game.Version, game.Controllers, game.Actions.Count, game.IsOver ?? false)
        {
            Created = game.Created,
            LastChanged = game.LastChanged,
            Away = game.Away,
        };
}
