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
/// leave for later.
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
    int? PromotionDue
);
