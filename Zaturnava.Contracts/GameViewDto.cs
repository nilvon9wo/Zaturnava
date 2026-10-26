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
/// throne of the seat deciding for itself, which may change what that seat may do, and who moves.
/// <see cref="Proposal"/> is a proposal to end the game, waiting on the deciding seat's answer.
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
    EndProposalDto? Proposal = null
);
