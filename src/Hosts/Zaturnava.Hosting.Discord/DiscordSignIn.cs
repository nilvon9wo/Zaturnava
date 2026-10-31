using System.Collections.Immutable;
using System.Text.Json;

namespace Net.NowhereAtAll.Zaturnava.Hosting.Discord;

/// <summary>
/// Signing in with a Discord account, by a Discord application. It asks only to know who someone is
/// (<c>identify</c>), never their e-mail or their servers: the number Discord knows them by, which never changes,
/// their display name if they set one, and else their user name.
/// </summary>
public sealed class DiscordSignIn : ISignInProviding
{
    public const string ProviderName = "Discord";

    public string Name => ProviderName;

    public Uri Authorization { get; } = new("https://discord.com/oauth2/authorize");

    public Uri Token { get; } = new("https://discord.com/api/oauth2/token");

    public Uri UserInformation { get; } = new("https://discord.com/api/users/@me");

    public ImmutableList<string> Scopes { get; } = ["identify"];

    public (string Key, string? Name) IdentityOf(JsonElement user) =>
        (user.GetProperty("id").ToString(), NameOf(user, "global_name") ?? NameOf(user, "username"));

    private static string? NameOf(JsonElement user, string property) =>
        user.TryGetProperty(property, out JsonElement name) && name.ValueKind == JsonValueKind.String
            ? name.GetString()
            : null;
}
