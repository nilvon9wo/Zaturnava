namespace Net.NowhereAtAll.Zaturnava.Contracts;

/// <summary>
/// Something that happened, for people: the <see cref="Number"/>th thing in the game (from 0), of one of
/// the kinds below, done by <see cref="Seat"/> (for a turn passing, the seat it passes to), told as
/// <see cref="Text"/>. A move or a capture also names the cells it went <see cref="From"/> and
/// <see cref="To"/>, so a client can mark them; a promotion, a piece going home or one escaping, its cell
/// as <see cref="To"/>. A piece escaping or captured also names its <see cref="Owner"/>, the seat whose army
/// it is; a capture, whether the piece <see cref="IsLeader"/>.
/// </summary>
public sealed record EventDto(
    int Number,
    string Kind,
    int Seat,
    string Text,
    int? From,
    int? To,
    int? Owner = null,
    bool IsLeader = false
)
{
    public const string RolledOff = "rolled-off";
    public const string Rolled = "rolled";
    public const string Forfeited = "forfeited";
    public const string Moved = "moved";
    public const string Captured = "captured";
    public const string Bonus = "bonus";
    public const string Shared = "shared";
    public const string Promoted = "promoted";
    public const string Escaped = "escaped";
    public const string Ransom = "ransom";
    public const string Ending = "ending";
    public const string Command = "command";
    public const string Empire = "empire";
    public const string Away = "away";
    public const string Passed = "passed";
    public const string Ended = "ended";
}
