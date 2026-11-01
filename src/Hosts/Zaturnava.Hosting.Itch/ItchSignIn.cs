using System.Collections.Immutable;
using System.Text.Json;

namespace Net.NowhereAtAll.Zaturnava.Hosting.Itch;

/// <summary>
/// Signing in with an itch.io account, by an itch.io OAuth application, which has no secret: itch.io hands the
/// token back to the browser. It asks only to read the person's profile (<c>profile:me</c>): the number itch.io
/// knows them by, which never changes, their display name if they set one, and else their user name.
/// </summary>
public sealed class ItchSignIn : ITokenSignInProviding
{
    public const string ProviderName = "Itch";

    public string Name => ProviderName;

    public Uri Authorization { get; } = new("https://itch.io/user/oauth");

    public Uri UserInformation { get; } = new("https://api.itch.io/profile");

    public ImmutableList<string> Scopes { get; } = ["profile:me"];

    public (string Key, string? Name) IdentityOf(JsonElement user)
    {
        JsonElement profile = user.GetProperty("user");
        return (profile.GetProperty("id").ToString(), NameOf(profile, "display_name") ?? NameOf(profile, "username"));
    }

    private static string? NameOf(JsonElement user, string property) =>
        user.TryGetProperty(property, out JsonElement name) && name.ValueKind == JsonValueKind.String
            ? name.GetString()
            : null;
}
