using System.Collections.Immutable;

namespace Net.NowhereAtAll.Zaturnava.Contracts;

/// <summary>
/// A game as any client shows it: the board, the pieces, the seats, whose decision it is (null once
/// the game is over) and the seat it decides for if not its own, the actions it may take, the dice
/// rolled this turn and not used yet, the latest of what has happened, oldest first, and how the
/// game ended, once it has. While a ransom waits on its answer, <see cref="Offer"/> is it;
/// <see cref="MayOffer"/> says whether the deciding seat may offer one (an <c>offer</c> action, not
/// among the legal actions); and <see cref="SecondsLeft"/>, if its decision is timed, how long it
/// has left, after which anyone may advance the game to decide for it. <see cref="PromotionDue"/> is
/// the cell of a Pawn just come to the far edge, which the deciding seat promotes now or passes to
/// leave for later. <see cref="IsTurnOver"/> says that the deciding seat's turn is over, and it may
/// only offer ransoms, or be done. <see cref="Returning"/> is a piece going home from a ransom, which
/// waits on the deciding seat, its owner, to choose where it goes (a <c>place</c> action, drawn on the
/// board as a cell to drop it on). <see cref="ThroneHeldBy"/> is the enemy seat whose leader sits on the
/// throne of the seat whose turn the deciding seat plays (its own, or an ally's it acts for), which may
/// change what it may do, and who moves.
/// <see cref="Proposal"/> is a proposal to end the game, waiting on the deciding seat's answer;
/// <see cref="Away"/>, the vote on whether to stop waiting for the deciding seat, if there is one.
/// <see cref="Vacancy"/> says, while the game waits for its open seats to be taken, who may fill them with bots, and
/// when.
/// <see cref="Kinds"/> tells of each kind of piece on the board or held prisoner, for a player looking at one.
/// </summary>
public sealed record GameViewDto(
    string Id,
    string RulesetId,
    string RulesetName,
    int Version,
    int TurnNumber,
    int? DecidingSeat,
    int? ActingFor,
    ImmutableList<CellDto> Cells,
    ImmutableList<PieceDto> Pieces,
    ImmutableList<SeatDto> Seats,
    ImmutableList<ActionDto> LegalActions,
    ImmutableList<DieDto> Dice,
    ImmutableList<EventDto> Log,
    OutcomeDto? Outcome,
    bool MayOffer,
    OfferDto? Offer,
    double? SecondsLeft,
    int? PromotionDue,
    bool IsTurnOver,
    PrisonerDto? Returning = null,
    int? ThroneHeldBy = null,
    EndProposalDto? Proposal = null,
    AwayDto? Away = null,
    VacancyDto? Vacancy = null
)
{
    public ImmutableList<PieceKindDto> Kinds { get; init; } = [];

    /// <summary>The lines drawn on the board, such as its rivers; none, for most games.</summary>
    public ImmutableList<LineDto> Lines { get; init; } = [];

    /// <summary>What a player should know of the board beyond its cells, as its rivers; null if nothing.</summary>
    public string? Terrain { get; init; }

    /// <summary>
    /// Whether the game was dealt its armies at random as it began, so that it may be played again with the same
    /// armies or new ones (<see cref="CreateGameRequest.ArmiesOf"/>).
    /// </summary>
    public bool IsDealt { get; init; }

    /// <summary>Whether every seat was dealt the same army (<see cref="CreateGameRequest.AreArmiesAlike"/>).</summary>
    public bool AreArmiesAlike { get; init; }

    /// <summary>Whether its players drafted their own armies (<see cref="CreateGameRequest.IsDrafted"/>).</summary>
    public bool IsDrafted { get; init; }

    /// <summary>
    /// For a game whose players drafted: the budget its creator set (<see cref="CreateGameRequest.DraftBudget"/>);
    /// null, for the ruleset's own.
    /// </summary>
    public int? DraftBudget { get; init; }

    /// <summary>
    /// For a game whose players drafted: the seat that alone drafted one army for everyone
    /// (<see cref="CreateGameRequest.Drafter"/>); null, if each drafted their own.
    /// </summary>
    public int? Drafter { get; init; }

    /// <summary>
    /// For a game whose players drafted: whether in secret (<see cref="CreateGameRequest.IsDraftSecret"/>). While the
    /// armies are drafted, the game is shown to the holder of a seat's token as that seat's team sees it: no other
    /// team's pieces, throne, palace or points left (null in <see cref="DraftLeft"/>), nor what it drafted in the log,
    /// nor its choices while it drafts. Asked for without a token, no seat's.
    /// </summary>
    public bool IsDraftSecret { get; init; }

    /// <summary>
    /// The board's size its creator set (<see cref="CreateGameRequest.Board"/>); null, for the ruleset's own.
    /// </summary>
    public BoardSizeDto? Board { get; init; }

    /// <summary>
    /// While the armies are drafted, what each seat has left to spend, seat by seat (null for a seat drafting in
    /// secret from the one asking, <see cref="IsDraftSecret"/>); null once play has begun, or in a game never drafted.
    /// </summary>
    public ImmutableList<int?>? DraftLeft { get; init; }

    /// <summary>
    /// While the armies are drafted, every kind a player may draft, at its price, whether or not they can pay for it
    /// yet: the catalogue they choose from. Empty at any other time.
    /// </summary>
    public ImmutableList<PieceKindDto> DraftKinds { get; init; } = [];

    /// <summary>
    /// Whether its seats for people were only for players signed in (<see cref="CreateGameRequest.IsForPlayers"/>),
    /// whether still open or since taken; so the game played again is too.
    /// </summary>
    public bool IsForPlayers { get; init; }
}
