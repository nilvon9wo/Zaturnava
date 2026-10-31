using System.Security.Cryptography;
using System.Text;

namespace Net.NowhereAtAll.Zaturnava.Server.Composition;

/// <summary>
/// What the hosted MCP server's calls to this server carry (<see cref="Header"/>), so that they count as made by
/// the address that called the MCP server (<see cref="ForHeader"/>), not by this server's own. A secret made anew
/// each time the server starts, never sent anywhere but to this server: no one else can claim another's address.
/// </summary>
internal sealed class SelfPass
{
    public const string Header = "X-Zaturnava-Self";
    public const string ForHeader = "X-Zaturnava-Self-For";

    private readonly byte[] _secret = Encoding.UTF8.GetBytes(Convert.ToHexString(RandomNumberGenerator.GetBytes(32)));

    public string Value => Encoding.UTF8.GetString(this._secret);

    /// <summary>
    /// The address a request counts as made by: the one it names, if it carries the pass; its own, otherwise
    /// (empty, if it has none).
    /// </summary>
    public string AddressOf(HttpContext context) =>
        this.IsCarriedBy(context.Request)
            ? context.Request.Headers[ForHeader].ToString()
            : OwnAddressOf(context);

    /// <summary>The address the request came from; empty, if it has none.</summary>
    public static string OwnAddressOf(HttpContext context) => $"{context.Connection.RemoteIpAddress}";

    private bool IsCarriedBy(HttpRequest request) =>
        CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(request.Headers[Header].ToString()),
            this._secret
        );
}
