namespace Net.NowhereAtAll.Zaturnava.Web;

/// <summary>Where the player is: at a game, holding one of its seats, or none of them, to watch.</summary>
public sealed record Seating(string GameId, SeatClaim? Claim)
{
    public static Seating Watching(string gameId) => new(gameId, null);
}

/// <summary>
/// A seat the player claimed, and the token that holds it. The token lives only in this page;
/// reloading the page gives the seat up.
/// </summary>
public sealed record SeatClaim(int Seat, string Token);
