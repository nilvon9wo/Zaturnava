using System.Collections.Immutable;

namespace Net.NowhereAtAll.Zaturnava.Contracts;

/// <summary>
/// A player who has signed in: the id that is theirs here, their name, and the services they may sign in with
/// (<see cref="GameRoutes.Logins"/>).
/// </summary>
public sealed record PlayerDto(string Id, string Name, ImmutableList<string>? Services = null);
