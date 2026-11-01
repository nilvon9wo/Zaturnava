using System.Collections.Immutable;

namespace Net.NowhereAtAll.Zaturnava.Application.Games;

/// <summary>A page of the games a query asks for, and how many there are in all.</summary>
public sealed record GamePage(ImmutableList<GameListing> Games, int Total);
