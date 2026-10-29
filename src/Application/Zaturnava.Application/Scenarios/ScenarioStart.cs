using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using Net.NowhereAtAll.Zaturnava.Engine.Actions;
using Net.NowhereAtAll.Zaturnava.Engine.Pieces;
using Net.NowhereAtAll.Zaturnava.Engine.Positions;
using Net.NowhereAtAll.Zaturnava.Engine.Rules;
using Net.NowhereAtAll.Zaturnava.Engine.Seats;

namespace Net.NowhereAtAll.Zaturnava.Application.Scenarios;

/// <summary>
/// How a game begins from a <see cref="Scenario"/>: its first action, which no ruleset sees, carries the
/// scenario whole, so that the game replays from the scenario as it was when the game began, however its
/// file changes since.
/// </summary>
public static class ScenarioStart
{
    public const string Kind = "scenario";
    private const string Argument = "scenario";

    public static ActionDescriptor Describe([Required] Scenario scenario) =>
        new(
            Kind,
            ImmutableSortedDictionary<string, string>.Empty
                .Add(Argument, JsonSerializer.Serialize(scenario, JsonSerializerOptions.Web))
        );

    /// <summary>Where a game of these actions starts: as the scenario its first carries, if any; else as new.</summary>
    public static GameState StateOf(
        [Required] ImmutableList<ActionDescriptor> actions,
        [Required] IRulesApplying rules
    ) =>
        actions.FirstOrDefault() is { Kind: Kind } first
            ? StateFrom(ScenarioOf(first), rules)
            : rules.CreateInitialState();

    /// <summary>
    /// A new game of the ruleset, set up as the scenario says. Each piece is given the id of a piece of its seat
    /// and kind that a new game has, in turn, so that it has that piece's home; any more than a new game has
    /// are given ids of their own.
    /// </summary>
    /// <exception cref="InvalidScenarioException">It names a cell, seat or kind of piece the ruleset lacks.</exception>
    public static GameState StateFrom([Required] Scenario scenario, [Required] IRulesApplying rules)
    {
        GameState initial = rules.CreateInitialState();
        ScenarioNames names = new(rules, initial);
        ImmutableList<(SeatId Owner, PieceKindId Kind)> standing = [.. scenario.Pieces.Values.Select(names.PieceOf)];
        ImmutableList<(SeatId Owner, PieceKindId Kind)> held =
            [.. scenario.Prisoners.Select(prisoner => names.PieceOf(prisoner.Piece))];
        ImmutableList<Piece> pieces = Identified(initial, standing.AddRange(held));
        Position position = scenario.Pieces.Keys
            .Zip(pieces)
            .Aggregate(Position.Empty, (board, placed) => board.Place(names.CellOf(placed.First), placed.Second));
        return initial with
        {
            Position = position,
            DecidingSeat = scenario.Turn is { } turn ? names.SeatOf(turn) : initial.Seats[0].Id,
            TurnNumber = 1,
            Scores = initial.Scores.SetItems(
                scenario.Scores.Select(score => KeyValuePair.Create(names.SeatOf(score.Key), score.Value))
            ),
            Prisoners =
            [
                .. scenario.Prisoners
                    .Zip(pieces.Skip(standing.Count))
                    .Select(prisoner => new Prisoner(prisoner.Second, names.SeatOf(prisoner.First.Holder))),
            ],
            RollOff = [],
            AwaitedDice = null,
            Dice = scenario.Dice,
        };
    }

    private static Scenario ScenarioOf(ActionDescriptor start) =>
        JsonSerializer.Deserialize<Scenario>(start.Arguments[Argument], JsonSerializerOptions.Web)!;

    private static ImmutableList<Piece> Identified(
        GameState initial,
        ImmutableList<(SeatId Owner, PieceKindId Kind)> wanted
    )
    {
        ILookup<(SeatId Owner, PieceKindId Kind), PieceId> starting = initial.Position.Occupants.Values
            .OrderBy(piece => piece.Id.Value)
            .ToLookup(piece => (piece.Owner, piece.Kind), piece => piece.Id);
        int spare = initial.Position.Occupants.Values.Select(piece => piece.Id.Value).DefaultIfEmpty(-1).Max() + 1;
        return
        [
            .. wanted.Select((piece, index) => new Piece(
                starting[piece]
                    .Select(id => (PieceId?)id)
                    .ElementAtOrDefault(wanted.Take(index).Count(earlier => earlier == piece))
                ?? new PieceId(spare + index),
                piece.Kind,
                piece.Owner
            )),
        ];
    }
}
