using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Net.NowhereAtAll.Zaturnava.Contracts;

namespace Net.NowhereAtAll.Zaturnava.Web.Test;

/// <summary>A bUnit context with the page's services, and <see cref="Server"/> as the game server.</summary>
internal sealed class PageContext : BunitContext
{
    /// <param name="pollInterval">How often tables poll: never, unless a test says so.</param>
    public PageContext(TimeSpan? pollInterval = null)
    {
        _ = this.Services.AddSingleton<IGameServing>(this.Server);
        _ = this.Services.AddSingleton(new TableSettings(pollInterval ?? Timeout.InfiniteTimeSpan));
    }

    public FakeGameServer Server { get; } = new();
}
