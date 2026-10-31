using System.ComponentModel.DataAnnotations;
using Net.NowhereAtAll.Zaturnava.Application.Players;

namespace Net.NowhereAtAll.Zaturnava.Application.Games;

/// <summary>
/// A seat claimed by whoever holds its token - through any client, or an AI over MCP. Only the
/// token's hash is kept; and, if whoever claimed it was signed in, the player they are, the <see cref="Claimant"/>.
/// </summary>
public sealed record HumanSeat(string TokenHash, PlayerId? Claimant = null) : SeatController
{
    public const string KindName = "human";

    public override string Kind => KindName;

    /// <summary>The token's hash, then, after a space, the claimant's id, if a player claimed the seat.</summary>
    public override string Detail =>
        this.Claimant is { } claimant ? $"{this.TokenHash} {claimant.Value}" : this.TokenHash;

    public override string Player => GameHistory.Person;

    public override bool IsHeldBy([Required] string token) => SeatTokens.Hash(token) == this.TokenHash;

    /// <summary>The seat as <see cref="Detail"/> stores it.</summary>
    public static HumanSeat From([Required] string detail) => Of(detail.Split(' '));

    private static HumanSeat Of(string[] parts) =>
        parts.Length == 2 ? new HumanSeat(parts[0], new PlayerId(parts[1])) : new HumanSeat(parts[0]);
}
