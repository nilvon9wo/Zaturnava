namespace Net.NowhereAtAll.Zaturnava.Contracts;

/// <summary>
/// The name a player would be shown by from now on (<see cref="GameRoutes.Me"/>): at most
/// <see cref="MaximumNameLength"/> characters, and cut if longer; one of only spaces changes nothing.
/// </summary>
public sealed record RenameRequest(string Name)
{
    public const int MaximumNameLength = 40;
}
