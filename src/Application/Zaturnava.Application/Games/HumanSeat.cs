using System.ComponentModel.DataAnnotations;
using System.Globalization;
using Net.NowhereAtAll.Zaturnava.Application.Players;

namespace Net.NowhereAtAll.Zaturnava.Application.Games;

/// <summary>
/// A seat claimed by whoever holds its token - through any client, or an AI over MCP. Only the
/// token's hash is kept; and, if whoever claimed it was signed in, the player they are, the <see cref="Claimant"/>;
/// if not, the <see cref="Nickname"/> they gave, if any. <see cref="IsAi"/> says that an AI, or a program, plays it,
/// as whoever claimed it said: for a signed-in player, on their behalf. <see cref="Arrival"/> is the game's version
/// when the seat was claimed, saying who came first; 0 for a seat claimed before arrivals were kept.
/// </summary>
public sealed record HumanSeat(
    string TokenHash,
    PlayerId? Claimant = null,
    string? Nickname = null,
    bool IsAi = false,
    int Arrival = 0
) : SeatController
{
    public const string KindName = "human";

    /// <summary>What marks, in <see cref="Detail"/>, a guest's nickname rather than a player's id.</summary>
    private const char NicknameMark = '~';

    /// <summary>What marks, in <see cref="Detail"/>, a seat an AI plays.</summary>
    private const char AiMark = '*';

    /// <summary>What marks, in <see cref="Detail"/>, the <see cref="Arrival"/> after the token's hash.</summary>
    private const char ArrivalMark = '@';

    public override string Kind => KindName;

    /// <summary>
    /// The token's hash, and <see cref="ArrivalMark"/> and the arrival, if kept; then, after a space,
    /// <see cref="AiMark"/> if an AI plays the seat, and the claimant's id, if a player claimed the seat, or else
    /// <see cref="NicknameMark"/> and the guest's nickname, if they gave one.
    /// </summary>
    public override string Detail =>
        $"{this.TokenHash}{this.Arrived} {(this.IsAi ? $"{AiMark}" : string.Empty)}{this.Who}".TrimEnd();

    public override string Player => GameHistory.Person;

    public override bool IsGuest => this.Claimant is null;

    public override bool IsHeldBy([Required] string token) => SeatTokens.Hash(token) == this.TokenHash;

    /// <summary>The seat as <see cref="Detail"/> stores it.</summary>
    public static HumanSeat From([Required] string detail) => Of(detail.Split(' ', 2));

    private static HumanSeat Of(string[] parts) =>
        Arriving(parts[0].Split(ArrivalMark, 2), parts.ElementAtOrDefault(1));

    private static HumanSeat Arriving(string[] held, string? rest) =>
        Of(held[0], rest) with
        {
            Arrival = held.Length == 2 ? int.Parse(held[1], NumberStyles.None, CultureInfo.InvariantCulture) : 0,
        };

    private string Arrived =>
        this.Arrival == 0 ? string.Empty : $"{ArrivalMark}{this.Arrival.ToString(CultureInfo.InvariantCulture)}";

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
