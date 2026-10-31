using System.Collections.Immutable;
using Microsoft.AspNetCore.Http.HttpResults;
using Net.NowhereAtAll.Zaturnava.Application.Chat;
using Net.NowhereAtAll.Zaturnava.Application.Games;
using Net.NowhereAtAll.Zaturnava.Application.Players;
using Net.NowhereAtAll.Zaturnava.Contracts;
using Net.NowhereAtAll.Zaturnava.Engine.Actions;
using Net.NowhereAtAll.Zaturnava.Engine.Seats;
using Net.NowhereAtAll.Zaturnava.Server.Composition;

namespace Net.NowhereAtAll.Zaturnava.Server.Endpoints;

/// <summary>
/// The routes of <see cref="GameRoutes"/>, each one call of the <see cref="GameService"/> between
/// translations to and from the contracts. A refusal is thrown, and answered by
/// <see cref="RefusalHandler"/>.
/// </summary>
internal static class GameEndpoints
{
    private const string GameRoute = GameRoutes.Games + "/{gameId}";

    public static IEndpointRouteBuilder MapGames(this IEndpointRouteBuilder routes)
    {
        _ = routes.MapGet(GameRoutes.Catalog, Catalog);
        _ = routes.MapGet(GameRoutes.Games, List);
        _ = routes.MapPost(GameRoutes.Games, Create).RequireRateLimiting(RequestLimits.NewGames);
        _ = routes.MapGet(GameRoute, Get);
        _ = routes.MapGet(GameRoute + "/history", History);
        _ = routes.MapGet(GameRoutes.Histories, Histories);
        _ = routes.MapPost(GameRoute + "/seats/{seat:int}", Claim);
        _ = routes.MapPost(GameRoute + "/actions", Submit);
        _ = routes.MapPost(GameRoute + "/advance", Advance);
        _ = routes.MapGet(GameRoute + "/chat", Chat);
        _ = routes.MapPost(GameRoute + "/chat", Say);
        return routes;
    }

    private static CatalogDto Catalog(GameService games) =>
        GameViews.CatalogOf(games.Rulesets, games.Bots, games.Scenarios);

    private static async Task<ImmutableList<GameSummaryDto>> List(
        GameService games,
        PlayerService players,
        CancellationToken cancellationToken
    )
    {
        ImmutableList<GameSnapshot> snapshots = await games.Snapshots(cancellationToken);
        ImmutableDictionary<HumanSeat, string> names = await players.NamesOf(
            snapshots.SelectMany(snapshot => snapshot.Record.Controllers),
            cancellationToken
        );
        return [.. snapshots.Select(snapshot => GameViews.SummaryOf(snapshot, names))];
    }

    /// <summary>The game as its seats see it, naming the bots and whoever holds each person's seat.</summary>
    private static async Task<GameViewDto> ViewOf(
        GameSnapshot snapshot,
        GameService games,
        PlayerService players,
        CancellationToken cancellationToken
    ) =>
        GameViews.ViewOf(
            snapshot,
            games.Bots,
            await players.NamesOf(snapshot.Record.Controllers, cancellationToken)
        );

    private static async Task<Created<GameCreatedDto>> Create(
        CreateGameRequest request,
        GameService games,
        ChatService chat,
        CancellationToken cancellationToken
    )
    {
        ImmutableList<SeatPlan> seats = [.. request.Bots.Select(bot => new SeatPlan(bot))];
        GameId id = await games.Create(request.RulesetId, seats, request.Scenario, cancellationToken);
        if (request.Continuing is { } previous)
        {
            chat.Continue(id, new GameId(previous));
        }

        return TypedResults.Created("/" + GameRoutes.Game(id.Value), new GameCreatedDto(id.Value));
    }

    private static async Task<GameViewDto> Get(
        string gameId,
        GameService games,
        PlayerService players,
        CancellationToken cancellationToken
    )
    {
        GameSnapshot snapshot = await games.Get(new GameId(gameId), cancellationToken);
        return await ViewOf(snapshot, games, players, cancellationToken);
    }

    private static async Task<GameHistoryDto> History(
        string gameId,
        GameService games,
        CancellationToken cancellationToken
    )
    {
        GameSnapshot snapshot = await games.Get(new GameId(gameId), cancellationToken);
        return GameViews.HistoryOf(GameHistory.Of(snapshot));
    }

    private static async Task<ImmutableList<GameHistoryDto>> Histories(
        GameService games,
        CancellationToken cancellationToken
    )
    {
        ImmutableList<GameSnapshot> snapshots = await games.Snapshots(cancellationToken);
        return
        [
            .. snapshots
                .Where(snapshot => snapshot.State.Outcome is not null)
                .Select(snapshot => GameViews.HistoryOf(GameHistory.Of(snapshot))),
        ];
    }

    private static async Task<GameViewDto> Advance(
        string gameId,
        AdvanceRequest request,
        GameService games,
        PlayerService players,
        CancellationToken cancellationToken
    )
    {
        GameSnapshot snapshot = await games.Advance(new GameId(gameId), request.Version, cancellationToken);
        return await ViewOf(snapshot, games, players, cancellationToken);
    }

    /// <summary>
    /// Claims the seat: for the player the request's token stands for, if any; else for a guest, by the nickname
    /// the body gives, if it gives one.
    /// </summary>
    private static async Task<SeatClaimedDto> Claim(
        string gameId,
        int seat,
        ClaimSeatRequest? claim,
        HttpRequest request,
        GameService games,
        PlayerService players,
        CancellationToken cancellationToken
    )
    {
        Player? player = SeatToken.OrNull(request) is { } playerToken
            ? await players.Find(playerToken, cancellationToken)
            : null;
        string token = await games.Claim(
            new GameId(gameId),
            new SeatId(seat),
            player?.Id,
            claim?.Nickname,
            cancellationToken
        );
        return new SeatClaimedDto(token);
    }

    private static async Task<GameViewDto> Submit(
        string gameId,
        ActionDto action,
        HttpRequest request,
        GameService games,
        PlayerService players,
        CancellationToken cancellationToken
    )
    {
        GameId id = new(gameId);
        ActionDescriptor descriptor = new(action.Kind, action.Arguments);
        string token = SeatToken.From(request, id);
        GameSnapshot snapshot = await games.Submit(id, token, descriptor, cancellationToken);
        return await ViewOf(snapshot, games, players, cancellationToken);
    }

    private static async Task<ImmutableList<ChatMessageDto>> Chat(
        string gameId,
        int? after,
        HttpRequest request,
        ChatService chat,
        CancellationToken cancellationToken
    )
    {
        ImmutableList<ChatMessage> said =
            await chat.Read(new GameId(gameId), SeatToken.OrNull(request), after ?? 0, cancellationToken);
        return [.. said.Select(ChatViews.MessageOf)];
    }

    private static async Task<ChatMessageDto> Say(
        string gameId,
        SayRequest say,
        HttpRequest request,
        ChatService chat,
        CancellationToken cancellationToken
    )
    {
        GameId id = new(gameId);
        string token = SeatToken.From(request, id);
        ChatMessage message =
            await chat.Say(id, token, ChatViews.ChannelOf(say.Channel), say.Text, cancellationToken);
        return ChatViews.MessageOf(message);
    }
}
