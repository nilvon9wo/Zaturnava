using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using Net.NowhereAtAll.Zaturnava.Application.Chat;
using Net.NowhereAtAll.Zaturnava.Application.Games;

namespace Net.NowhereAtAll.Zaturnava.Application.Ports;

/// <summary>
/// What is said at each table (<see cref="ChatTables"/>), kept apart from the games: talk changes nothing in play,
/// so saying something never makes a move in a game go stale.
/// </summary>
public interface IChatPersisting
{
    /// <summary>Keeps <paramref name="message"/>, numbered after the last said at its table.</summary>
    /// <returns>The message as kept, with its number.</returns>
    Task<ChatMessage> Add([Required] ChatMessage message, CancellationToken cancellationToken);

    /// <summary>Everything said at the table, in order.</summary>
    Task<ImmutableList<ChatMessage>> Of([Required] GameId table, CancellationToken cancellationToken);

    /// <summary>The tables anything is kept for.</summary>
    Task<ImmutableList<GameId>> Tables(CancellationToken cancellationToken);

    /// <summary>Lets go of everything said at the table.</summary>
    Task Remove([Required] GameId table, CancellationToken cancellationToken);
}
