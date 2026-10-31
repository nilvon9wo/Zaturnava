using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using Net.NowhereAtAll.Zaturnava.Application.Chat;
using Net.NowhereAtAll.Zaturnava.Application.Games;

namespace Net.NowhereAtAll.Zaturnava.Application.Ports;

/// <summary>
/// What is said at each game's table, kept apart from the game: talk changes nothing in play, so saying something
/// never makes a move in the game go stale.
/// </summary>
public interface IChatPersisting
{
    /// <summary>Keeps <paramref name="message"/>, numbered after the last said at its game's table.</summary>
    /// <returns>The message as kept, with its number.</returns>
    Task<ChatMessage> Add([Required] ChatMessage message, CancellationToken cancellationToken);

    /// <summary>Everything said at the game's table, in order.</summary>
    Task<ImmutableList<ChatMessage>> Of([Required] GameId game, CancellationToken cancellationToken);
}
