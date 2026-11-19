using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;

namespace Net.NowhereAtAll.Zaturnava.Contracts;

/// <summary>
/// A new game: its ruleset, and for each seat in order, the bot that plays it, or null to leave it
/// open for a person to claim. Both are required: a request without them is refused as it is read.
/// </summary>
public sealed record CreateGameRequest([Required] string RulesetId, [Required] ImmutableList<string?> Bots)
{
    /// <summary>The scenario to start from (<see cref="CatalogDto.Scenarios"/>); null to start a new game.</summary>
    public string? Scenario { get; init; }

    /// <summary>
    /// The game this one is played again after, if any: the new game is at the same table, so what was said there
    /// goes on.
    /// </summary>
    public string? Continuing { get; init; }

    /// <summary>
    /// For a game whose armies are dealt (<see cref="RulesetDto.IsDealt"/>): every seat the same army, laid out alike
    /// in its own kingdom; each its own, unless asked.
    /// </summary>
    public bool AreArmiesAlike { get; init; }

    /// <summary>
    /// For a game whose armies are dealt: the game whose armies, as they were dealt, this one is dealt again (played
    /// again with the same armies); null to deal anew.
    /// </summary>
    public string? ArmiesOf { get; init; }

    /// <summary>
    /// For a game whose players may draft their own armies (<see cref="RulesetDto.IsDraftable"/>): whether they do, in
    /// turn, before play begins, rather than be dealt them. Dealt, unless asked.
    /// </summary>
    public bool IsDrafted { get; init; }

    /// <summary>
    /// What each seat's army has to spend, one value for everyone, drafted or dealt at random; the ruleset's own
    /// (<see cref="RulesetDto.DraftBudget"/>), if null.
    /// </summary>
    [Range(1, 100_000)]
    public int? DraftBudget { get; init; }

    /// <summary>
    /// For armies drafted: the seat that alone drafts, one army that every seat then has alike (the game's creator's,
    /// as the lobby asks it); null, for each seat to draft its own.
    /// </summary>
    [Range(0, 25)]
    public int? Drafter { get; init; }

    /// <summary>
    /// For armies each seat drafts its own: whether in secret, no one shown another team's palace, throne, pieces or
    /// points left until every army is drafted (<see cref="GameViewDto.IsDraftSecret"/>).
    /// </summary>
    public bool IsDraftSecret { get; init; }

    /// <summary>
    /// For armies drafted in secret: whether from allies too, each seat shown no army but its own until every army is
    /// drafted (<see cref="GameViewDto.IsDraftHiddenFromAllies"/>); else allies see each other's.
    /// </summary>
    public bool IsDraftHiddenFromAllies { get; init; }

    /// <summary>
    /// For a game whose creator may set its board's size (<see cref="RulesetDto.SizableBoard"/>), and whose armies are
    /// dealt: the size they set; null, for the ruleset's own.
    /// </summary>
    public BoardSizeDto? Board { get; init; }

    /// <summary>
    /// For a game whose creator may say so (<see cref="RulesetDto.MayPromoteAtSide"/>), and whose armies are dealt:
    /// whether pieces promoted at the far edge are promoted at the edge to their right too, as their army faces
    /// (<see cref="GameViewDto.PromotesAtSide"/>). Not, unless asked.
    /// </summary>
    public bool PromotesAtSide { get; init; }

    /// <summary>
    /// Whether the seats left open are only for players signed in: a guest may not take one
    /// (<see cref="GameSummaryDto.IsForPlayers"/>). Open to anyone, unless asked.
    /// </summary>
    public bool IsForPlayers { get; init; }

    /// <summary>
    /// For a game that rolls dice (<see cref="RulesetDto.HasDice"/>), not from a scenario: whether it is played without
    /// them, a pure abstract strategy game (<see cref="GameViewDto.AreDiceOff"/>). With dice, unless asked.
    /// </summary>
    public bool AreDiceOff { get; init; }

    /// <summary>
    /// For a game whose creator may set it (<see cref="RulesetDto.IdleRounds"/>), not from a scenario: how many rounds
    /// in a row no one may move before the game ends on points, at least 1 (<see cref="GameViewDto.IdleRounds"/>).
    /// Null, for the server's own.
    /// </summary>
    public int? IdleRounds { get; init; }

    /// <summary>
    /// For a game played without dice (<see cref="AreDiceOff"/>): the seat, from 0, that opens it. Null, for one the
    /// server draws at random.
    /// </summary>
    public int? Opener { get; init; }
}
