using System.ComponentModel.DataAnnotations;
using System.Diagnostics;

namespace Net.NowhereAtAll.Zaturnava.Bots.FairyStockfish;

/// <summary>A UCI engine run as a program of its own, spoken to on its standard input and output.</summary>
public sealed class ProcessUciChannel : IUciChannel
{
    private readonly Process _process;

    /// <summary>
    /// Starts <paramref name="path"/>, with <paramref name="arguments"/>, in <paramref name="folder"/>, made if need
    /// be: so that a file there may be named to it alone, even a Windows program run from Linux.
    /// </summary>
    public ProcessUciChannel([Required] string path, [Required] string folder, [Required] params string[] arguments)
    {
        ProcessStartInfo start = new(path, arguments)
        {
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = Directory.CreateDirectory(folder).FullName,
        };
        this._process = Process.Start(start)!; // a program, not a document, so a process always starts or throws
    }

    public async Task Send([Required] string line, CancellationToken cancellationToken)
    {
        await this._process.StandardInput.WriteLineAsync(line.AsMemory(), cancellationToken);
        await this._process.StandardInput.FlushAsync(cancellationToken);
    }

    public async Task<string?> Receive(CancellationToken cancellationToken) =>
        await this._process.StandardOutput.ReadLineAsync(cancellationToken);

    /// <summary>Stops the engine, if it has not stopped itself.</summary>
    public void Dispose()
    {
        this._process.Kill(entireProcessTree: true);
        this._process.Dispose();
    }
}
