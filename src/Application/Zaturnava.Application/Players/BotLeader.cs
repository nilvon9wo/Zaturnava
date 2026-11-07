namespace Net.NowhereAtAll.Zaturnava.Application.Players;

/// <summary>A bot on a leaderboard: its name, as offered, and where it stands.</summary>
public sealed record BotLeader(string Name, BotStanding Standing);
