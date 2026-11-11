using Net.NowhereAtAll.Zaturnava.Contracts;

namespace Net.NowhereAtAll.Zaturnava.Web;

/// <summary>
/// Who takes a seat from this browser: the player signed in, by their token, if one is; else a guest, by the
/// nickname they play by, if they gave one, and the token they hold it by as their alias, if they hold it.
/// </summary>
internal sealed record Claimant(string? PlayerToken, string? Nickname, string? AliasToken = null)
{
    /// <summary>What a claim of a seat says of the guest, should no player be signed in.</summary>
    public ClaimSeatRequest Claim => new(this.Nickname, AliasToken: this.AliasToken);
}
