using System.ComponentModel.DataAnnotations;

namespace Net.NowhereAtAll.Zaturnava.Contracts;

/// <summary>
/// A board's size as a game's creator may set it (<see cref="RulesetDto.SizableBoard"/>): <paramref name="Width"/>
/// cells across and <paramref name="Height"/> up, each kingdom <paramref name="KingdomWidth"/> along its back rank and
/// <paramref name="KingdomDepth"/> deep. The ruleset says which sizes it is played at; a game set to another is
/// refused.
/// </summary>
public sealed record BoardSizeDto(
    [property: Range(1, 26)] int Width,
    [property: Range(1, 26)] int Height,
    [property: Range(1, 26)] int KingdomWidth,
    [property: Range(1, 26)] int KingdomDepth
);
