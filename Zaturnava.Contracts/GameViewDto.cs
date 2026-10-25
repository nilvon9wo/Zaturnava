using System.Collections.Immutable;

namespace Net.NowhereAtAll.Zaturnava.Contracts;

/// <summary>
/// A game as any client shows it: the board, the pieces, the seats, whose decision it is (null once
/// the game is over), the actions that seat may take, the dice it has rolled this turn and not used
/// yet, and the latest of what has happened, oldest first.
/// </summary>
public sealed record GameViewDto(
    string Id,
    string RulesetId,
    string RulesetName,
    int Version,
    int TurnNumber,
    int? DecidingSeat,
    ImmutableList<CellDto> Cells,
    ImmutableList<PieceDto> Pieces,
    ImmutableList<SeatDto> Seats,
    ImmutableList<ActionDto> LegalActions,
    ImmutableList<DieDto> Dice,
    ImmutableList<EventDto> Log
);
