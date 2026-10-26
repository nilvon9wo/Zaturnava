using System.Collections.Immutable;

namespace Net.NowhereAtAll.Zaturnava.Contracts;

/// <summary>
/// A seat: its name, its team, its score, who plays it - <c>open</c>, <c>human</c> or <c>bot</c>,
/// with <see cref="Bot"/> naming the bot for the last, and <see cref="BotId"/> saying which bot it is,
/// to start another game with it - and the pieces it holds prisoner. <see cref="HasGivenUp"/> says that
/// it gave up its turns, proposing to end the game; <see cref="MayProposeEnd"/>, that it may propose so
/// now.
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
    bool MayProposeEnd = false
);
