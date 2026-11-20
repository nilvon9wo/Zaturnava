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
/// preferring its game's pieces while playing well): it plays as Sage does, but, drafting its army, buys, of each
/// class, only pieces of its family (<see cref="PieceGuide.Origin"/>), if any are offered; else of its kin's, the
/// family most like its own, if any (Proposed, 2026-11-20: no piece is added for it); else whatever Sage would. In a
/// game whose armies are dealt, its army is dealt so (<see cref="BotProfile.Family"/>; the user's report of 2026-11-20:
/// Naresuan dealt every game's pieces), unless every army is alike.
/// </summary>
public sealed class FamilyBot : IBotPlaying
{
    private readonly SageBot _sage;
    private readonly string _family;
    private readonly ImmutableList<string> _nearestFirst;

    private FamilyBot(BotProfile profile, ImmutableList<string> nearestFirst, Random random)
    {
        this.Profile = profile;
        this._family = nearestFirst[0];
        this._nearestFirst = nearestFirst;
        this._sage = new SageBot(random);
    }

    public BotProfile Profile { get; }

    /// <summary>
    /// One bot of each family, as the specification names them: a figure of each game's own lore. Its profile names
    /// its families, nearest first, joined by "+" (<see cref="BotProfile.Family"/>: "Makruk+Western"), its army dealt
    /// of them so.
    /// </summary>
    public static ImmutableList<FamilyBot> All([Required] Random random) =>
    [
        .. Families.Select(family =>
            new FamilyBot(
                new BotProfile(
                    family.Id,
                    family.Name,
                    $"Plays as Sage does, but its army is of {family.Family} pieces wherever it can be. "
                        + "A plain algorithm: no AI.",
                    UsesAi: false
                )
                {
                    Family = string.Join('+', NearestFirst(family)),
                },
                NearestFirst(family),
                random
            )
        ),
    ];

    /// <summary>
    /// Each family, its bot's id and name, and the family whose pieces are most like its own, if any, for a class its
    /// own lacks: Makruk's are Western chess's but for its Pawns and Nobles; Xiangqi's and Janggi's are each other's
    /// (Proposed, 2026-11-20; the user's report: Naresuan's army not of "Makruk or Makruk-like pieces").
    /// </summary>
    internal static ImmutableList<(string Family, string Id, string Name, string? Kin)> Families { get; } =
    [
        ("Western", "philidor", "Philidor", null),
        ("Xiangqi", "sun-tzu", "Sun Tzu", "Janggi"),
        ("Janggi", "yi-sun-sin", "Yi Sun-sin", "Xiangqi"),
        ("Shogi", "musashi", "Musashi", null),
        ("Makruk", "naresuan", "Naresuan", "Western"),
        ("Checkers", "palamedes", "Palamedes", null),
        ("Tafl", "ragnar", "Ragnar", null),
        ("Fairy", "oberon", "Oberon", "Western"),
    ];

    /// <summary>The family's own, then its kin's, if any.</summary>
    private static ImmutableList<string> NearestFirst((string Family, string Id, string Name, string? Kin) family) =>
        family.Kin is { } kin ? [family.Family, kin] : [family.Family];

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
            each => [.. each.Kinds.Select(kind => each.GuideOf(kind).Origin).OfType<string>()]
        );

    private static readonly ConditionalWeakTable<IRulesApplying, ImmutableHashSet<string>> Found = [];

    public Task<GameAction?> Seize([Required] DecisionRequest request, CancellationToken cancellationToken) =>
        this._sage.Seize(request, cancellationToken);

    public Task<GameAction> Decide([Required] DecisionRequest request, CancellationToken cancellationToken) =>
        this._sage.Decide(this.Narrowed(request), cancellationToken);

    /// <summary>
    /// The request, its drafting narrowed, class by class, to the pieces of the nearest of its families that offers
    /// any of the class (<see cref="NearestFirst"/>), as its army is dealt; a class none offers, as is; as is, if it
    /// may draft nothing.
    /// </summary>
    private DecisionRequest Narrowed(DecisionRequest request)
    {
        ImmutableList<(DraftAction Draft, PieceGuide Guide)> drafts =
        [
            .. request.LegalActions.OfType<DraftAction>().Select(draft => (draft, request.Rules.GuideOf(draft.Piece))),
        ];
        ImmutableList<GameAction> own =
        [
            .. drafts.GroupBy(each => each.Guide.Class)
                .SelectMany(byClass =>
                    this._nearestFirst.Select(family => byClass.Where(each => each.Guide.Origin == family).ToList())
                        .FirstOrDefault(found => found.Count > 0) ?? [.. byClass]
                )
                .Select(each => each.Draft),
        ];
        return own.IsEmpty ? request : request with { LegalActions = own };
    }
}
