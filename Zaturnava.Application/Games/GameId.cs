namespace Net.NowhereAtAll.Zaturnava.Application.Games;

public readonly record struct GameId(string Value)
{
    public static GameId New() => new(Guid.NewGuid().ToString("N"));
}
