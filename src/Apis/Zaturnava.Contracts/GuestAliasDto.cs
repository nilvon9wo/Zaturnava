namespace Net.NowhereAtAll.Zaturnava.Contracts;

/// <summary>
/// The alias a guest holds for their visit, and the token they hold it by (<see cref="GameRoutes.Aliases"/>): no
/// other guest holds it meanwhile, and no player goes by it, whatever its case. Unless renewed
/// (<see cref="GameRoutes.MyAlias"/>), it lapses after a while (12 hours, for now).
/// </summary>
public sealed record GuestAliasDto(string Alias, string Token);
