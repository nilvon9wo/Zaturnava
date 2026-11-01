using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Net.NowhereAtAll.Zaturnava.Client;
using Net.NowhereAtAll.Zaturnava.Engine.Chance;
using Net.NowhereAtAll.Zaturnava.Server.Composition;
using Net.NowhereAtAll.Zaturnava.Server.Endpoints;

namespace Net.NowhereAtAll.Zaturnava.Server.Test;

/// <summary>
/// The server, hosted in the test process as configured for development (appsettings.json, then
/// appsettings.Development.json): games kept in memory, the page served from this machine. Its dice
/// are loaded, so that what follows a roll is known: Black wins every roll-off with
/// <see cref="OpeningValue"/>, and every later die comes up <see cref="DieValue"/>.
/// </summary>
public class ZaturnavaServer : WebApplicationFactory<Program>
{
    public const string Chaturaji2027 = "chaturaji-2027";
    public const string Random = "random";

    /// <summary>A pawn or the raja: Black's opening move.</summary>
    public const int OpeningValue = 6;

    /// <summary>The other seats' roll-off die.</summary>
    public const int LosingValue = 1;

    /// <summary>An elephant, so often hemmed in that the turn passes.</summary>
    public const int DieValue = 3;

    /// <summary>Seat 0 open for a person, the others played by the random bot.</summary>
    public static readonly ImmutableList<string?> OneOpenSeat = [null, Random, Random, Random];

    /// <summary>A client of this server, as any .NET client would reach it.</summary>
    public HttpGameClient GameClient() => new(this.CreateClient());

    /// <summary>
    /// The dice loaded; the hosted MCP server's tools calling this server in the test process; and no service to
    /// sign in with, whatever this machine's user secrets set up, so that a test sees only what it sets up itself.
    /// </summary>
    protected override void ConfigureWebHost(IWebHostBuilder builder) =>
        SignInServices.Known
            .Select(provider => provider.Name)
            .Concat(SignInServices.KnownByToken.Select(provider => provider.Name))
            .Aggregate(
                builder,
                (configured, provider) =>
                    configured.UseSetting(
                        $"{SignInServices.ProvidersKey}:{provider}:{SignInServices.ClientIdKey}",
                        string.Empty
                    )
            )
            .ConfigureTestServices(services =>
            {
                _ = services.AddSingleton<IDiceRolling>(new LoadedDice());
                _ = services
                    .AddHttpClient(McpEndpoints.SelfClient)
                    .ConfigurePrimaryHttpMessageHandler(() => this.Server.CreateHandler());
            });
}

/// <summary>
/// Dice that let the first of every four seats to roll off win, and otherwise always come up
/// <see cref="ZaturnavaServer.DieValue"/>. A roll-off is the only roll of a single die.
/// </summary>
file sealed class LoadedDice : IDiceRolling
{
    private const int Seats = 4;

    private int _rolledOff = -1;

    public ImmutableList<int> Roll([Required] DiceRequest request) =>
        request.Count == 1
            ? [this.RollOff()]
            : [.. Enumerable.Repeat(ZaturnavaServer.DieValue, request.Count)];

    private int RollOff() =>
        Interlocked.Increment(ref this._rolledOff) % Seats == 0
            ? ZaturnavaServer.OpeningValue
            : ZaturnavaServer.LosingValue;
}
