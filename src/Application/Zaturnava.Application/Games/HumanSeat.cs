using System.ComponentModel.DataAnnotations;
using Net.NowhereAtAll.Zaturnava.Application.Players;

namespace Net.NowhereAtAll.Zaturnava.Application.Games;

/// <summary>
/// A seat claimed by whoever holds its token - through any client, or an AI over MCP. Only the
/// token's hash is kept; and, if whoever claimed it was signed in, the player they are, the <see cref="Claimant"/>;
/// if not, the <see cref="Nickname"/> they gave, if any. <see cref="IsAi"/> says that an AI, or a program, plays it,
/// as whoever claimed it said: for a signed-in player, on their behalf.
/// </summary>
public sealed record HumanSeat(
    string TokenHash,
    PlayerId? Claimant = null,
    string? Nickname = null,
    bool IsAi = false
) : SeatController
{
    public const string KindName = "human";

    /// <summary>What marks, in <see cref="Detail"/>, a guest's nickname rather than a player's id.</summary>
    private const char NicknameMark = '~';

    /// <summary>What marks, in <see cref="Detail"/>, a seat an AI plays.</summary>
    private const char AiMark = '*';

    public override string Kind => KindName;

    /// <summary>
    /// The token's hash; then, after a space, <see cref="AiMark"/> if an AI plays the seat, and the claimant's id, if
    /// a player claimed the seat, or else <see cref="NicknameMark"/> and the guest's nickname, if they gave one.
    /// </summary>
    public override string Detail =>
        $"{this.TokenHash} {(this.IsAi ? $"{AiMark}" : string.Empty)}{this.Who}".TrimEnd();

    public override string Player => GameHistory.Person;

    public override bool IsGuest => this.Claimant is null;

    public override bool IsHeldBy([Required] string token) => SeatTokens.Hash(token) == this.TokenHash;

    /// <summary>The seat as <see cref="Detail"/> stores it.</summary>
    public static HumanSeat From([Required] string detail) => Of(detail.Split(' ', 2));

    private static HumanSeat Of(string[] parts) => Of(parts[0], parts.ElementAtOrDefault(1));

    private string Who =>
        (this.Claimant, this.Nickname) switch
        {
            ({ } claimant, _) => claimant.Value,
            (_, { } nickname) => $"{NicknameMark}{nickname}",
            _ => string.Empty,
        };

    private static HumanSeat Of(string hash, string? rest) =>
        rest switch
        {
            null or "" => new HumanSeat(hash),
            [AiMark, .. { } after] => Of(hash, after) with { IsAi = true },
            [NicknameMark, .. { } nickname] => new HumanSeat(hash, Nickname: nickname),
            _ => new HumanSeat(hash, new PlayerId(rest)),
        };
}
