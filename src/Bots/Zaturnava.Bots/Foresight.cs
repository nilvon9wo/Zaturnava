using System.Collections.Immutable;
using Net.NowhereAtAll.Zaturnava.Engine.Actions;
using Net.NowhereAtAll.Zaturnava.Engine.Chance;
using Net.NowhereAtAll.Zaturnava.Engine.Decisions;
using Net.NowhereAtAll.Zaturnava.Engine.Rules;

namespace Net.NowhereAtAll.Zaturnava.Bots;

/// <summary>
/// Play that looks ahead through the rest of the bot's own turn, as far as <paramref name="depth"/> of its
/// decisions: each action is worth the best the bot could make of where it leads, and where it waits on the
/// dice, the average of every way they could fall, each as likely as it is. So a roll is worth what the bot
/// would expect to make of it, against what a deliberate move makes for certain. Where the turn passes, or
/// the look ahead ends, the position is worth what <paramref name="valuing"/> says (Sage's
/// <see cref="Appraisal"/>, say), the enemy's reply weighed only as that counts it. To keep it quick, only
/// the <paramref name="beam"/> most promising actions at each step are followed further. Among actions
/// worth the same, it would rather do something than pass, and picks at random.
/// </summary>
internal sealed class Foresight(IPositionValuing valuing, int depth, int beam)
{
    private const double Tolerance = 1e-9;

    private readonly IPositionValuing _valuing = valuing;
    private readonly int _depth = depth;
    private readonly int _beam = beam;

    public GameAction Choose(DecisionRequest request, Random random)
    {
        ImmutableList<(GameAction Action, double Worth)> worths =
        [
            .. Playable.Of(request).Select(action => (
                action,
                this.WorthOf(request, Apply(request, request.State, action), this._depth - 1)
            )),
        ];
        double best = worths.Max(worth => worth.Worth);
        ImmutableList<GameAction> top =
            [.. worths.Where(worth => worth.Worth >= best - Tolerance).Select(worth => worth.Action)];
        ImmutableList<GameAction> active = [.. top.Where(action => action is not PassAction)];
        ImmutableList<GameAction> choices = active.IsEmpty
            ? top
            : active;
        return choices[random.Next(choices.Count)];
    }

    /// <summary>Each way the dice could fall, its values in order, with how likely it is.</summary>
    internal static ImmutableList<(ImmutableList<int> Values, double Chance)> Outcomes(DiceRequest dice)
    {
        IEnumerable<ImmutableList<int>> rolls = Enumerable
            .Range(0, dice.Count)
            .Aggregate(
                (IEnumerable<ImmutableList<int>>)[[]],
                (partial, _) => partial.SelectMany(values => Enumerable.Range(1, dice.Sides).Select(values.Add))
            );
        double each = 1 / Math.Pow(dice.Sides, dice.Count);
        return
        [
            .. rolls
                .Select(values => values.Sort())
                .GroupBy(values => string.Join(',', values))
                .Select(same => (same.First(), same.Count() * each)),
        ];
    }

    /// <summary>
    /// What the state is worth to the bot: once its turn has passed (or the game is over), its value;
    /// the average over the dice, if it waits on the bot's; else the best the bot could make of it.
    /// </summary>
    private double WorthOf(DecisionRequest request, GameState state, int stepsLeft) =>
        (state.DecidingSeat == request.Seat, state.AwaitedDice) switch
        {
            (false, _) => this._valuing.ValueOf(request, state),
            (_, { } dice) => Outcomes(dice).Sum(outcome =>
                outcome.Chance
                * this.WorthOf(request, Apply(request, state, new DiceAction(request.Seat, outcome.Values)), stepsLeft)
            ),
            _ => this.BestOf(request, state, stepsLeft),
        };

    /// <summary>
    /// The best the bot could make of the state, stopping there or taking any of its next actions, each
    /// worth its value - but the most promising followed further, while steps are left.
    /// </summary>
    private double BestOf(DecisionRequest request, GameState state, int stepsLeft)
    {
        DecisionRequest now = request with { State = state, LegalActions = request.Rules.LegalActions(state) };
        ImmutableList<(GameState After, double Worth)> next =
        [
            .. Playable.Of(now)
                .Select(action => Apply(request, state, action))
                .Select(after => (after, this._valuing.ValueOf(request, after)))
                .OrderByDescending(step => step.Item2),
        ];
        IEnumerable<double> further = stepsLeft > 1
            ? next.Take(this._beam).Select(step => this.WorthOf(request, step.After, stepsLeft - 1))
            : [];
        return next
            .Select(step => step.Worth)
            .Concat(further)
            .Append(this._valuing.ValueOf(request, state))
            .Max();
    }

    private static GameState Apply(DecisionRequest request, GameState state, GameAction action) =>
        request.Rules.Apply(state, action).State;
}
