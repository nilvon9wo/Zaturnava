using System.ComponentModel.DataAnnotations;

namespace Net.NowhereAtAll.Zaturnava.Bots.FairyStockfish;

/// <summary>Lines to and from a UCI engine, one at a time.</summary>
public interface IUciChannel : IDisposable
{
    Task Send([Required] string line, CancellationToken cancellationToken);

    /// <summary>The next line the engine says; none, once it says no more.</summary>
    Task<string?> Receive(CancellationToken cancellationToken);
}
