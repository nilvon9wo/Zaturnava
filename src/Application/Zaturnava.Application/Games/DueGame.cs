namespace Net.NowhereAtAll.Zaturnava.Application.Games;

/// <summary>
/// A game waiting for one of the server's bots, by its id and its ruleset (<see cref="GameService.PlayIdleBots"/>).
/// </summary>
public sealed record DueGame(GameId Id, string RulesetId);
