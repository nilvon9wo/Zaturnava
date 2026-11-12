using System.ComponentModel.DataAnnotations;
using Net.NowhereAtAll.Zaturnava.Engine.Actions;
using Net.NowhereAtAll.Zaturnava.Engine.Decisions;
using Net.NowhereAtAll.Zaturnava.Engine.Rules;

namespace Net.NowhereAtAll.Zaturnava.Application.Running;

/// <summary>
/// A bot that, while the armies are drafted, drafts its own as the army filled at random would be (Proposed: until
/// each bot drafts its own way, as the roadmap's Midas by value, Solomon evenly), a step at a time, as a person
/// does: its throne where that army's would be; then, a turn at a time, the dearest piece of that army, on its square,
/// or a dearer one there, should the budget leave more than that army spends; done once that army has nothing more to
/// add. It plays as itself once play begins.
/// </summary>
public sealed class FillingArmies([Required] IDeciding bot) : IDeciding
{
    private readonly IDeciding _bot = bot;

    public Task<GameAction> Decide([Required] DecisionRequest request, CancellationToken cancellationToken) =>
        request.LegalActions.OfType<FillArmyAction>().FirstOrDefault() is { } fill
            ? Task.FromResult(Drafted(request, fill))
            : this._bot.Decide(request, cancellationToken);

    public Task<GameAction?> Seize([Required] DecisionRequest request, CancellationToken cancellationToken) =>
        this._bot.Seize(request, cancellationToken);

    /// <summary>
    /// The next step toward the army <paramref name="fill"/> would make: its throne; else the first piece offered
    /// (the dearest) that it would place where it is offered, made better (<see cref="Bettered"/>); else, if it would
    /// add nothing, being done; else, should the rules offer none of what it would add, the fill itself.
    /// </summary>
    private static GameAction Drafted(DecisionRequest request, FillArmyAction fill)
    {
        GameState now = request.State;
        GameState filled = request.Rules.Apply(now, fill).State;
        bool isAdded = filled.Position.Occupants.Count > now.Position.Occupants.Count;
        GameAction? throne = request.LegalActions.OfType<ThroneAction>()
            .FirstOrDefault(action => filled.Thrones![request.Seat] == action.Cell);
        DraftAction? piece = request.LegalActions.OfType<DraftAction>()
            .FirstOrDefault(action =>
                filled.Position.PieceAt(action.Cell) is { } placed
                && placed.Kind == action.Piece
                && placed.Owner == request.Seat
            );
        return (throne ?? Bettered(request, filled, piece), isAdded) switch
        {
            ({ } next, _) => next,
            (_, false) => request.LegalActions.OfType<PassAction>().First(),
            _ => fill,
        };
    }

    /// <summary>
    /// The <paramref name="piece"/> the fill would buy, or, on any square but the throne (its leader's), the dearest
    /// kind offered there that the budget can pay for, keeping enough for the rest of the fill's army: what is left
    /// over, a person spends on stronger pieces, not keeps, nor spreads over cheap ones (Proposed; the user's report
    /// of 2026-11-12, of bots buying cheap pieces with a large budget).
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
        return request.LegalActions.OfType<DraftAction>()
            .Where(action => action.Cell == piece.Cell && rules.WorthOf(action.Piece) <= most)
            .MaxBy(action => rules.WorthOf(action.Piece));
    }
}
