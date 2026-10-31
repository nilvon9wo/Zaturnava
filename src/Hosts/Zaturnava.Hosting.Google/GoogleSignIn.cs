using System.Collections.Immutable;
using System.Text.Json;

namespace Net.NowhereAtAll.Zaturnava.Hosting.Google;

/// <summary>
/// Signing in with a Google account, by a Google OAuth client. It asks only to know who someone is
/// (<c>openid</c>, <c>profile</c>), never their e-mail: the number Google knows them by (<c>sub</c>), which never
/// changes, and their name if they let it be seen.
/// </summary>
public sealed class GoogleSignIn : ISignInProviding
{
    public const string ProviderName = "Google";

    public string Name => ProviderName;

    public Uri Authorization { get; } = new("https://accounts.google.com/o/oauth2/v2/auth");

    public Uri Token { get; } = new("https://oauth2.googleapis.com/token");

    public Uri UserInformation { get; } = new("https://openidconnect.googleapis.com/v1/userinfo");

    public ImmutableList<string> Scopes { get; } = ["openid", "profile"];

    public (string Key, string? Name) IdentityOf(JsonElement user) =>
        (user.GetProperty("sub").ToString(), NameOf(user));

    private static string? NameOf(JsonElement user) =>
        user.TryGetProperty("name", out JsonElement name) && name.ValueKind == JsonValueKind.String
            ? name.GetString()
            : null;
}
