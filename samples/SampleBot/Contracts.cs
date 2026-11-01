using System.Collections.Immutable;

namespace SampleBot;

// The parts of the server's JSON this bot reads, and no more: a client need not model what it ignores. Names are
// camelCase on the wire; the serializer maps them (GameApi). The whole of the view is described at
// docs/bots/http-api.md#the-view, and as OpenAPI at {server}/openapi/v1.json, to generate a full client from.

/// <summary>One of the legal actions: sent back as it came, the server takes it.</summary>
internal sealed record GameAction(string Kind, ImmutableSortedDictionary<string, string> Arguments)
{
    /// <summary>What a page would print on its button; the server ignores it.</summary>
    public string? Label { get; init; }
}

internal sealed record Piece(int Id, string Kind, string Name, int Owner, int Cell);

internal sealed record Seat(int Id, string Name, int Team, int Score, string Controller);

internal sealed record Outcome(ImmutableList<int> Winners, string Ending, string Text);

/// <summary>The game as it stands, as <c>GET api/games/{id}</c> gives it.</summary>
internal sealed record GameView(
    string Id,
    int Version,
    int? DecidingSeat,
    ImmutableList<Piece> Pieces,
    ImmutableList<Seat> Seats,
    ImmutableList<GameAction> LegalActions,
    Outcome? Outcome
);

internal sealed record Ruleset(string Id, string Name, int SeatCount);

/// <summary>What the server hosts, as <c>GET api/catalog</c> gives it: here, only its rulesets.</summary>
internal sealed record Catalog(ImmutableList<Ruleset> Rulesets);

internal sealed record GameCreated(string GameId);

internal sealed record SeatClaimed(string Token);

internal sealed record CreateGame(string RulesetId, ImmutableList<string?> Bots);

internal sealed record ClaimSeat(string? Nickname, bool IsAi);

internal sealed record Advance(int Version);

/// <summary>A refusal, as the server words it: which (<see cref="Title"/>), and why.</summary>
internal sealed record Problem(string? Title, string? Detail);
