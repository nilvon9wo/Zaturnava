using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;

namespace Net.NowhereAtAll.Zaturnava.Bots.FairyStockfish;

/// <summary>
/// Fairy-Stockfish, spoken to by UCI over <paramref name="channel"/>, one question at a time: each variant's
/// definition written to a file of its own in <paramref name="folder"/>, the folder the engine runs in, and loaded
/// once, by its name alone.
/// </summary>
public sealed class UciEngine(IUciChannel channel, string folder) : IDisposable
{
    private readonly IUciChannel _channel = channel;
    private readonly string _folder = folder;
    private readonly SemaphoreSlim _turn = new(1, 1);
    private ImmutableHashSet<string> _loaded = [];
    private bool _isStarted;

    /// <summary>
    /// The move Fairy-Stockfish would make, of <paramref name="moves"/> (as it writes them), thinking for
    /// <paramref name="thinking"/>; none, if it names none of them.
    /// </summary>
    public async Task<string?> BestMove(
        [Required] FairyPosition position,
        [Required] ImmutableList<string> moves,
        TimeSpan thinking,
        CancellationToken cancellationToken
    )
    {
        await this._turn.WaitAsync(cancellationToken);
        try
        {
            await this.Start(cancellationToken);
            await this.Load(position, cancellationToken);
            await this._channel.Send($"setoption name UCI_Variant value {position.Name}", cancellationToken);
            await this._channel.Send($"position fen {position.Fen}", cancellationToken);
            await this._channel.Send(
                $"go movetime {(int)thinking.TotalMilliseconds} searchmoves {string.Join(' ', moves)}",
                cancellationToken
            );
            string? best = (await this.Until("bestmove", cancellationToken))?.Split(' ')[1];
            return best is not null && moves.Contains(best) ? best : null;
        }
        finally
        {
            _ = this._turn.Release();
        }
    }

    public void Dispose()
    {
        this._channel.Dispose();
        this._turn.Dispose();
    }

    private async Task Start(CancellationToken cancellationToken)
    {
        if (!this._isStarted)
        {
            await this._channel.Send("uci", cancellationToken);
            _ = await this.Until("uciok", cancellationToken);
            this._isStarted = true;
        }
    }

    /// <summary>Loads the variant, unless it was loaded before; and waits until the engine is ready.</summary>
    private async Task Load(FairyPosition position, CancellationToken cancellationToken)
    {
        if (!this._loaded.Contains(position.Name))
        {
            string file = position.Name + ".ini";
            _ = Directory.CreateDirectory(this._folder);
            await File.WriteAllTextAsync(Path.Combine(this._folder, file), position.Definition, cancellationToken);
            await this._channel.Send($"setoption name VariantPath value {file}", cancellationToken);
            this._loaded = this._loaded.Add(position.Name);
        }

        await this._channel.Send("isready", cancellationToken);
        _ = await this.Until("readyok", cancellationToken);
    }

    /// <summary>The first line the engine says starting <paramref name="word"/>; none, if it stops first.</summary>
    private async Task<string?> Until(string word, CancellationToken cancellationToken)
    {
        string? line = await this._channel.Receive(cancellationToken);
        while (line is not null && !line.StartsWith(word, StringComparison.Ordinal))
        {
            line = await this._channel.Receive(cancellationToken);
        }

        return line;
    }
}
