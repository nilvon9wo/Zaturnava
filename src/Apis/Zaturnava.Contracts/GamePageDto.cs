using System.Collections.Immutable;

namespace Net.NowhereAtAll.Zaturnava.Contracts;

/// <summary>
/// A page of the games a lobby lists (<see cref="GameListQuery"/>), newest change first: page
/// <see cref="Page"/> of <see cref="PageSize"/> games, of <see cref="Total"/> that match in all.
/// </summary>
public sealed record GamePageDto(ImmutableList<GameSummaryDto> Games, int Total, int Page, int PageSize);
