using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using Net.NowhereAtAll.Zaturnava.Application.Games;
using Net.NowhereAtAll.Zaturnava.Application.Ports;
using Net.NowhereAtAll.Zaturnava.Application.Running;
using Net.NowhereAtAll.Zaturnava.Engine.Rules;
using Net.NowhereAtAll.Zaturnava.Engine.Seats;

namespace Net.NowhereAtAll.Zaturnava.Application.Chat;

/// <summary>
/// Talk at a game's table. A player, by the token that holds their seat, speaks to the whole table or to their
/// team alone. Anyone reads what is said to the table, watchers too; what is said to a team, only its players.
/// Bots say nothing.
/// </summary>
public sealed class ChatService(GameService games, IChatPersisting store, DecisionClock clock)
{
    /// <summary>The most a message may say, in characters.</summary>
    public const int MaximumLength = 500;

    private readonly GameService _games = games;
    private readonly IChatPersisting _store = store;
    private readonly DecisionClock _clock = clock;

    /// <summary>The message, kept and numbered; its words without the spaces around them.</summary>
    /// <exception cref="GameNotFoundException">There is no such game.</exception>
    /// <exception cref="NotSeatedException">The token holds no seat of the game.</exception>
    /// <exception cref="ArgumentException">The message says nothing.</exception>
    /// <exception cref="InvalidChatException">The message says too much.</exception>
    public async Task<ChatMessage> Say(
        [Required] GameId id,
        [Required] string token,
        ChatChannel channel,
        [Required] string text,
        CancellationToken cancellationToken
    )
    {
        GameSnapshot snapshot = await this._games.Get(id, cancellationToken);
        SeatId seat = SeatOf(snapshot.Record, token) ?? throw new NotSeatedException(id);
        string said = text.Trim();
        return said.Length > MaximumLength
            ? throw new InvalidChatException()
            : await this._store.Add(new ChatMessage(id, 0, seat, channel, said, this._clock.Now), cancellationToken);
    }

    /// <summary>
    /// What was said at the table after message number <paramref name="after"/> that the holder of
    /// <paramref name="token"/> may read: all that was said to the table, and what was said to their own team.
    /// Without a token, or with one that holds no seat, only what was said to the table.
    /// </summary>
    /// <exception cref="GameNotFoundException">There is no such game.</exception>
    public async Task<ImmutableList<ChatMessage>> Read(
        [Required] GameId id,
        string? token,
        int after,
        CancellationToken cancellationToken
    )
    {
        GameSnapshot snapshot = await this._games.Get(id, cancellationToken);
        SeatId? reader = token is null ? null : SeatOf(snapshot.Record, token);
        ImmutableList<ChatMessage> said = await this._store.Of(id, cancellationToken);
        return [.. said.Where(message => message.Number > after && MayRead(snapshot.State, reader, message))];
    }

    private static bool MayRead(GameState state, SeatId? reader, ChatMessage message) =>
        (message.Channel, reader) switch
        {
            (ChatChannel.Table, _) => true,
            (_, { } seat) => state.Teams.AreAllied(seat, message.From),
            _ => false,
        };

    private static SeatId? SeatOf(GameRecord record, string token) =>
        record.Controllers
            .Select((controller, seat) => (Controller: controller, Seat: new SeatId(seat)))
            .Where(held => held.Controller.IsHeldBy(token))
            .Select(held => (SeatId?)held.Seat)
            .FirstOrDefault();
}
