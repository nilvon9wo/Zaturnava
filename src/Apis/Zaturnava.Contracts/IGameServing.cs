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

    /// <summary>
    /// What was said at the game's table after message number <paramref name="after"/> (0 for everything) that
    /// the holder of <paramref name="token"/> may read: what was said to the table, and to their own team.
    /// Without a token, what was said to the table.
    /// </summary>
    Task<ImmutableList<ChatMessageDto>> Chat(
        [Required] string gameId,
        string? token,
        int after,
        CancellationToken cancellationToken
    );

    /// <summary>The names of the services a person may sign in with; none, if none is set up.</summary>
    Task<ImmutableList<string>> SignInProviders(CancellationToken cancellationToken);

    /// <summary>The player a player's <paramref name="token"/> stands for; none, if it stands for no one.</summary>
    Task<PlayerDto?> Me([Required] string token, CancellationToken cancellationToken);

    /// <summary>Says something at the game's table, as the seat <paramref name="token"/> holds.</summary>
    Task<ChatMessageDto> Say(
        [Required] string gameId,
        [Required] string token,
        [Required] SayRequest request,
        CancellationToken cancellationToken
    );
}
