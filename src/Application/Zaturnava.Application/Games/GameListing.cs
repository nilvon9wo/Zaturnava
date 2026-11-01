using System.Collections.Immutable;

namespace Net.NowhereAtAll.Zaturnava.Application.Games;

/// <summary>
/// A game as a lobby lists it, read as stored, without replaying it: who controls each seat, how many actions have
/// been taken, and whether it is over.
/// </summary>
public sealed record GameListing(
    GameId Id,
    string RulesetId,
    int Version,
    ImmutableList<SeatController> Controllers,
    int Moves,
    bool IsOver
);
