using System.Collections.Immutable;
using System.Text.Json;

namespace Net.NowhereAtAll.Zaturnava.Hosting;

/// <summary>
/// A service people sign in with by OAuth 2.0's implicit grant: it hands the token back to the browser, in the
/// address's fragment, with no secret of the server's; the server then asks, with that token, who they are.
/// </summary>
public interface ITokenSignInProviding
{
    /// <summary>The name the server knows it by, in its routes and its configuration.</summary>
    string Name { get; }

    Uri Authorization { get; }

    Uri UserInformation { get; }

    /// <summary>What to ask leave to read; none, if who someone is needs no leave.</summary>
    ImmutableList<string> Scopes { get; }

    /// <summary>
    /// Who someone is, from what <see cref="UserInformation"/> says of them: the key the service knows them by,
    /// which never changes, and the name they go by, if they gave one.
    /// </summary>
    (string Key, string? Name) IdentityOf(JsonElement user);
}
