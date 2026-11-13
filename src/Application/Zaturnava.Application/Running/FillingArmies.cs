using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using Net.NowhereAtAll.Zaturnava.Engine.Actions;
using Net.NowhereAtAll.Zaturnava.Engine.Decisions;
using Net.NowhereAtAll.Zaturnava.Engine.Pieces;
using Net.NowhereAtAll.Zaturnava.Engine.Rules;

namespace Net.NowhereAtAll.Zaturnava.Application.Running;

/// <summary>
/// A bot that, while the armies are drafted, drafts its own as the army filled at random would be (Proposed: until
/// each bot drafts its own way, as the roadmap's Midas by value, Solomon evenly), a step at a time, as a person
/// does: its palace and throne where that army's would be; then, a turn at a time, the dearest piece of that army, on
/// its square, or a stronger one there that the budget still pays for; done once that army has nothing more to add.
/// It plays as itself once play begins.
/// </summary>
public sealed class FillingArmies([Required] IDeciding bot) : IDeciding
{
    /// <summary>
    /// How strong, as a percentage of the strongest piece it can pay for, a piece must be for a bot to draft it in
    /// place of the fill's (Proposed).
    /// </summary>
    public const int NearlyStrongest = 75;

    private readonly IDeciding _bot = bot;

    public Task<GameAction> Decide([Required] DecisionRequest request, CancellationToken cancellationToken) =>
        request.LegalActions.OfType<FillArmyAction>().FirstOrDefault() is { } fill
            ? Task.FromResult(Drafted(request, fill))
            : this._bot.Decide(request, cancellationToken);

    /// <summary>
    /// While the armies are drafted all at once, its next step, whoever's turn it is, as on its own turn
    /// (<see cref="Decide"/>); otherwise whatever chance the bot itself takes.
    /// </summary>
    public Task<GameAction?> Seize([Required] DecisionRequest request, CancellationToken cancellationToken) =>
        request.LegalActions.OfType<FillArmyAction>().FirstOrDefault() is { } fill
            ? Task.FromResult<GameAction?>(Drafted(request, fill))
            : this._bot.Seize(request, cancellationToken);

    /// <summary>
    /// The next step toward the army <paramref name="fill"/> would make: its palace; its throne; else the first piece
    /// offered (the dearest) that it would place where it is offered, made better (<see cref="Bettered"/>); else, if it
    /// would add nothing, being done; else, should the rules offer none of what it would add, the fill itself.
    /// </summary>
    private static GameAction Drafted(DecisionRequest request, FillArmyAction fill)
    {
        GameState now = request.State;
        GameState filled = request.Rules.Apply(now, fill).State;
        bool isAdded = filled.Position.Occupants.Count > now.Position.Occupants.Count;
        GameAction? home = request.LegalActions.FirstOrDefault(action => action switch
        {
            PalaceAction => request.Rules.Apply(now, action).State.Palaces![request.Seat]
                .SequenceEqual(filled.Palaces![request.Seat]),
            ThroneAction throne => filled.Thrones![request.Seat] == throne.Cell,
            _ => false,
        });
        DraftAction? piece = request.LegalActions.OfType<DraftAction>()
            .FirstOrDefault(action =>
                filled.Position.PieceAt(action.Cell) is { } placed
                && placed.Kind == action.Piece
                && placed.Owner == request.Seat
            );
        return (home ?? Bettered(request, filled, piece), isAdded) switch
        {
            ({ } next, _) => next,
            (_, false) => request.LegalActions.OfType<PassAction>().First(),
            _ => fill,
        };
    }

    /// <summary>
    /// The <paramref name="piece"/> the fill would buy, or, on any square but the throne (its leader's), a stronger
    /// one there that the budget can pay for, keeping enough for the rest of the fill's army (Proposed; the user's
    /// reports of 2026-11-12, of bots buying cheap pieces with a large budget, and that the dearest piece is not
    /// always the strongest: a Shogi Pawn is dear for the leader it may become). Strength is what the piece does on
    /// the board, its reach (<see cref="ReachOf"/>), not its price; and, so that armies differ, it is drawn at random
    /// among those it can pay for nearly as strong as the strongest (<see cref="NearlyStrongest"/>).
    /// </summary>
    private static DraftAction? Bettered(DecisionRequest request, GameState filled, DraftAction? piece)
    {
        if (piece is null || request.State.Thrones![request.Seat] == piece.Cell)
        {
            return piece;
        }

        IRulesApplying rules = request.Rules;
        int spent = filled.Position.Occupants
            .Where(entry => entry.Value.Owner == request.Seat && request.State.Position.PieceAt(entry.Key) is null)
            .Sum(entry => rules.WorthOf(entry.Value.Kind));
        int most = rules.WorthOf(piece.Piece) + request.State.Draft!.Left[request.Seat] - spent;
        ImmutableList<(DraftAction Action, int Reach)> affordable =
        [
            .. request.LegalActions.OfType<DraftAction>()
                .Where(action => action.Cell == piece.Cell && rules.WorthOf(action.Piece) <= most)
                .Select(action => (action, ReachOf(rules, action.Piece))),
        ];
        int strongest = affordable.Max(each => each.Reach);
        ImmutableList<DraftAction> strong =
        [
            .. affordable.Where(each => each.Reach * 100 >= strongest * NearlyStrongest).Select(each => each.Action),
        ];
        Random random = new(request.State.Draft.Seed + request.State.Position.Occupants.Count);
        return strong[random.Next(strong.Count)];
    }

    /// <summary>How many squares a <paramref name="kind"/> reaches, to move or capture, as its card draws.</summary>
    private static int ReachOf(IRulesApplying rules, PieceKindId kind)
    {
        MoveDiagram diagram = rules.GuideOf(kind).Diagram;
        return diagram.Moves.Union(diagram.Captures).Count();
    }
}
