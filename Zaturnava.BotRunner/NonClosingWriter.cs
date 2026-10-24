using System.Text;

namespace Net.NowhereAtAll.Zaturnava.BotRunner;

/// <summary>
/// Writes through to a writer someone else owns (standard output), and only flushes it when
/// disposed, so the command can dispose of whatever it writes to alike.
/// </summary>
internal sealed class NonClosingWriter(TextWriter inner) : TextWriter
{
    private readonly TextWriter _inner = inner;

    public override Encoding Encoding => this._inner.Encoding;

    public override void Write(char value) => this._inner.Write(value);

    public override void Write(string? value) => this._inner.Write(value);

    protected override void Dispose(bool disposing) => this._inner.Flush();
}
