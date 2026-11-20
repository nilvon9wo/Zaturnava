using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using Net.NowhereAtAll.Zaturnava.Engine.Actions;
using Net.NowhereAtAll.Zaturnava.Engine.Decisions;

namespace Net.NowhereAtAll.Zaturnava.Bots;

/// <summary>
/// A strong bot with a family of its own (the specification's D4: strong Grand Legions bots by family, each
/// preferring its game's pieces while playing well): it plays as Sage does, but, drafting its army, buys only pieces of
/// its family (<see cref="Engine.Pieces.PieceGuide.Origin"/>) while it can afford any, and only then whatever Sage
/// would. Where its family lacks a piece, it takes the nearest Sage judges best (Proposed, 2026-11-20: no piece is
/// added for it). In a game whose armies are dealt, or whose pieces have no family, it plays as Sage.
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
                    $"Plays as Sage does, but drafts its army of {family.Family} pieces whenever it can. "
                        + "A plain algorithm: no AI.",
                    UsesAi: false
                ),
                family.Family,
                random
            )
        ),
    ];

    /// <summary>Each family, its bot's id and name (Proposed, 2026-11-20).</summary>
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
    ];

    public Task<GameAction?> Seize([Required] DecisionRequest request, CancellationToken cancellationToken) =>
        this._sage.Seize(request, cancellationToken);

    public Task<GameAction> Decide([Required] DecisionRequest request, CancellationToken cancellationToken) =>
        this._sage.Decide(this.Narrowed(request), cancellationToken);

    /// <summary>The request, its drafting narrowed to its family's pieces, if it may draft any; else as is.</summary>
    private DecisionRequest Narrowed(DecisionRequest request)
    {
        ImmutableList<GameAction> own =
        [
            .. request.LegalActions.Where(action =>
                action is DraftAction draft && request.Rules.GuideOf(draft.Piece).Origin == this._family
            ),
        ];
        return own.IsEmpty ? request : request with { LegalActions = own };
    }
}
