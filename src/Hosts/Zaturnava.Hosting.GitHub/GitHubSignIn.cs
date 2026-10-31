using System.Collections.Immutable;
using System.Globalization;
using System.Text.Json;

namespace Net.NowhereAtAll.Zaturnava.Hosting.GitHub;

/// <summary>
/// Signing in with a GitHub account, by a GitHub OAuth app. Who someone is needs no leave beyond the app's own:
/// their public profile gives the number GitHub knows them by, which never changes, their name if they set one,
/// and else their login.
/// </summary>
public sealed class GitHubSignIn : ISignInProviding
{
    public const string ProviderName = "GitHub";

    public string Name => ProviderName;

    public Uri Authorization { get; } = new("https://github.com/login/oauth/authorize");

    public Uri Token { get; } = new("https://github.com/login/oauth/access_token");

    public Uri UserInformation { get; } = new("https://api.github.com/user");

    public ImmutableList<string> Scopes { get; } = [];

    public (string Key, string? Name) IdentityOf(JsonElement user) =>
        (
            user.GetProperty("id").GetInt64().ToString(CultureInfo.InvariantCulture),
            NameOf(user, "name") ?? NameOf(user, "login")
        );

    private static string? NameOf(JsonElement user, string property) =>
        user.TryGetProperty(property, out JsonElement name) && name.ValueKind == JsonValueKind.String
            ? name.GetString()
            : null;
}
