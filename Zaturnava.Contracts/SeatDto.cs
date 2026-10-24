namespace Net.NowhereAtAll.Zaturnava.Contracts;

/// <summary>
/// A seat: its name, its team, its score, and who plays it - <c>open</c>, <c>human</c> or <c>bot</c>,
/// with <see cref="Bot"/> naming the bot for the last.
/// </summary>
public sealed record SeatDto(int Id, string Name, int Team, int Score, string Controller, string? Bot);
