using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Text.Json;
using Net.NowhereAtAll.Zaturnava.Engine.Actions;
using Net.NowhereAtAll.Zaturnava.Engine.Boards;
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

    /// <summary>
    /// The kind of the first action of a game neither dealt nor from a scenario, begun as the ruleset begins every game
    /// but as its creator set it: without dice (the user's request of 2026-11-19), or ending after so many idle rounds.
    /// </summary>
    public const string SetupKind = "setup";

    /// <summary>The argument of a game's first action saying it is played without dice.</summary>
    public const string DiceOffArgument = "dice-off";

    /// <summary>The argument of a game's first action saying how many idle rounds end it.</summary>
    private const string IdleRoundsArgument = "idle-rounds";

    /// <summary>The argument of a game's first action saying which seat opens a game without dice.</summary>
    private const string OpenerArgument = "opener";

    /// <summary>The argument of a game's first action saying a team with no leader left has lost.</summary>
    private const string LeadersArgument = "lost-without-leaders";

    /// <summary>The argument of a game's first action saying how many seconds idle pass a player's turn.</summary>
    private const string IdleTimeArgument = "idle-seconds";

    /// <summary>
    /// The edition of a ruleset's pieces and rules a game dealt now is dealt from (<see cref="GameState.Edition"/>): 1,
    /// since Grand Legions gained the Checkers and Tafl pieces (2026-11-13); 2, since it gained mercenaries (the same
    /// day); 3, since its quick armies spread their budget evenly (the same day); 4, since it gained the Berolina
    /// Pawn, the Manchu Chariot and the Courier (the same day); 5, since its armies' budgets follow the designer's
    /// formula, and the pieces that take several at once are priced for it (the same day); 6, since a chain of jumps
    /// is played jump by jump (the same day); 7, since the Guard is priced as the Minister (the same day); 8, since a
    /// Xiangqi Elephant's river is the one where it stands, palace-bound pieces go home within their palace, and the
    /// Tafl custodians hold with allies and go berserk (2026-11-18); 9, since an army filled at random keeps the palace
    /// its player placed (2026-11-19); 10, since an army aims at a composition and a budget for it, its pieces drawn
    /// the rarer the further their price from their class's commonest (the same day); 11, since it gained the
    /// Evangelist (the same day); 12, since a second leader a palace holds starts in its palace (the same day); 13,
    /// since an army's palace is dealt anywhere in its kingdom, not always against the board's edge (the same day);
    /// 14, since a palace standing partly outside its kingdom leaves out the squares another army's could share (the
    /// same day); 15, since the Tafl King may leave the board on the move it reaches its far corner (the same day); 16,
    /// since it may leave at any point of its turn, using no die, as a piece is promoted (the same day). A game dealt
    /// before says none, and is 0.
    /// </summary>
    public const int Edition = 16;

    private const string Argument = "scenario";
    private const string SeedArgument = "seed";
    private const string AlikeArgument = "alike";
    private const string TerrainArgument = "terrain";
    private const string PalacesArgument = "palaces";
    private const string PalacePiecesArgument = "palace-pieces";
    private const string EdgePalacesArgument = "edge-palaces";
    private const string EditionArgument = "edition";
    private const string DraftedArgument = "drafted";
    private const string BudgetArgument = "budget";
    private const string ArmyBudgetArgument = "army-budget";
    private const string DrafterArgument = "drafter";
    private const string SecretArgument = "secret";
    private const string HiddenFromAlliesArgument = "hidden-from-allies";
    private const string AllAtOnceArgument = "all-at-once";
    private const string SideArgument = "side";
    private const string HeightArgument = "height";
    private const string SidePromotionArgument = "side-promotion";
    private const string KingdomWidthArgument = "kingdom-width";
    private const string KingdomDepthArgument = "kingdom-depth";
    private const string ChosenTerrainArgument = "chosen-terrain";
    private const string FamiliesArgument = "families";

    /// <summary>The argument of a game's first action keeping what its deal dealt (<see cref="KeptDeal"/>).</summary>
    public const string KeptArgument = "kept";

    /// <summary>The roll that seeds a game dealt anew: one die of as many sides as a seed may be.</summary>
    private static readonly DiceRequest SeedRoll = new(1, int.MaxValue - 1);

    /// <summary>Whether the action begins the game, rather than being played in it.</summary>
    public static bool IsStart([Required] ActionDescriptor action) => action.Kind is Kind or DealKind or SetupKind;

    /// <summary>The first action of a game begun as its ruleset begins every game, but without dice.</summary>
    public static ActionDescriptor WithoutDice() => SetUp(new GameChoices(AreDiceOff: true))!;

    /// <summary>
    /// The first action of a game begun as its ruleset begins every game, but as its creator set it
    /// (<paramref name="choices"/>). None, if they set nothing, for a game begun as every game is.
    /// </summary>
    public static ActionDescriptor? SetUp([Required] GameChoices choices) =>
        choices.IsNone
            ? null
            : Chosen(new ActionDescriptor(SetupKind, ImmutableSortedDictionary<string, string>.Empty), choices);

    /// <summary>
    /// The game's first action, dealing it as <paramref name="deal"/> did, but set up as <paramref name="choices"/>
    /// say, whatever the game it dealt chose: without dice, or with them; ending after so many idle rounds, if set;
    /// opened by the seat they name, if any, without dice; lost by a team with no leader left, if they say so; and
    /// each player's turn passing after so many seconds idle, if set.
    /// </summary>
    public static ActionDescriptor Chosen([Required] ActionDescriptor deal, [Required] GameChoices choices) =>
        deal with
        {
            Arguments = Set(
                Set(
                    Set(
                        Set(
                            Set(
                                deal.Arguments,
                                DiceOffArgument,
                                choices.AreDiceOff ? ActionDescriptor.Flag(true) : null
                            ),
                            IdleRoundsArgument,
                            choices.IdleRounds?.ToString(CultureInfo.InvariantCulture)
                        ),
                        OpenerArgument,
                        choices.AreDiceOff ? choices.Opener?.ToString(CultureInfo.InvariantCulture) : null
                    ),
                    LeadersArgument,
                    choices.IsLostWithoutLeaders ? ActionDescriptor.Flag(true) : null
                ),
                IdleTimeArgument,
                choices.IdleSeconds?.ToString(CultureInfo.InvariantCulture)
            ),
        };

    /// <summary>
    /// How long a player may be idle, deciding, in the game the first of <paramref name="actions"/> began, before
    /// their turn passes, if its creator set it; none, if not.
    /// </summary>
    public static TimeSpan? IdleTimeOf([Required] ImmutableList<ActionDescriptor> actions) =>
        actions.FirstOrDefault() is { Kind: DealKind or SetupKind } first
        && first.Arguments.TryGetValue(IdleTimeArgument, out string? seconds)
            ? TimeSpan.FromSeconds(int.Parse(seconds, CultureInfo.InvariantCulture))
            : null;

    /// <summary>Whether the first of <paramref name="actions"/> began a game a leaderless team loses.</summary>
    public static bool IsLostWithoutLeaders([Required] ImmutableList<ActionDescriptor> actions) =>
        actions.FirstOrDefault() is { Kind: DealKind or SetupKind } first
        && first.Arguments.ContainsKey(LeadersArgument);

    /// <summary>
    /// The arguments with <paramref name="name"/> set to <paramref name="value"/>, or without it, if none.
    /// </summary>
    private static ImmutableSortedDictionary<string, string> Set(
        ImmutableSortedDictionary<string, string> arguments,
        string name,
        string? value
    ) =>
        value is null ? arguments.Remove(name) : arguments.SetItem(name, value);

    /// <summary>
    /// The seat that opens the game the first of <paramref name="actions"/> began without dice, if it says.
    /// </summary>
    public static int? OpenerOf([Required] ImmutableList<ActionDescriptor> actions) =>
        actions.FirstOrDefault() is { Kind: DealKind or SetupKind } first
        && first.Arguments.TryGetValue(OpenerArgument, out string? seat)
            ? int.Parse(seat, CultureInfo.InvariantCulture)
            : null;

    /// <summary>Whether the first of <paramref name="actions"/> began the game without dice.</summary>
    public static bool AreDiceOff([Required] ImmutableList<ActionDescriptor> actions) =>
        actions.FirstOrDefault() is { Kind: DealKind or SetupKind } first
        && first.Arguments.ContainsKey(DiceOffArgument);

    /// <summary>
    /// How many idle rounds end the game the first of <paramref name="actions"/> began, if it set them.
    /// </summary>
    public static int? IdleRoundsOf([Required] ImmutableList<ActionDescriptor> actions) =>
        actions.FirstOrDefault() is { Kind: DealKind or SetupKind } first
        && first.Arguments.TryGetValue(IdleRoundsArgument, out string? rounds)
            ? int.Parse(rounds, CultureInfo.InvariantCulture)
            : null;

    /// <summary>
    /// The first action of a game dealt by a seed <paramref name="dice"/> roll, its terrain and palaces too: every army
    /// alike, if <paramref name="areAlike"/>, which a game dealt before armies could be alike never says; nor does one
    /// dealt before terrain was say that its terrain is, nor one dealt before palaces were that its palaces are, nor
    /// one dealt before they could be that its armies may hold the pieces a palace holds, nor one dealt before they
    /// could that a palace may stand partly outside a kingdom too small for it; and the edition of the pieces it is
    /// dealt from (<see cref="Edition"/>), which one dealt before there was another never says. Its
    /// armies drafted by its players, not dealt, if <paramref name="drafting"/> says how
    /// (<see cref="IRulesApplying.Drafted"/>): its budget and its one drafter, if it sets them. On a board of
    /// <paramref name="board"/>'s size, if its creator set one (<see cref="IRulesApplying.Sized"/>). Its pieces
    /// promoted at the far edge promoted at the edge to their right too, if <paramref name="promotesAtSide"/>
    /// (<see cref="GameState.PromotesAtSide"/>). Its armies dealt, not drafted, each spending
    /// <paramref name="budget"/>, if its creator set one. Its terrain, as far as they chose it
    /// (<paramref name="terrain"/>), the rest dealt. Each seat's army of the family of pieces
    /// <paramref name="families"/> names for it, by seat, if any (a bot's own). Set up otherwise as
    /// <paramref name="choices"/> say, if any.
    /// </summary>
    public static ActionDescriptor Dealt(
        [Required] IDiceRolling dice,
        bool areAlike = false,
        DraftSettings? drafting = null,
        BoardSize? board = null,
        bool promotesAtSide = false,
        int? budget = null,
        GameChoices? choices = null,
        TerrainChoice? terrain = null,
        ImmutableList<string?>? families = null
    ) =>
        Chosen(
            Familied(
                Terrained(DealtWithDice(dice, areAlike, drafting, board, promotesAtSide, budget), terrain),
                families
            ),
            choices ?? GameChoices.None
        );

    /// <summary>The deal, each seat's family told, by seat, "-" for none, if any seat has one.</summary>
    private static ActionDescriptor Familied(ActionDescriptor deal, ImmutableList<string?>? families) =>
        families is { } named && named.Any(family => family is not null)
            ? deal with
            {
                Arguments = deal.Arguments.Add(
                    FamiliesArgument,
                    string.Join(',', named.Select(family => family ?? NoFamily))
                ),
            }
            : deal;

    private const string NoFamily = "-";

    /// <summary>Each seat's family of pieces, as the first of <paramref name="actions"/> dealt it, if any.</summary>
    public static ImmutableList<string?>? FamiliesOf([Required] ImmutableList<ActionDescriptor> actions) =>
        actions.FirstOrDefault() is { Kind: DealKind } first
        && first.Arguments.TryGetValue(FamiliesArgument, out string? families)
            ? [.. families.Split(',').Select(family => family == NoFamily ? null : family)]
            : null;

    /// <summary>The deal, its terrain set as far as <paramref name="terrain"/> chose it, if it chose any.</summary>
    private static ActionDescriptor Terrained(ActionDescriptor deal, TerrainChoice? terrain) =>
        terrain is { Mask: not 0 } chosen
            ? deal with
            {
                Arguments = deal.Arguments.Add(
                    ChosenTerrainArgument,
                    string.Create(CultureInfo.InvariantCulture, $"{chosen.Mask}:{chosen.Value & chosen.Mask}")
                ),
            }
            : deal;

    private static ActionDescriptor DealtWithDice(
        IDiceRolling dice,
        bool areAlike,
        DraftSettings? drafting,
        BoardSize? board,
        bool promotesAtSide,
        int? budget
    )
    {
        ImmutableSortedDictionary<string, string> seeded = ImmutableSortedDictionary<string, string>.Empty
            .Add(SeedArgument, dice.Roll(SeedRoll)[0].ToString(CultureInfo.InvariantCulture))
            .Add(TerrainArgument, ActionDescriptor.Flag(true))
            .Add(PalacesArgument, ActionDescriptor.Flag(true))
            .Add(PalacePiecesArgument, ActionDescriptor.Flag(true))
            .Add(EdgePalacesArgument, ActionDescriptor.Flag(true))
            .Add(EditionArgument, Edition.ToString(CultureInfo.InvariantCulture));
        ImmutableSortedDictionary<string, string> alike =
            areAlike ? seeded.Add(AlikeArgument, ActionDescriptor.Flag(true)) : seeded;
        ImmutableSortedDictionary<string, string> sided =
            promotesAtSide ? alike.Add(SidePromotionArgument, ActionDescriptor.Flag(true)) : alike;
        ImmutableSortedDictionary<string, string> sized = board is null ? sided : Sizing(sided, board);
        return new ActionDescriptor(
            DealKind,
            (drafting, budget) switch
            {
                ({ } drafted, _) => Drafting(sized, drafted),
                (null, { } spent) => sized.Add(ArmyBudgetArgument, spent.ToString(CultureInfo.InvariantCulture)),
                _ => sized,
            }
        );
    }

    /// <summary>
    /// What of its terrain the creator of the game the first of <paramref name="actions"/> dealt chose, if any.
    /// </summary>
    public static TerrainChoice? TerrainOf([Required] ImmutableList<ActionDescriptor> actions) =>
        actions.FirstOrDefault() is { Kind: DealKind } first
        && first.Arguments.TryGetValue(ChosenTerrainArgument, out string? chosen)
            ? new TerrainChoice(
                int.Parse(chosen.Split(':')[0], CultureInfo.InvariantCulture),
                int.Parse(chosen.Split(':')[1], CultureInfo.InvariantCulture)
            )
            : null;

    /// <summary>
    /// What each army of the game the first of <paramref name="actions"/> dealt has to spend on its pieces, by
    /// <paramref name="rules"/>: the draft's budget, or the deal's, as its creator set it, else the ruleset's at the
    /// board's size; none, for a game not dealt, or begun from a scenario.
    /// </summary>
    public static int? SpentOf([Required] ImmutableList<ActionDescriptor> actions, [Required] IRulesApplying rules) =>
        actions.FirstOrDefault() is { Kind: DealKind }
            ? DraftingOf(actions)?.Budget ?? ArmyBudgetOf(actions) ?? RulesOf(actions, rules).DraftBudget
            : null;

    /// <summary>What each army dealt by the first of <paramref name="actions"/> spends, if set.</summary>
    public static int? ArmyBudgetOf([Required] ImmutableList<ActionDescriptor> actions) =>
        actions.FirstOrDefault() is { Kind: DealKind } first
        && first.Arguments.TryGetValue(ArmyBudgetArgument, out string? budget)
            ? int.Parse(budget, CultureInfo.InvariantCulture)
            : null;

    private static ImmutableSortedDictionary<string, string> Sizing(
        ImmutableSortedDictionary<string, string> arguments,
        BoardSize board
    )
    {
        ImmutableSortedDictionary<string, string> sized = arguments
            .Add(SideArgument, board.Width.ToString(CultureInfo.InvariantCulture))
            .Add(KingdomWidthArgument, board.KingdomWidth.ToString(CultureInfo.InvariantCulture))
            .Add(KingdomDepthArgument, board.KingdomDepth.ToString(CultureInfo.InvariantCulture));
        return board.Height == board.Width
            ? sized
            : sized.Add(HeightArgument, board.Height.ToString(CultureInfo.InvariantCulture));
    }

    /// <summary>
    /// The board's size the first of <paramref name="actions"/> set; none, if it set none. Its side is its width, and
    /// its height too, unless it says another (as a game set before boards could be other than square never does).
    /// </summary>
    public static BoardSize? BoardOf([Required] ImmutableList<ActionDescriptor> actions) =>
        actions.FirstOrDefault() is { Kind: DealKind } first
        && first.Arguments.TryGetValue(SideArgument, out string? side)
            ? new BoardSize(
                int.Parse(side, CultureInfo.InvariantCulture),
                int.Parse(first.Arguments.GetValueOrDefault(HeightArgument, side), CultureInfo.InvariantCulture),
                int.Parse(first.Arguments[KingdomWidthArgument], CultureInfo.InvariantCulture),
                int.Parse(first.Arguments[KingdomDepthArgument], CultureInfo.InvariantCulture)
            )
            : null;

    /// <summary>
    /// The rules a game of these actions is played by: <paramref name="rules"/> on the board's size its first set, if
    /// any; else as they are.
    /// </summary>
    /// <exception cref="ArgumentException">The rules may not be played at that size.</exception>
    public static IRulesApplying RulesOf(
        [Required] ImmutableList<ActionDescriptor> actions,
        [Required] IRulesApplying rules
    ) =>
        BoardOf(actions) is { } board
            ? rules.Sized(board)
                ?? throw new ArgumentException($"{rules.Name} is not played on {board}.", nameof(actions))
            : rules;

    private static ImmutableSortedDictionary<string, string> Drafting(
        ImmutableSortedDictionary<string, string> arguments,
        DraftSettings drafting
    )
    {
        ImmutableSortedDictionary<string, string> drafted = arguments.Add(DraftedArgument, ActionDescriptor.Flag(true));
        ImmutableSortedDictionary<string, string> budgeted = drafting.Budget is { } budget
            ? drafted.Add(BudgetArgument, budget.ToString(CultureInfo.InvariantCulture))
            : drafted;
        ImmutableSortedDictionary<string, string> secret = drafting switch
        {
            { IsSecret: true, IsHiddenFromAllies: true } => budgeted
                .Add(SecretArgument, ActionDescriptor.Flag(true))
                .Add(HiddenFromAlliesArgument, ActionDescriptor.Flag(true)),
            { IsSecret: true } => budgeted.Add(SecretArgument, ActionDescriptor.Flag(true)),
            _ => budgeted,
        };
        return drafting switch
        {
            { Drafter: { } drafter } => secret.Add(
                DrafterArgument,
                drafter.Value.ToString(CultureInfo.InvariantCulture)
            ),
            { IsSimultaneous: true } => secret.Add(AllAtOnceArgument, ActionDescriptor.Flag(true)),
            _ => secret,
        };
    }

    /// <summary>Whether the first of <paramref name="actions"/> had the armies drafted by the players.</summary>
    public static bool IsDrafted([Required] ImmutableList<ActionDescriptor> actions) =>
        DraftingOf(actions) is not null;

    /// <summary>
    /// How the first of <paramref name="actions"/> had the armies drafted, its budget and its one drafter, if it set
    /// them, whether in secret, from allies too, and whether all at once (a game begun before drafting could be never
    /// says so, and was drafted in turn); none, if they were not drafted.
    /// </summary>
    public static DraftSettings? DraftingOf([Required] ImmutableList<ActionDescriptor> actions) =>
        actions.FirstOrDefault() is { Kind: DealKind } first && first.Arguments.ContainsKey(DraftedArgument)
            ? new DraftSettings(
                first.Arguments.TryGetValue(BudgetArgument, out string? budget)
                    ? int.Parse(budget, CultureInfo.InvariantCulture)
                    : null,
                first.Arguments.TryGetValue(DrafterArgument, out string? drafter)
                    ? new SeatId(int.Parse(drafter, CultureInfo.InvariantCulture))
                    : null
            )
            {
                IsSecret = first.Arguments.ContainsKey(SecretArgument),
                IsHiddenFromAllies = first.Arguments.ContainsKey(HiddenFromAlliesArgument),
                IsSimultaneous = first.Arguments.ContainsKey(AllAtOnceArgument),
            }
            : null;

    /// <summary>Whether the first of <paramref name="actions"/> dealt every army alike.</summary>
    public static bool AreAlike([Required] ImmutableList<ActionDescriptor> actions) =>
        actions.FirstOrDefault() is { Kind: DealKind } first && first.Arguments.ContainsKey(AlikeArgument);

    /// <summary>
    /// The id of the scenario the first of <paramref name="actions"/> starts from, so that it may be played again;
    /// null if it starts from none, or from one set up before scenarios had ids.
    /// </summary>
    public static string? ScenarioIdOf([Required] ImmutableList<ActionDescriptor> actions) =>
        actions.FirstOrDefault() is { Kind: Kind } first && ScenarioOf(first).Id is { Length: > 0 } id ? id : null;

    /// <summary>
    /// Whether the first of <paramref name="actions"/> starts the game from a scenario, with an id or not.
    /// </summary>
    public static bool IsScenario([Required] ImmutableList<ActionDescriptor> actions) =>
        actions.FirstOrDefault() is { Kind: Kind };

    public static ActionDescriptor Describe([Required] Scenario scenario) =>
        new(
            Kind,
            ImmutableSortedDictionary<string, string>.Empty
                .Add(Argument, JsonSerializer.Serialize(scenario, JsonSerializerOptions.Web))
        );

    /// <summary>
    /// Where a game of these actions starts: as the scenario its first carries, if any; as dealt by the seed it
    /// carries, if it is dealt; else as new; without dice, if it says so.
    /// </summary>
    public static GameState StateOf(
        [Required] ImmutableList<ActionDescriptor> actions,
        [Required] IRulesApplying rules
    )
    {
        GameState state = actions.FirstOrDefault() switch
        {
            { Kind: Kind } first => StateFrom(ScenarioOf(first), rules),
            { Kind: DealKind } first => DealtBy(
                rules,
                int.Parse(first.Arguments[SeedArgument], CultureInfo.InvariantCulture),
                actions
            ),
            _ => rules.CreateInitialState(),
        };
        GameState idled = IdleRoundsOf(actions) is { } rounds ? state with { IdleRounds = rounds } : state;
        GameState opened = OpenerOf(actions) is { } opener ? idled with { Opener = new SeatId(opener) } : idled;
        GameState led = opened with { IsLostWithoutLeaders = IsLostWithoutLeaders(actions) };
        return AreDiceOff(actions) ? rules.WithoutDice(led) : led;
    }

    /// <summary>
    /// The game dealt by <paramref name="seed"/>, its armies then drafted by its players, if so begun.
    /// </summary>
    private static GameState DealtBy(IRulesApplying rules, int seed, ImmutableList<ActionDescriptor> actions)
    {
        GameState dealt = Fresh(rules, seed, actions);
        GameState kept = actions[0].Arguments.TryGetValue(KeptArgument, out string? deal)
            ? KeptDeal.Parsed(deal).Applied(dealt)
            : dealt;
        return DraftingOf(actions) is { } drafting ? rules.Drafted(kept, seed, drafting) : kept;
    }

    /// <summary>
    /// The deal <paramref name="deal"/> began, with what it dealt kept in it (<see cref="KeptDeal"/>), so that the game
    /// replays as it was dealt however dealing changes; as it is, if it is no deal, or keeps its deal already.
    /// </summary>
    public static ActionDescriptor Kept([Required] ActionDescriptor deal, [Required] IRulesApplying rules) =>
        deal.Kind == DealKind && !deal.Arguments.ContainsKey(KeptArgument)
            ? deal with
            {
                Arguments = deal.Arguments.Add(
                    KeptArgument,
                    KeptDeal.Of(
                        Fresh(rules, int.Parse(deal.Arguments[SeedArgument], CultureInfo.InvariantCulture), [deal])
                    ).Serialized()
                ),
            }
            : deal;

    /// <summary>Whether the game the first of <paramref name="actions"/> dealt keeps what it dealt.</summary>
    public static bool IsKept([Required] ImmutableList<ActionDescriptor> actions) =>
        actions.FirstOrDefault() is not { Kind: DealKind } first || first.Arguments.ContainsKey(KeptArgument);

    /// <summary>The game as <paramref name="seed"/> deals it now, before any draft.</summary>
    private static GameState Fresh(IRulesApplying rules, int seed, ImmutableList<ActionDescriptor> actions) =>
        rules.Deal(
            rules.CreateInitialState(),
            seed,
            AreAlike(actions),
            actions[0].Arguments.ContainsKey(TerrainArgument),
            actions[0].Arguments.ContainsKey(PalacesArgument),
            actions[0].Arguments.ContainsKey(PalacePiecesArgument),
            actions[0].Arguments.ContainsKey(EdgePalacesArgument),
            actions[0].Arguments.TryGetValue(EditionArgument, out string? edition)
                ? int.Parse(edition, CultureInfo.InvariantCulture)
                : 0,
            ArmyBudgetOf(actions),
            TerrainOf(actions),
            FamiliesOf(actions)
        ) with
        {
            PromotesAtSide = PromotesAtSide(actions),
        };

    /// <summary>
    /// Whether the first of <paramref name="actions"/> had pieces promoted at the far edge promoted at the edge to
    /// their right too.
    /// </summary>
    public static bool PromotesAtSide([Required] ImmutableList<ActionDescriptor> actions) =>
        actions.FirstOrDefault() is { Kind: DealKind } first && first.Arguments.ContainsKey(SidePromotionArgument);

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
            Edition = scenario.Edition,
            PromotesAtSide = scenario.PromotesAtSide,
            Palaces = scenario.Palaces.IsEmpty
                ? initial.Palaces
                : scenario.Palaces.ToImmutableDictionary(
                    palace => names.SeatOf(palace.Key),
                    palace => PalaceAbout(rules.Topology, names.CellOf(palace.Value))
                ),
        };
    }

    /// <summary>The cell <paramref name="centre"/> and those about it on the board: a palace about it.</summary>
    private static ImmutableList<CellId> PalaceAbout(ICellNavigating topology, CellId centre) =>
    [
        .. Enumerable.Range(-1, 3)
            .SelectMany(rows => Enumerable.Range(-1, 3).Select(columns => new Direction(columns, rows)))
            .Select(way => way == new Direction(0, 0) ? centre : topology.Step(centre, way))
            .OfType<CellId>(),
    ];

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
