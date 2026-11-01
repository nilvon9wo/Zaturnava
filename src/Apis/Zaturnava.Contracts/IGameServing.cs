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

    /// <summary>
    /// Claims an open seat; the token returned is the only way to act for it. With a signed-in player's token
    /// (<paramref name="playerToken"/>), the seat is theirs, and shows their name; without one, a guest's, shown by
    /// the <paramref name="claim"/>'s nickname, if one is given, as "Nickname (guest)". A claim that says an AI plays
    /// the seat has it shown so.
    /// </summary>
    Task<SeatClaimedDto> Claim(
        [Required] string gameId,
        int seat,
        string? playerToken,
        ClaimSeatRequest? claim,
        CancellationToken cancellationToken
    );

    /// <summary>
    /// Says anew who holds a seat already taken (<see cref="IdentifySeatRequest"/>): the player whose token
    /// <paramref name="playerToken"/> is, if one is given; else a guest, by the request's nickname, if it gives one.
    /// </summary>
    Task Identify(
        [Required] string gameId,
        int seat,
        string? playerToken,
        [Required] IdentifySeatRequest request,
        CancellationToken cancellationToken
    );

    /// <summary>
    /// Has bots take the open seats of a game waiting for people, for the player whose seat <paramref name="token"/>
    /// holds, once they may (<see cref="VacancyDto"/>); the game begins, if no seat is left open.
    /// </summary>
    Task<GameViewDto> FillOpenSeats(
        [Required] string gameId,
        [Required] string token,
        CancellationToken cancellationToken
    );

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

    /// <summary>
    /// Names the player <paramref name="token"/> stands for anew (<see cref="RenameRequest"/>); none, if it stands for
    /// no one.
    /// </summary>
    Task<PlayerDto?> Rename([Required] string token, [Required] string name, CancellationToken cancellationToken);

    /// <summary>
    /// A one-time code for the player <paramref name="token"/> stands for to link another service with
    /// (<see cref="LinkCodeDto"/>); none, if it stands for no one.
    /// </summary>
    Task<string?> LinkCode([Required] string token, CancellationToken cancellationToken);

    /// <summary>
    /// Lets the player <paramref name="token"/> stands for sign in with <paramref name="provider"/> no more; none, if
    /// it stands for no one. Refused, if it is the only service they may sign in with.
    /// </summary>
    Task<PlayerDto?> Unlink([Required] string token, [Required] string provider, CancellationToken cancellationToken);

    /// <summary>
    /// Shows <paramref name="links"/> on the profile of the player <paramref name="token"/> stands for, in place of
    /// those before; none, if it stands for no one. Refused, if one is not a web address.
    /// </summary>
    Task<PlayerDto?> SetLinks(
        [Required] string token,
        [Required] ImmutableList<string> links,
        CancellationToken cancellationToken
    );

    /// <summary>
    /// A new token for an AI to play for the player <paramref name="token"/> stands for (<see cref="AiTokenDto"/>);
    /// none, if it stands for no one.
    /// </summary>
    Task<string?> AiToken([Required] string token, CancellationToken cancellationToken);

    /// <summary>The player's profile, as anyone may see it; none, if no such player is known.</summary>
    Task<ProfileDto?> Profile([Required] string playerId, CancellationToken cancellationToken);

    /// <summary>Signs out: the player's <paramref name="token"/> stands for no one from now on.</summary>
    Task SignOut([Required] string token, CancellationToken cancellationToken);

    /// <summary>
    /// Deletes the account the player's <paramref name="token"/> stands for: their name, sign-ins and sessions.
    /// Whether there was one to delete.
    /// </summary>
    Task<bool> DeleteMe([Required] string token, CancellationToken cancellationToken);

    /// <summary>Says something at the game's table, as the seat <paramref name="token"/> holds.</summary>
    Task<ChatMessageDto> Say(
        [Required] string gameId,
        [Required] string token,
        [Required] SayRequest request,
        CancellationToken cancellationToken
    );
}
