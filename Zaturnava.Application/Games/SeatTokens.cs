using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using System.Text;

namespace Net.NowhereAtAll.Zaturnava.Application.Games;

/// <summary>
/// A claimed seat's token is its only credential: whoever holds it plays the seat, with no account
/// and no identity provider needed. A login (itch.io, or any other) can be layered on later.
/// </summary>
public static class SeatTokens
{
    private const int TokenBytes = 32;

    public static string Issue()
    {
        byte[] secret = RandomNumberGenerator.GetBytes(TokenBytes);
        return Convert.ToHexString(secret);
    }

    public static string Hash([Required] string token)
    {
        byte[] text = Encoding.UTF8.GetBytes(token);
        byte[] digest = SHA256.HashData(text);
        return Convert.ToHexString(digest);
    }
}
