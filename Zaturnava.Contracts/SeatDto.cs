using System.Collections.Immutable;

namespace Net.NowhereAtAll.Zaturnava.Contracts;

/// <summary>
/// A seat: its name, its team, its score, who plays it - <c>open</c>, <c>human</c> or <c>bot</c>,
/// with <see cref="Bot"/> naming the bot for the last, and <see cref="BotId"/> saying which bot it is,
/// to start another game with it - and the pieces it holds prisoner.
/// </summary>
public sealed record SeatDto(
    int Id,
    string Name,
    int Team,
    int Score,
    string Controller,
    string? Bot,
    ImmutableList<PrisonerDto> Prisoners,
    string? BotId = null
);
