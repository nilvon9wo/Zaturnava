using Net.NowhereAtAll.Zaturnava.Presentation;

namespace Net.NowhereAtAll.Zaturnava.Web;

/// <summary>The player's turn at a table: the seat they hold, and what it may do now.</summary>
internal sealed record Turn(SeatClaim Claim, BoardActions Actions);
