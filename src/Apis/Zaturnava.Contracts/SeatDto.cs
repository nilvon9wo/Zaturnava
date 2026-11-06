using System.Collections.Immutable;

namespace Net.NowhereAtAll.Zaturnava.Contracts;

/// <summary>
/// A seat: its name, its team, its score, who plays it - <c>open</c>, <c>human</c> or <c>bot</c>,
/// with <see cref="Bot"/> naming the bot for the last, and <see cref="BotId"/> saying which bot it is,
/// to start another game with it - and the pieces it holds prisoner. <see cref="HasGivenUp"/> says that
/// it gave up its turns, proposing to end the game; <see cref="MayProposeEnd"/>, that it may propose so
/// now; <see cref="MayRejoin"/>, that it may take its turns back, or say it is back, now; and
/// <see cref="TurnsMissed"/>, how many of its turns in a row passed while it was away.
/// <see cref="IsPaidBot"/> says that its bot costs tokens for each use; <see cref="IsRetired"/>, that it
/// retired, ending its part in the game. <see cref="Player"/> says who holds a person's seat: the name of the player
/// who claimed it signed in, <c>deleted player</c> for one who has since deleted their account; for someone who did
/// not sign in, <c>guest</c>, or their nickname as <c>Nickname (guest)</c>; null for an open seat or a bot's.
/// <see cref="IsGuest"/> says that a person holds it without having signed in, and so may read the table's talk but
/// not speak; <see cref="IsAi"/>, that an AI, or another program, plays it, as whoever claimed it said.
/// <see cref="PlayerId"/> is the id of the player who claimed it signed in, whose profile anyone may see
/// (<see cref="GameRoutes.Profile"/>); null for anyone else.
/// </summary>
public sealed record SeatDto(
    int Id,
    string Name,
    int Team,
    int Score,
    string Controller,
    string? Bot,
    ImmutableList<PrisonerDto> Prisoners,
    string? BotId = null,
    bool HasGivenUp = false,
    bool MayProposeEnd = false,
    bool MayRejoin = false,
    int TurnsMissed = 0,
    bool IsPaidBot = false,
    bool IsRetired = false,
    string? Player = null,
    bool IsGuest = false,
    bool IsAi = false,
    string? PlayerId = null
)
{
    /// <summary>
    /// What the seat may do now, whoever decides, for as long as the chance holds, as a leader face-off; each submitted
    /// as any action is, its <see cref="ActionDto.Label"/> saying what it does. Empty, for none.
    /// </summary>
    public ImmutableList<ActionDto> Chances { get; init; } = [];

    /// <summary>
    /// Which way is forward for the seat's pieces, on the board as it lies, unturned: up, for the seat whose pieces
    /// move up the board. A piece's move diagram (<see cref="MoveDiagramDto"/>) is drawn with forward up, so a client
    /// turns it this way to show it as the board lies. Up, unless set.
    /// </summary>
    public DirectionDto Forward { get; init; } = new(0, 1);
}
