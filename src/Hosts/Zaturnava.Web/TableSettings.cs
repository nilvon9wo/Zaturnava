namespace Net.NowhereAtAll.Zaturnava.Web;

/// <summary>
/// How a table keeps up with the game: there is no push channel yet, so it asks the server for the
/// game every <see cref="PollInterval"/> while a person elsewhere decides, and lets a deciding bot take
/// its next step every <see cref="BotPace"/>, slowly enough for people to follow. While a decision is
/// timed, it counts down every <see cref="CountdownStep"/>, and advances the game once time is up.
/// </summary>
internal sealed record TableSettings(TimeSpan PollInterval, TimeSpan BotPace, TimeSpan CountdownStep);
