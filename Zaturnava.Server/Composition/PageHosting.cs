using Microsoft.AspNetCore.Cors.Infrastructure;
using Net.NowhereAtAll.Zaturnava.Hosting;
using Net.NowhereAtAll.Zaturnava.Hosting.Itch;

namespace Net.NowhereAtAll.Zaturnava.Server.Composition;

/// <summary>
/// Where the web client's page is served from, chosen by configuration: <c>Listed</c> (the
/// default), whose origins are named under <c>PageHosting:Origins</c>, or <c>Itch</c>. A browser
/// may call the server from the chosen host's origins only.
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
        IPageHosting host = Host(configuration, configuration[ProviderKey] ?? Listed);
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

    private static IEnumerable<string> Origins(IConfiguration configuration) =>
        configuration.GetSection(OriginsKey).GetChildren().Select(origin => origin.Value).OfType<string>();

    private static void Allow(CorsPolicyBuilder policy, IPageHosting host) =>
        _ = policy
            .WithOrigins([.. host.Origins])
            .AllowAnyHeader()
            .AllowAnyMethod();
}
