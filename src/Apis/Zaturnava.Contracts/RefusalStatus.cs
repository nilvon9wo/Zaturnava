using System.ComponentModel.DataAnnotations;

namespace Net.NowhereAtAll.Zaturnava.Contracts;

/// <summary>
/// How each <see cref="Refusal"/> travels over HTTP: one table, read by the server to answer and by
/// clients to understand the answer.
/// </summary>
public static class RefusalStatus
{
    public const int Invalid = 400;
    public const int Forbidden = 403;
    public const int NotFound = 404;
    public const int Conflict = 409;
    public const int Illegal = 422;
    public const int TooBusy = 429;

    public static int CodeOf(Refusal refusal) =>
        refusal switch
        {
            Refusal.NotFound => NotFound,
            Refusal.Forbidden => Forbidden,
            Refusal.Conflict => Conflict,
            Refusal.Illegal => Illegal,
            Refusal.TooBusy => TooBusy,
            _ => Invalid,
        };

    /// <summary>The refusal an HTTP status reports, or null for a status that reports none.</summary>
    public static Refusal? RefusalOf([Range(100, 599)] int statusCode) =>
        statusCode switch
        {
            Invalid => Refusal.Invalid,
            Forbidden => Refusal.Forbidden,
            NotFound => Refusal.NotFound,
            Conflict => Refusal.Conflict,
            Illegal => Refusal.Illegal,
            TooBusy => Refusal.TooBusy,
            _ => null,
        };
}
