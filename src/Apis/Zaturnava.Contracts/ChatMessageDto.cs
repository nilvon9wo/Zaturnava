namespace Net.NowhereAtAll.Zaturnava.Contracts;

/// <summary>
/// One thing said at a game's table: its place among everything said there (from 1), the seat that said it, to
/// whom (<see cref="ChatChannels"/>), the words, and when.
/// </summary>
public sealed record ChatMessageDto(int Number, int Seat, string Channel, string Text, DateTimeOffset At);

/// <summary>
/// Whom a message is said to: <see cref="Table"/>, everyone there; <see cref="Team"/>, the sender's team.
/// </summary>
public static class ChatChannels
{
    public const string Table = "table";
    public const string Team = "team";
}

/// <summary>
/// Say <see cref="Text"/> to <see cref="Channel"/> (<see cref="ChatChannels"/>), as the seat the token holds.
/// </summary>
public sealed record SayRequest(string Channel, string Text);
