using Net.NowhereAtAll.Zaturnava.Application.Games;
using Net.NowhereAtAll.Zaturnava.Engine.Seats;

namespace Net.NowhereAtAll.Zaturnava.Application.Chat;

/// <summary>
/// One thing said at a table (known by its first game, <see cref="ChatTables"/>): its place among everything said
/// there (from 1), the seat that said it, to whom, the words, and when.
/// </summary>
public sealed record ChatMessage(
    GameId Table,
    int Number,
    SeatId From,
    ChatChannel Channel,
    string Text,
    DateTimeOffset At
);
