using System.ComponentModel.DataAnnotations;
using System.Globalization;
using Net.NowhereAtAll.Zaturnava.Application.Players;

namespace Net.NowhereAtAll.Zaturnava.Application.Games;

/// <summary>
/// A seat claimed by whoever holds its token - through any client, or an AI over MCP. Only the
/// token's hash is kept; and, if whoever claimed it was signed in, the player they are, the <see cref="Claimant"/>;
/// if not, the <see cref="Nickname"/> they gave, if any. <see cref="IsAi"/> says that an AI, or a program, plays it,
/// as whoever claimed it said: for a signed-in player, on their behalf, and by the <see cref="Nickname"/> it gave
/// itself, if any, so that one player's AIs may be told apart. <see cref="Arrival"/> is the game's version
/// when the seat was claimed, saying who came first; 0 for a seat claimed before arrivals were kept.
/// <see cref="SeatController.IsForPlayers"/> says the seat was one only a player signed in might claim.
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

    /// <summary>What marks, in <see cref="Detail"/>, a seat that was only for players signed in.</summary>
    private const char ForPlayersMark = '+';

    /// <summary>What marks, in <see cref="Detail"/>, the <see cref="Arrival"/> after the token's hash.</summary>
    private const char ArrivalMark = '@';

    public override string Kind => KindName;

    /// <summary>
    /// The token's hash, and <see cref="ArrivalMark"/> and the arrival, if kept; then, after a space,
    /// <see cref="ForPlayersMark"/> if the seat was only for players, <see cref="AiMark"/> if an AI plays it, and
    /// the claimant's id, if a player claimed the seat; then <see cref="NicknameMark"/> and the nickname, if a guest,
    /// or an AI, gave one.
    /// </summary>
    public override string Detail =>
        $"{this.TokenHash}{this.Arrived} {this.Marks}{this.Who}".TrimEnd();

    public override string Player => GameHistory.Person;

    public override bool IsGuest => this.Claimant is null;

    public override bool IsHeldBy([Required] string token) => SeatTokens.Hash(token) == this.TokenHash;

    /// <summary>
    /// The seat, claimed by <paramref name="into"/> instead if <paramref name="from"/> claimed it: as one player is
    /// merged into another.
    /// </summary>
    public static SeatController Reclaimed([Required] SeatController seat, PlayerId from, PlayerId into) =>
        seat is HumanSeat human && human.Claimant == from ? human with { Claimant = into } : seat;

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

    private string Marks =>
        (this.IsForPlayers ? $"{ForPlayersMark}" : string.Empty) + (this.IsAi ? $"{AiMark}" : string.Empty);

    private string Who =>
        $"{this.Claimant?.Value}{(this.Nickname is { } nickname ? $"{NicknameMark}{nickname}" : string.Empty)}";

    private static HumanSeat Of(string hash, string? rest) =>
        rest switch
        {
            null or "" => new HumanSeat(hash),
            [ForPlayersMark, .. { } after] => Of(hash, after) with { IsForPlayers = true },
            [AiMark, .. { } after] => Of(hash, after) with { IsAi = true },
            [NicknameMark, .. { } nickname] => new HumanSeat(hash, Nickname: nickname),
            _ => Claimed(hash, rest.Split(NicknameMark, 2)),
        };

    private static HumanSeat Claimed(string hash, string[] who) =>
        new(hash, new PlayerId(who[0]), who.ElementAtOrDefault(1));
}
