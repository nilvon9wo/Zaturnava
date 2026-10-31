namespace Net.NowhereAtAll.Zaturnava.Application.Players;

public readonly record struct PlayerId(string Value)
{
    public static PlayerId New() => new(Guid.NewGuid().ToString("N"));
}
