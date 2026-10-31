using System.ComponentModel.DataAnnotations;
using Net.NowhereAtAll.Zaturnava.Application.Players;

namespace Net.NowhereAtAll.Zaturnava.Application.Games;

/// <summary>
/// A seat claimed by whoever holds its token - through any client, or an AI over MCP. Only the
/// token's hash is kept; and, if whoever claimed it was signed in, the player they are, the <see cref="Claimant"/>;
/// if not, the <see cref="Nickname"/> they gave, if any.
/// </summary>
public sealed record HumanSeat(string TokenHash, PlayerId? Claimant = null, string? Nickname = null) : SeatController
{
    public const string KindName = "human";

    /// <summary>What marks, in <see cref="Detail"/>, a guest's nickname rather than a player's id.</summary>
    private const char NicknameMark = '~';

    public override string Kind => KindName;

    /// <summary>
    /// The token's hash; then, after a space, the claimant's id, if a player claimed the seat, or else
    /// <see cref="NicknameMark"/> and the guest's nickname, if they gave one.
    /// </summary>
    public override string Detail =>
        (this.Claimant, this.Nickname) switch
        {
            ({ } claimant, _) => $"{this.TokenHash} {claimant.Value}",
            (_, { } nickname) => $"{this.TokenHash} {NicknameMark}{nickname}",
            _ => this.TokenHash,
        };

    public override string Player => GameHistory.Person;

    public override bool IsGuest => this.Claimant is null;

    public override bool IsHeldBy([Required] string token) => SeatTokens.Hash(token) == this.TokenHash;

    /// <summary>The seat as <see cref="Detail"/> stores it.</summary>
    public static HumanSeat From([Required] string detail) => Of(detail.Split(' ', 2));

    private static HumanSeat Of(string[] parts) => Of(parts[0], parts.ElementAtOrDefault(1));

    private static HumanSeat Of(string hash, string? rest) =>
        rest switch
        {
            null => new HumanSeat(hash),
            [NicknameMark, .. { } nickname] => new HumanSeat(hash, Nickname: nickname),
            _ => new HumanSeat(hash, new PlayerId(rest)),
        };
}
