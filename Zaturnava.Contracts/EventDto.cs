namespace Net.NowhereAtAll.Zaturnava.Contracts;

/// <summary>
/// Something that happened, for people: the <see cref="Number"/>th thing in the game (from 0), of one of
/// the kinds below, done by <see cref="Seat"/>, told as <see cref="Text"/>. A move or a capture also
/// names the cells it went <see cref="From"/> and <see cref="To"/>, so a client can mark them.
/// </summary>
public sealed record EventDto(int Number, string Kind, int Seat, string Text, int? From, int? To)
{
    public const string RolledOff = "rolled-off";
    public const string Rolled = "rolled";
    public const string Moved = "moved";
    public const string Captured = "captured";
    public const string Bonus = "bonus";
    public const string Shared = "shared";
    public const string Passed = "passed";
    public const string Ended = "ended";
}
