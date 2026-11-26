namespace Net.NowhereAtAll.Zaturnava.Bots;

/// <summary>
/// How readily a bot calls a game decided (<see cref="Ending"/>; F6, Asked 2026-11-25: "each bot by its own
/// threshold"): <paramref name="Lead"/>, the ruleset's clear lead it wants, as a part of it, before proposing to end a
/// game it is winning; <paramref name="Optimism"/>, how its team's hope is counted against its face before it calls a
/// game it is losing hopeless (Proposed).
/// </summary>
internal sealed record Temper(double Lead, double Optimism)
{
    /// <summary>Most bots': the ruleset's clear lead, and hope at <see cref="Ending.Optimism"/> its face.</summary>
    public static Temper Even { get; } = new(1, Ending.Optimism);

    /// <summary>
    /// A searching bot's, which sees a game won sooner (Sage, Tactician; Proposed): three quarters of the clear lead;
    /// its hope as most bots', so that it does not concede a close game.
    /// </summary>
    public static Temper Realist { get; } = new(0.75, Ending.Optimism);

    /// <summary>
    /// The aimless bot's (Random; Proposed), which plays on: twice the clear lead, and hope at twice its face.
    /// </summary>
    public static Temper Dogged { get; } = new(2, 2);
}
