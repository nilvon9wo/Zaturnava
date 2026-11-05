using System.Collections.Immutable;

namespace McpSampleBot;

// The parts of the tools' JSON this bot reads, and no more: a client need not model what it ignores. Each tool
// answers with the JSON the HTTP API's route would (docs/bots/http-api.md#the-view), camelCase; the serializer maps
// it (GameTools).

/// <summary>One of the legal actions: sent back as it came, the server takes it.</summary>
internal sealed record GameAction(string Kind, ImmutableSortedDictionary<string, string> Arguments)
{
    /// <summary>What a page would print on its button; the server ignores it.</summary>
    public string? Label { get; init; }
}

internal sealed record Piece(int Id, string Kind, string Name, int Owner, int Cell);

internal sealed record Seat(int Id, string Name, int Team, int Score, string Controller);

internal sealed record Outcome(ImmutableList<int> Winners, string Ending, string Text);

/// <summary>The game as it stands, as the <c>get_game</c> tool gives it.</summary>
internal sealed record GameView(
    string Id,
    int Version,
    int? DecidingSeat,
    ImmutableList<Piece> Pieces,
    ImmutableList<Seat> Seats,
    ImmutableList<GameAction> LegalActions,
    Outcome? Outcome
);

/// <summary>
/// A ruleset: what it calls each seat (Black, Yellow...), and each seat's team, by number: seats of one number are
/// allies.
/// </summary>
internal sealed record Ruleset(
    string Id,
    string Name,
    int SeatCount,
    ImmutableList<string> SeatNames,
    ImmutableList<int> SeatTeams
);

/// <summary>What the server hosts, as the <c>catalog</c> tool gives it: here, only its rulesets.</summary>
internal sealed record Catalog(ImmutableList<Ruleset> Rulesets);

internal sealed record GameCreated(string GameId);

internal sealed record SeatClaimed(string Token);
