using System.Net;
using System.Text;

namespace Net.NowhereAtAll.Zaturnava.Client.Test;

/// <summary>
/// Stands in for the game server: answers every request with one status and body, and remembers
/// what it was sent - read before the client disposes of the request.
/// </summary>
internal sealed class StubServer(HttpStatusCode status, string body) : HttpMessageHandler
{
    public static readonly Uri BaseAddress = new("https://games.example/");

    public string? Method { get; private set; }

    public string? Path { get; private set; }

    public string? Authorization { get; private set; }

    public string? Body { get; private set; }

    public HttpClient Client() => new(this) { BaseAddress = BaseAddress };

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken
    )
    {
        this.Method = request.Method.Method;
        this.Path = request.RequestUri?.AbsolutePath;
        this.Authorization = request.Headers.Authorization?.ToString();
        this.Body = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
        return new HttpResponseMessage(status)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json"),
        };
    }
}
