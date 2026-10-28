namespace Net.NowhereAtAll.Zaturnava.Contracts;

/// <summary>The server refused a request, for the reason given; the message says why, for a person.</summary>
public sealed class GameRequestRefusedException(Refusal refusal, string message) : Exception(message)
{
    public Refusal Refusal { get; } = refusal;
}
