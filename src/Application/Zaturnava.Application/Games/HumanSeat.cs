using System.ComponentModel.DataAnnotations;

namespace Net.NowhereAtAll.Zaturnava.Application.Games;

/// <summary>
/// A seat claimed by whoever holds its token - through any client, or an AI over MCP. Only the
/// token's hash is kept.
/// </summary>
public sealed record HumanSeat(string TokenHash) : SeatController
{
    public const string KindName = "human";

    public override string Kind => KindName;

    public override string Detail => this.TokenHash;

    public override string Player => GameHistory.Person;

    public override bool IsHeldBy([Required] string token) => SeatTokens.Hash(token) == this.TokenHash;
}
