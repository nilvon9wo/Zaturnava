using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;

namespace Net.NowhereAtAll.Zaturnava.Contracts;

/// <summary>
/// Everything a client can ask of a game server, whatever carries the request. Every method may
/// throw <see cref="GameRequestRefusedException"/>.
/// </summary>
public interface IGameServing
{
    Task<CatalogDto> Catalog(CancellationToken cancellationToken);

    Task<ImmutableList<GameSummaryDto>> List(CancellationToken cancellationToken);

    Task<GameCreatedDto> Create([Required] CreateGameRequest request, CancellationToken cancellationToken);

    Task<GameViewDto> Get([Required] string gameId, CancellationToken cancellationToken);

    /// <summary>The game as data to learn from: every action so far, and how it ended, if it has.</summary>
    Task<GameHistoryDto> History([Required] string gameId, CancellationToken cancellationToken);

    /// <summary>The history of every finished game the server keeps.</summary>
    Task<ImmutableList<GameHistoryDto>> Histories(CancellationToken cancellationToken);

    /// <summary>
    /// Lets the bot whose turn it is make one decision, if the game is still at <paramref name="version"/>;
    /// otherwise, or when no bot is deciding, changes nothing. Either way, returns the game as it is then.
    /// </summary>
    Task<GameViewDto> Advance([Required] string gameId, int version, CancellationToken cancellationToken);

    /// <summary>Claims an open seat; the token returned is the only way to act for it.</summary>
    Task<SeatClaimedDto> Claim([Required] string gameId, int seat, CancellationToken cancellationToken);

    /// <summary>Takes <paramref name="action"/> for the seat <paramref name="token"/> holds.</summary>
    Task<GameViewDto> Submit(
        [Required] string gameId,
        [Required] string token,
        [Required] ActionDto action,
        CancellationToken cancellationToken
    );
}
