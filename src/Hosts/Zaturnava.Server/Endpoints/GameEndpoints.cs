using System.Collections.Immutable;
using Microsoft.AspNetCore.Http.HttpResults;
using Net.NowhereAtAll.Zaturnava.Application.Games;
using Net.NowhereAtAll.Zaturnava.Contracts;
using Net.NowhereAtAll.Zaturnava.Engine.Actions;
using Net.NowhereAtAll.Zaturnava.Engine.Seats;

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
        _ = routes.MapPost(GameRoutes.Games, Create);
        _ = routes.MapGet(GameRoute, Get);
        _ = routes.MapPost(GameRoute + "/seats/{seat:int}", Claim);
        _ = routes.MapPost(GameRoute + "/actions", Submit);
        _ = routes.MapPost(GameRoute + "/advance", Advance);
        return routes;
    }

    private static CatalogDto Catalog(GameService games) => GameViews.CatalogOf(games.Rulesets, games.Bots);

    private static async Task<ImmutableList<GameSummaryDto>> List(
        GameService games,
        CancellationToken cancellationToken
    )
    {
        ImmutableList<GameSnapshot> snapshots = await games.Snapshots(cancellationToken);
        return [.. snapshots.Select(GameViews.SummaryOf)];
    }

    private static async Task<Created<GameCreatedDto>> Create(
        CreateGameRequest request,
        GameService games,
        CancellationToken cancellationToken
    )
    {
        ImmutableList<SeatPlan> seats = [.. request.Bots.Select(bot => new SeatPlan(bot))];
        GameId id = await games.Create(request.RulesetId, seats, cancellationToken);
        return TypedResults.Created("/" + GameRoutes.Game(id.Value), new GameCreatedDto(id.Value));
    }

    private static async Task<GameViewDto> Get(string gameId, GameService games, CancellationToken cancellationToken)
    {
        GameSnapshot snapshot = await games.Get(new GameId(gameId), cancellationToken);
        return GameViews.ViewOf(snapshot, games.Bots);
    }

    private static async Task<GameViewDto> Advance(
        string gameId,
        AdvanceRequest request,
        GameService games,
        CancellationToken cancellationToken
    )
    {
        GameSnapshot snapshot = await games.Advance(new GameId(gameId), request.Version, cancellationToken);
        return GameViews.ViewOf(snapshot, games.Bots);
    }

    private static async Task<SeatClaimedDto> Claim(
        string gameId,
        int seat,
        GameService games,
        CancellationToken cancellationToken
    )
    {
        string token = await games.Claim(new GameId(gameId), new SeatId(seat), cancellationToken);
        return new SeatClaimedDto(token);
    }

    private static async Task<GameViewDto> Submit(
        string gameId,
        ActionDto action,
        HttpRequest request,
        GameService games,
        CancellationToken cancellationToken
    )
    {
        GameId id = new(gameId);
        ActionDescriptor descriptor = new(action.Kind, action.Arguments);
        string token = SeatToken.From(request, id);
        GameSnapshot snapshot = await games.Submit(id, token, descriptor, cancellationToken);
        return GameViews.ViewOf(snapshot, games.Bots);
    }
}
