using System.Collections.Immutable;
using System.Text.Json;

namespace Net.NowhereAtAll.Zaturnava.Hosting;

/// <summary>
/// A service people sign in with, by OAuth 2.0: where to send them to agree, where to swap what they bring
/// back for a token, and where to ask, with that token, who they are.
/// </summary>
public interface ISignInProviding
{
    /// <summary>The name the server knows it by, in its routes and its configuration.</summary>
    string Name { get; }

    Uri Authorization { get; }

    Uri Token { get; }

    Uri UserInformation { get; }

    /// <summary>What to ask leave to read; none, if who someone is needs no leave.</summary>
    ImmutableList<string> Scopes { get; }

    /// <summary>
    /// Who someone is, from what <see cref="UserInformation"/> says of them: the key the service knows them by,
    /// which never changes, and the name they go by, if they gave one.
    /// </summary>
    (string Key, string? Name) IdentityOf(JsonElement user);
}
