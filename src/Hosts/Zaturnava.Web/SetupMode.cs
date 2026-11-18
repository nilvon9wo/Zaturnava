namespace Net.NowhereAtAll.Zaturnava.Web;

/// <summary>
/// How the lobby sets up a new game: at once; as the player chooses, by how strong each bot is; or from a scenario,
/// set up as it needs.
/// </summary>
internal enum SetupMode
{
    Quick,
    Custom,
    Scenarios,
}
