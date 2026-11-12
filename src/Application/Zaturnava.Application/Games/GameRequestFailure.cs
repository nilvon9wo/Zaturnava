namespace Net.NowhereAtAll.Zaturnava.Application.Games;

public enum GameRequestFailure
{
    NotFound,
    Invalid,
    Forbidden,
    Conflict,

    /// <summary>The game is there, but can no longer be shown or played.</summary>
    Gone,
}
