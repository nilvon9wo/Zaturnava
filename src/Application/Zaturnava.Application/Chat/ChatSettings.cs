namespace Net.NowhereAtAll.Zaturnava.Application.Chat;

/// <summary>
/// How long talk is kept: until no player at the table has been heard from (their page asks for the talk every
/// few seconds) for <see cref="LeftAfter"/>, as checked every <see cref="SweepInterval"/>.
/// </summary>
public sealed record ChatSettings(TimeSpan LeftAfter, TimeSpan SweepInterval)
{
    public static readonly TimeSpan DefaultLeftAfter = TimeSpan.FromMinutes(2);
    public static readonly TimeSpan DefaultSweepInterval = TimeSpan.FromSeconds(30);

    public static ChatSettings Default { get; } = new(DefaultLeftAfter, DefaultSweepInterval);
}
