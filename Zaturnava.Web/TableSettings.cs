namespace Net.NowhereAtAll.Zaturnava.Web;

/// <summary>
/// How often a table asks the server for the game while other players decide: there is no push
/// channel yet, so it polls.
/// </summary>
internal sealed record TableSettings(TimeSpan PollInterval);
