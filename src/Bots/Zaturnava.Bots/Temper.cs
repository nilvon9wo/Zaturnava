namespace Net.NowhereAtAll.Zaturnava.Bots;

/// <summary>
/// How readily a bot calls a game decided (<see cref="Ending"/>; F6, Asked 2026-11-25: "each bot by its own
/// threshold"): <paramref name="Lead"/>, the ruleset's clear lead it wants, as a part of it, before proposing to end a
/// game it is winning; <paramref name="Optimism"/>, how its team's hope is counted against its face before it calls a
/// game it is losing hopeless (Proposed).
/// </summary>
internal sealed record Temper(double Lead, double Optimism)
{
    /// <summary>
    /// Most bots': half as much again as the ruleset's clear lead, and hope at <see cref="Ending.Optimism"/> its face
    /// (the user, 2026-11-28: "The bots are still too eager to end the game. Red/yellow is only up 8 points and they
    /// don't have that much more material"; the clear lead itself, 10, till then).
    /// </summary>
    public static Temper Even { get; } = new(1.5, Ending.Optimism);

    /// <summary>
    /// A searching bot's, which sees a game won a little sooner (Sage, Tactician; Proposed): a quarter more than the
    /// clear lead (three quarters of it, 7.5, till 2026-11-28, when Sage and Tactician proposed 8 points up); its hope
    /// as most bots', so that it does not concede a close game.
    /// </summary>
    public static Temper Realist { get; } = new(1.25, Ending.Optimism);

    /// <summary>
    /// The aimless bot's (Random; Proposed), which plays on: three times the clear lead, and hope at twice its face.
    /// </summary>
    public static Temper Dogged { get; } = new(3, 2);
}
