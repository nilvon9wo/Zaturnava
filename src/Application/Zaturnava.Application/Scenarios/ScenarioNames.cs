using System.Collections.Immutable;
using Net.NowhereAtAll.Zaturnava.Engine.Boards;
using Net.NowhereAtAll.Zaturnava.Engine.Pieces;
using Net.NowhereAtAll.Zaturnava.Engine.Rules;
using Net.NowhereAtAll.Zaturnava.Engine.Seats;

namespace Net.NowhereAtAll.Zaturnava.Application.Scenarios;

/// <summary>
/// A ruleset's cells, seats and kinds of piece - those a new game of it has - by what people call them, in
/// any case.
/// </summary>
internal sealed class ScenarioNames(IRulesApplying rules, GameState initial)
{
    private readonly ImmutableDictionary<string, CellId> _cells =
        rules.Topology.Cells.ToImmutableDictionary(rules.Topology.NameOf, StringComparer.OrdinalIgnoreCase);

    private readonly ImmutableDictionary<string, SeatId> _seats = initial.Seats.ToImmutableDictionary(
        seat => rules.NameOf(seat.Id),
        seat => seat.Id,
        StringComparer.OrdinalIgnoreCase
    );

    private readonly ImmutableDictionary<string, PieceKindId> _kinds = initial.Position.Occupants.Values
        .Select(piece => piece.Kind)
        .Distinct()
        .ToImmutableDictionary(rules.NameOf, StringComparer.OrdinalIgnoreCase);

    /// <exception cref="InvalidScenarioException">There is no such cell.</exception>
    public CellId CellOf(string name) => Named(this._cells, name, "cell");

    /// <exception cref="InvalidScenarioException">There is no such seat.</exception>
    public SeatId SeatOf(string name) => Named(this._seats, name, "seat");

    /// <summary>The seat and kind of a piece written as both, "Black Pawn".</summary>
    /// <exception cref="InvalidScenarioException">It is not a seat and a kind of piece.</exception>
    public (SeatId Owner, PieceKindId Kind) PieceOf(string name)
    {
        KeyValuePair<string, SeatId> owner =
            this._seats.FirstOrDefault(seat => name.StartsWith(seat.Key + " ", StringComparison.OrdinalIgnoreCase));
        return owner.Key is null
            ? throw new InvalidScenarioException($"'{name}' is not a seat and a kind of piece, as 'Black Pawn' is.")
            : (owner.Value, Named(this._kinds, name[(owner.Key.Length + 1)..].Trim(), "kind of piece"));
    }

    private static T Named<T>(ImmutableDictionary<string, T> named, string name, string what) =>
        named.TryGetValue(name, out T? value)
            ? value
            : throw new InvalidScenarioException($"There is no {what} '{name}'.");
}
