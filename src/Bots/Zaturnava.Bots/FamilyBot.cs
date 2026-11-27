using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;
using Net.NowhereAtAll.Zaturnava.Engine.Actions;
using Net.NowhereAtAll.Zaturnava.Engine.Decisions;
using Net.NowhereAtAll.Zaturnava.Engine.Pieces;
using Net.NowhereAtAll.Zaturnava.Engine.Rules;

namespace Net.NowhereAtAll.Zaturnava.Bots;

/// <summary>
/// A strong bot with a family of its own (the specification's D4: strong Grand Legions bots by family, each
/// preferring its game's pieces while playing well): it plays as Sage does, but its army is of its family's game's
/// pieces only (<see cref="PieceGuide.IsOf"/>: its own, and those it plays under other names, as Makruk the Western
/// Rook), none of a class its game has none of (the user's word of 2026-11-20: "Western Chess doesn't have artilary
/// ... This game should reflect that"). Drafting, it buys only those, and is done once it can buy none; it never has
/// its army filled at random, which might bring it others. In a game whose armies are dealt, its army is dealt so
/// (<see cref="BotProfile.Family"/>), unless every army is alike, a game it is not offered for.
/// </summary>
public sealed class FamilyBot : IBotPlaying
{
    private readonly SageBot _sage;
    private readonly string _family;

    private FamilyBot(BotProfile profile, string family, Random random)
    {
        this.Profile = profile;
        this._family = family;
        this._sage = new SageBot(random);
    }

    public BotProfile Profile { get; }

    /// <summary>One bot of each family, as the specification names them: a figure of each game's own lore.</summary>
    public static ImmutableList<FamilyBot> All([Required] Random random) =>
    [
        .. Families.Select(family =>
            new FamilyBot(
                new BotProfile(
                    family.Id,
                    family.Name,
                    $"A {family.Family} purist: drafts its army only of {family.Family} pieces, then plays "
                        + "thoughtfully, weighing the whole board (points, pieces, leaders, prisoners, thrones, what "
                        + "could be taken next) and planning the rest of its turn, dice included. As an ally or an "
                        + $"enemy: as strong as its {family.Family} pieces are in this game. A plain algorithm: no AI.",
                    UsesAi: false
                )
                {
                    Family = family.Family,
                },
                family.Family,
                random
            )
        ),
    ];

    /// <summary>
    /// Each family, its bot's id and name (Proposed, 2026-11-20): Othello's, Desdemona, of the play the game is named
    /// for, its army all Evangelists (the user's word of 2026-11-20).
    /// </summary>
    internal static ImmutableList<(string Family, string Id, string Name)> Families { get; } =
    [
        ("Western", "philidor", "Philidor"),
        ("Xiangqi", "sun-tzu", "Sun Tzu"),
        ("Janggi", "yi-sun-sin", "Yi Sun-sin"),
        ("Shogi", "musashi", "Musashi"),
        ("Makruk", "naresuan", "Naresuan"),
        ("Checkers", "palamedes", "Palamedes"),
        ("Tafl", "ragnar", "Ragnar"),
        ("Fairy", "oberon", "Oberon"),
        ("Othello", "desdemona", "Desdemona"),
    ];

    /// <summary>
    /// Whether it plays the ruleset: only one whose armies may be drafted, and of pieces of its family, as Grand
    /// Legions'; not Chaturaji 2027, say, where no Shogi or Tafl piece is to be had (the user's word of 2026-11-20:
    /// "Selecting bots should be limited to the bots which make sense for the game!").
    /// </summary>
    public bool Knows([Required] IRulesApplying rules) => rules.IsDraftable && FamiliesOf(rules).Contains(this._family);

    /// <summary>
    /// The families of the ruleset's pieces, found once for each ruleset: a piece's guide draws its moves, too slow to
    /// ask of every piece each time the bots are listed.
    /// </summary>
    private static ImmutableHashSet<string> FamiliesOf(IRulesApplying rules) =>
        Found.GetValue(
            rules,
            each =>
            [
                .. each.Kinds.Select(each.GuideOf)
                    .SelectMany(guide => guide.AlsoIn.Prepend(guide.Origin))
                    .OfType<string>(),
            ]
        );

    private static readonly ConditionalWeakTable<IRulesApplying, ImmutableHashSet<string>> Found = [];

    public Task<GameAction?> Seize([Required] DecisionRequest request, CancellationToken cancellationToken) =>
        this._sage.Seize(request, cancellationToken);

    public Task<GameAction> Decide([Required] DecisionRequest request, CancellationToken cancellationToken) =>
        this._sage.Decide(this.Narrowed(request), cancellationToken);

    /// <summary>
    /// The request, its army kept to its family's pieces: drafting, never a piece of another's bought, nor the army
    /// filled at random. Buying, only its family's pieces; once it can buy none of them, only being done - never
    /// taking back what it drafted, nor moving its throne, which a draft all at once allows and Sage might do
    /// without end. Placing its palace or throne, as Sage would, but never filling at random. As is, out of the
    /// draft, or if that would leave it nothing to do.
    /// </summary>
    private DecisionRequest Narrowed(DecisionRequest request)
    {
        ImmutableList<GameAction> legal = request.LegalActions;
        ImmutableList<GameAction> own =
        [
            .. legal.OfType<DraftAction>().Where(draft => request.Rules.GuideOf(draft.Piece).IsOf(this._family)),
        ];
        bool isDrafting = legal.Any(action => action is FillArmyAction);
        bool isBuying = legal.Any(action => action is DraftAction or PassAction);
        ImmutableList<GameAction> kept = (own.IsEmpty, isDrafting, isBuying) switch
        {
            (false, _, _) => own,
            (_, true, true) => [.. legal.OfType<PassAction>()],
            (_, true, false) => [.. legal.Where(action => action is not FillArmyAction)],
            _ => legal,
        };
        return kept.IsEmpty ? request : request with { LegalActions = kept };
    }
}
