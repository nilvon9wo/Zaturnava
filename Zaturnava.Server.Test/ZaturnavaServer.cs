using System.Collections.Immutable;
using Microsoft.AspNetCore.Mvc.Testing;
using Net.NowhereAtAll.Zaturnava.Client;

namespace Net.NowhereAtAll.Zaturnava.Server.Test;

/// <summary>
/// The server, hosted in the test process as configured for development (appsettings.json, then
/// appsettings.Development.json): games kept in memory, the page served from this machine.
/// </summary>
public class ZaturnavaServer : WebApplicationFactory<Program>
{
    public const string Classic = "classic";
    public const string Random = "random";

    /// <summary>Seat 0 open for a person, the others played by the random bot.</summary>
    public static readonly ImmutableList<string?> OneOpenSeat = [null, Random, Random, Random];

    /// <summary>A client of this server, as any .NET client would reach it.</summary>
    public HttpGameClient GameClient() => new(this.CreateClient());
}
