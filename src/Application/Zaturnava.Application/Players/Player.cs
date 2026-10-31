using System.Collections.Immutable;

namespace Net.NowhereAtAll.Zaturnava.Application.Players;

/// <summary>A person who has signed in: the name they are shown by, and every login that is theirs.</summary>
public sealed record Player(PlayerId Id, string Name, ImmutableList<Login> Logins);
