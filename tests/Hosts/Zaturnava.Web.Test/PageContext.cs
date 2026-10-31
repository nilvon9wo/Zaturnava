using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Net.NowhereAtAll.Zaturnava.Contracts;

namespace Net.NowhereAtAll.Zaturnava.Web.Test;

/// <summary>A bUnit context with the page's services, and <see cref="Server"/> as the game server.</summary>
internal sealed class PageContext : BunitContext
{
    /// <summary>Where the game server is: elsewhere than the page, as on itch.io.</summary>
    public static readonly Uri ServerUri = new("https://games.example/");

    /// <param name="pollInterval">How often tables poll, and bots play: never, unless a test says so.</param>
    public PageContext(TimeSpan? pollInterval = null)
    {
        this.JSInterop.Mode = JSRuntimeMode.Loose;
        _ = this.Services.AddSingleton<IGameServing>(this.Server);
        _ = this.Services.AddSingleton<Browser>();
        _ = this.Services.AddSingleton(new ServerAddress(ServerUri));
        TimeSpan interval = pollInterval ?? Timeout.InfiniteTimeSpan;
        _ = this.Services.AddSingleton(new TableSettings(interval, interval, interval));
        _ = this.Services.AddSingleton(TimeProvider.System);
    }

    public FakeGameServer Server { get; } = new();
}
