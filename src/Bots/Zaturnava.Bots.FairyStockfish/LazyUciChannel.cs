using System.ComponentModel.DataAnnotations;

namespace Net.NowhereAtAll.Zaturnava.Bots.FairyStockfish;

/// <summary>A channel opened only once it is first used, so that a bot never asked never starts an engine.</summary>
public sealed class LazyUciChannel(Func<IUciChannel> open) : IUciChannel
{
    private readonly Lazy<IUciChannel> _channel = new(open);

    public Task Send([Required] string line, CancellationToken cancellationToken) =>
        this._channel.Value.Send(line, cancellationToken);

    public Task<string?> Receive(CancellationToken cancellationToken) => this._channel.Value.Receive(cancellationToken);

    /// <summary>Closes the channel, if it was ever opened.</summary>
    public void Dispose()
    {
        if (this._channel.IsValueCreated)
        {
            this._channel.Value.Dispose();
        }
    }
}
