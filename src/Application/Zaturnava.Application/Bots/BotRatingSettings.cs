namespace Net.NowhereAtAll.Zaturnava.Application.Bots;

/// <summary>
/// How the server rates its bots (<see cref="BotRatingService"/>), all Proposed: the bot rated
/// <see cref="BotRatingFit.AnchorRating"/> (<paramref name="Anchor"/>); the most decisions a game may take before
/// it goes to the side with more points (<paramref name="MaximumSteps"/>); and how many games each pair of bots must
/// have played at a ruleset before the ratings fitted there replace the ones the server shipped with
/// (<paramref name="MinimumGames"/>).
/// </summary>
public sealed record BotRatingSettings(string Anchor, int MaximumSteps = 2000, int MinimumGames = 2);
