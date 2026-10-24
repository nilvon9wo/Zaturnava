using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Net.NowhereAtAll.Zaturnava.Contracts;

namespace Net.NowhereAtAll.Zaturnava.Web.Test;

/// <summary>A bUnit context with the page's services, and <see cref="Server"/> as the game server.</summary>
internal sealed class PageContext : BunitContext
{
    /// <param name="pollInterval">How often tables poll, and bots play: never, unless a test says so.</param>
    public PageContext(TimeSpan? pollInterval = null)
    {
        this.JSInterop.Mode = JSRuntimeMode.Loose;
        _ = this.Services.AddSingleton<IGameServing>(this.Server);
        _ = this.Services.AddSingleton<Browser>();
        TimeSpan interval = pollInterval ?? Timeout.InfiniteTimeSpan;
        _ = this.Services.AddSingleton(new TableSettings(interval, interval));
    }

    public FakeGameServer Server { get; } = new();
}
