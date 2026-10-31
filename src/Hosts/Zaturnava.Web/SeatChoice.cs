namespace Net.NowhereAtAll.Zaturnava.Web;

/// <summary>
/// How a seat set up by strength is to be played: by a bot of <see cref="Strength"/>, the one chosen
/// (<see cref="Bot"/>) or any; or, without a strength, by a person.
/// </summary>
internal sealed record SeatChoice(Strength? Strength, string? Bot = null);
