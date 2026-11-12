using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Text.Json;
using Net.NowhereAtAll.Zaturnava.Engine.Actions;
using Net.NowhereAtAll.Zaturnava.Engine.Chance;
using Net.NowhereAtAll.Zaturnava.Engine.Pieces;
using Net.NowhereAtAll.Zaturnava.Engine.Positions;
using Net.NowhereAtAll.Zaturnava.Engine.Rules;
using Net.NowhereAtAll.Zaturnava.Engine.Seats;

namespace Net.NowhereAtAll.Zaturnava.Application.Scenarios;

/// <summary>
/// How a game begins, if not as its ruleset begins every game: from a <see cref="Scenario"/>, its first action,
/// which no ruleset sees, carrying the scenario whole, so that the game replays from the scenario as it was when
/// the game began, however its file changes since; or dealt anew (<see cref="IRulesApplying.Deal"/>), its first
/// action carrying the seed it was dealt by.
/// </summary>
public static class ScenarioStart
{
    public const string Kind = "scenario";

    /// <summary>The kind of the first action of a game dealt anew.</summary>
    public const string DealKind = "deal";

    private const string Argument = "scenario";
    private const string SeedArgument = "seed";
    private const string AlikeArgument = "alike";
    private const string TerrainArgument = "terrain";
    private const string DraftedArgument = "drafted";

    /// <summary>The roll that seeds a game dealt anew: one die of as many sides as a seed may be.</summary>
    private static readonly DiceRequest SeedRoll = new(1, int.MaxValue - 1);

    /// <summary>Whether the action begins the game, rather than being played in it.</summary>
    public static bool IsStart([Required] ActionDescriptor action) => action.Kind is Kind or DealKind;

    /// <summary>
    /// The first action of a game dealt by a seed <paramref name="dice"/> roll, its terrain too: every army alike, if
    /// <paramref name="areAlike"/>, which a game dealt before armies could be alike never says; nor does one dealt
    /// before terrain was say that its terrain is. Its armies drafted by its players, not dealt, if
    /// <paramref name="isDrafted"/> (<see cref="IRulesApplying.Drafted"/>).
    /// </summary>
    public static ActionDescriptor Dealt([Required] IDiceRolling dice, bool areAlike = false, bool isDrafted = false)
    {
        ImmutableSortedDictionary<string, string> seeded = ImmutableSortedDictionary<string, string>.Empty
            .Add(SeedArgument, dice.Roll(SeedRoll)[0].ToString(CultureInfo.InvariantCulture))
            .Add(TerrainArgument, ActionDescriptor.Flag(true));
        ImmutableSortedDictionary<string, string> alike =
            areAlike ? seeded.Add(AlikeArgument, ActionDescriptor.Flag(true)) : seeded;
        return new ActionDescriptor(
            DealKind,
            isDrafted ? alike.Add(DraftedArgument, ActionDescriptor.Flag(true)) : alike
        );
    }

    /// <summary>Whether the first of <paramref name="actions"/> had the armies drafted by the players.</summary>
    public static bool IsDrafted([Required] ImmutableList<ActionDescriptor> actions) =>
        actions.FirstOrDefault() is { Kind: DealKind } first && first.Arguments.ContainsKey(DraftedArgument);

    /// <summary>Whether the first of <paramref name="actions"/> dealt every army alike.</summary>
    public static bool AreAlike([Required] ImmutableList<ActionDescriptor> actions) =>
        actions.FirstOrDefault() is { Kind: DealKind } first && first.Arguments.ContainsKey(AlikeArgument);

    public static ActionDescriptor Describe([Required] Scenario scenario) =>
        new(
            Kind,
            ImmutableSortedDictionary<string, string>.Empty
                .Add(Argument, JsonSerializer.Serialize(scenario, JsonSerializerOptions.Web))
        );

    /// <summary>
    /// Where a game of these actions starts: as the scenario its first carries, if any; as dealt by the seed it
    /// carries, if it is dealt; else as new.
    /// </summary>
    public static GameState StateOf(
        [Required] ImmutableList<ActionDescriptor> actions,
        [Required] IRulesApplying rules
    ) =>
        actions.FirstOrDefault() switch
        {
            { Kind: Kind } first => StateFrom(ScenarioOf(first), rules),
            { Kind: DealKind } first => DealtBy(
                rules,
                int.Parse(first.Arguments[SeedArgument], CultureInfo.InvariantCulture),
                actions
            ),
            _ => rules.CreateInitialState(),
        };

    /// <summary>
    /// The game dealt by <paramref name="seed"/>, its armies then drafted by its players, if so begun.
    /// </summary>
    private static GameState DealtBy(IRulesApplying rules, int seed, ImmutableList<ActionDescriptor> actions)
    {
        GameState dealt = rules.Deal(
            rules.CreateInitialState(),
            seed,
            AreAlike(actions),
            actions[0].Arguments.ContainsKey(TerrainArgument)
        );
        return IsDrafted(actions) ? rules.Drafted(dealt, seed) : dealt;
    }

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
