namespace Net.NowhereAtAll.Zaturnava.Contracts;

/// <summary>Why the server refused a request.</summary>
public enum Refusal
{
    /// <summary>The request makes no sense: an unknown ruleset or bot, a malformed action.</summary>
    Invalid,

    /// <summary>There is no such game.</summary>
    NotFound,

    /// <summary>The token does not hold the seat that must decide.</summary>
    Forbidden,

    /// <summary>The seat is taken, or the game changed meanwhile.</summary>
    Conflict,

    /// <summary>The rules do not allow the action now.</summary>
    Illegal,

    /// <summary>Too many requests from the same address of late: wait, and ask again.</summary>
    TooBusy,
}
