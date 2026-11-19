using Microsoft.AspNetCore.Cors.Infrastructure;
using Net.NowhereAtAll.Zaturnava.Hosting;
using Net.NowhereAtAll.Zaturnava.Hosting.Itch;

namespace Net.NowhereAtAll.Zaturnava.Server.Composition;

/// <summary>
/// Where the web client's page is served from, chosen by configuration: <c>Listed</c> (the
/// default), whose origins are named under <c>PageHosting:Origins</c>, or <c>Itch</c>, beside any
/// origins listed there too, as the game server's own, where it serves the page as well (Stated
/// 2026-11-19, OQ9). A browser may call the server, or sign in, from those origins only.
/// </summary>
internal static class PageHosting
{
    public const string ProviderKey = "PageHosting:Provider";

    /// <summary>
    /// The configuration section of a listed host's origins, each under a name of its own, so that
    /// another settings file adds an origin rather than replacing one by index.
    /// </summary>
    public const string OriginsKey = "PageHosting:Origins";

    public const string Listed = "Listed";
    public const string Itch = "Itch";

    /// <exception cref="InvalidOperationException">The provider is unknown.</exception>
    public static IServiceCollection AddPageHosting(this IServiceCollection services, IConfiguration configuration)
    {
        IPageHosting host = AlsoListed(Host(configuration, configuration[ProviderKey] ?? Listed), configuration);
        _ = services.AddOptions<CorsOptions>().Configure(cors => cors.AddDefaultPolicy(policy => Allow(policy, host)));
        return services.AddSingleton(host).AddCors();
    }

    private static IPageHosting Host(IConfiguration configuration, string provider) =>
        provider switch
        {
            Listed => new ListedPageHost([.. Origins(configuration)]),
            Itch => new ItchPageHost(),
            _ => throw new InvalidOperationException(
                $"There is no '{provider}' page host; {ProviderKey} must be {Listed} or {Itch}."
            ),
        };

    /// <summary>
    /// <paramref name="host"/>, with the origins listed beside its own, if any are and it does not list them already.
    /// </summary>
    private static IPageHosting AlsoListed(IPageHosting host, IConfiguration configuration) =>
        host is ListedPageHost || !Origins(configuration).Any()
            ? host
            : new ListedPageHost([.. host.Origins, .. Origins(configuration)]);

    private static IEnumerable<string> Origins(IConfiguration configuration) =>
        configuration.GetSection(OriginsKey).GetChildren().Select(origin => origin.Value).OfType<string>();

    private static void Allow(CorsPolicyBuilder policy, IPageHosting host) =>
        _ = policy
            .WithOrigins([.. host.Origins])
            .AllowAnyHeader()
            .AllowAnyMethod();
}
