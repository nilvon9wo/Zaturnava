using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Net.NowhereAtAll.Zaturnava.Client;
using Net.NowhereAtAll.Zaturnava.Engine.Chance;

namespace Net.NowhereAtAll.Zaturnava.Server.Test;

/// <summary>
/// The server, hosted in the test process as configured for development (appsettings.json, then
/// appsettings.Development.json): games kept in memory, the page served from this machine. Its dice
/// always come up <see cref="DieValue"/>, so that what follows a roll is known.
/// </summary>
public class ZaturnavaServer : WebApplicationFactory<Program>
{
    public const string Classic = "classic";
    public const string Random = "random";

    /// <summary>An elephant: on the opening turn, hemmed in by its own pawn, so the turn passes.</summary>
    public const int DieValue = 3;

    /// <summary>Seat 0 open for a person, the others played by the random bot.</summary>
    public static readonly ImmutableList<string?> OneOpenSeat = [null, Random, Random, Random];

    /// <summary>A client of this server, as any .NET client would reach it.</summary>
    public HttpGameClient GameClient() => new(this.CreateClient());

    protected override void ConfigureWebHost(IWebHostBuilder builder) =>
        builder.ConfigureTestServices(services => services.AddSingleton<IDiceRolling>(new LoadedDice()));
}

/// <summary>Dice that always come up <see cref="ZaturnavaServer.DieValue"/>.</summary>
file sealed class LoadedDice : IDiceRolling
{
    public ImmutableList<int> Roll([Required] DiceRequest request) =>
        [.. Enumerable.Repeat(ZaturnavaServer.DieValue, request.Count)];
}
