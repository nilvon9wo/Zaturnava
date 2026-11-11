using System.Collections.Immutable;
using Net.NowhereAtAll.Zaturnava.Engine.Actions;
using Net.NowhereAtAll.Zaturnava.Engine.Decisions;
using Net.NowhereAtAll.Zaturnava.Engine.Rules;
using Net.NowhereAtAll.Zaturnava.Engine.Seats;

namespace Net.NowhereAtAll.Zaturnava.Bots;

/// <summary>
/// Tactician's search (Proposed): a look ahead past the end of the bot's own turn, through every seat's, each
/// mind playing as it would. The bot plays for the best it can make of a position; an enemy, for the worst it
/// can leave the bot (alpha-beta, so that a line already shown worse than another is followed no further); an
/// ally, as it would choose for itself, looking one step ahead, as a mind of its own and not the bot's to
/// command. Where the game waits on the dice, a position is worth the average of every way they could fall,
/// each as likely as it is. It looks one decision further at a time, the best actions first, for as long as
/// <paramref name="budget"/> lasts - each position reached or judged spending one - and plays as the last look
/// it finished says. Past the first step, only the <paramref name="width"/> most promising actions at each
/// decision are followed. A position at the end of the look, or one in which the game is over, is worth what
/// <paramref name="valuing"/> says (Sage's <see cref="Appraisal"/>, say). Among actions worth the same, it
/// would rather do something than pass, and picks at random.
/// </summary>
internal sealed class Tactics(IPositionValuing valuing, int budget, int width)
{
    private const double Tolerance = 1e-9;

    /// <summary>
    /// How many decisions with only one thing to do a line may pass through without the look growing any shorter;
    /// past that, each counts as any other does, so that a line of nothing but passing ends.
    /// </summary>
    private const int MostForced = 32;

    private readonly IPositionValuing _valuing = valuing;
    private readonly int _budget = budget;
    private readonly int _width = width;

    public GameAction Choose(DecisionRequest request, Random random)
    {
        ImmutableList<GameAction> playable = Playable.Of(request);
        return playable.Count == 1
            ? playable[0]
            : this.Searched(request, playable, random);
    }

    private GameAction Searched(DecisionRequest request, ImmutableList<GameAction> playable, Random random)
    {
        Search search = new(request, this._valuing, this._budget, this._width);
        ImmutableList<(GameAction Action, GameState After)> children =
        [
            .. playable.Select(action => (action, search.Apply(request.State, action))),
        ];
        ImmutableList<(GameAction Action, GameState After, double Worth)> worths =
            [.. children.Select(child => (child.Action, child.After, search.Judge(child.After)))];
        bool isCut = children.Any(child => child.After.Outcome is null);
        for (int depth = 2; isCut && !search.IsSpent; depth++)
        {
            ImmutableList<(GameAction Action, GameState After, double Worth)> deeper =
                search.Root([.. worths.OrderByDescending(worth => worth.Worth)], depth);
            worths = deeper.IsEmpty
                ? worths
                : deeper;
            isCut = search.IsCut;
        }

        double best = worths.Max(worth => worth.Worth);
        ImmutableList<GameAction> top =
            [.. worths.Where(worth => worth.Worth >= best - Tolerance).Select(worth => worth.Action)];
        ImmutableList<GameAction> active = [.. top.Where(action => action is not PassAction)];
        ImmutableList<GameAction> choices = active.IsEmpty
            ? top
            : active;
        return choices[random.Next(choices.Count)];
    }

    /// <summary>One decision's search: what it has spent, and whether its last look stopped short anywhere.</summary>
    private sealed class Search(DecisionRequest request, IPositionValuing valuing, int budget, int width)
    {
        /// <summary>The actions not looked into: offers, proposals and votes, playing nothing on the board.</summary>
        private static readonly ImmutableHashSet<Type> NotPlay =
        [
            typeof(OfferAction), typeof(ProposeEndAction), typeof(VoteAwayAction), typeof(PassAwayAction),
            typeof(RejoinAction),
        ];

        private readonly DecisionRequest _request = request;
        private readonly IPositionValuing _valuing = valuing;
        private readonly int _budget = budget;
        private readonly int _width = width;
        private int _spent;
        private int _forced;

        public bool IsSpent => this._spent >= this._budget;

        /// <summary>Whether the last look stopped anywhere the game was not over.</summary>
        public bool IsCut { get; private set; }

        /// <summary>
        /// Each action worth what it leads to, looking <paramref name="depth"/> decisions ahead in all: exactly,
        /// if it is worth as much as the best before it; else no more than it is worth. Should the budget run out,
        /// only the actions looked at in full before it did, in order: the best of the last look first, so that
        /// any found better than it is better at the greater depth.
        /// </summary>
        public ImmutableList<(GameAction Action, GameState After, double Worth)> Root(
            ImmutableList<(GameAction Action, GameState After, double Worth)> ordered,
            int depth
        )
        {
            this.IsCut = false;
            double best = double.NegativeInfinity;
            ImmutableList<(GameAction Action, GameState After, double Worth)>.Builder worths =
                ImmutableList.CreateBuilder<(GameAction Action, GameState After, double Worth)>();
            foreach ((GameAction action, GameState after, _) in ordered)
            {
                double worth = this.Value(after, depth - 1, best - (2 * Tolerance), double.PositiveInfinity);
                if (this.IsSpent)
                {
                    break;
                }

                best = Math.Max(best, worth);
                worths.Add((action, after, worth));
            }

            return worths.ToImmutable();
        }

        public GameState Apply(GameState state, GameAction action)
        {
            this._spent++;
            return this._request.Rules.Apply(state, action).State;
        }

        /// <summary>What the state is worth to the bot, as it stands.</summary>
        public double Judge(GameState state) => this.JudgeFor(this._request, state);

        /// <summary>What the state is worth to the seat deciding <paramref name="deciding"/>, as it stands.</summary>
        private double JudgeFor(DecisionRequest deciding, GameState state)
        {
            this._spent++;
            return this._valuing.ValueOf(deciding, state);
        }

        /// <summary>
        /// What the state is worth to the bot, looking <paramref name="depth"/> decisions ahead: its value, once
        /// the game is over, or the look ends, or the budget is spent; the average over the dice, if it waits on
        /// them; else what the seat deciding would make of it.
        /// </summary>
        private double Value(GameState state, int depth, double alpha, double beta)
        {
            if (state.Outcome is not null || this.IsSpent)
            {
                return this.Judge(state);
            }

            if (depth == 0)
            {
                this.IsCut = true;
                return this.Judge(state);
            }

            SeatId seat = state.DecidingSeat.GetValueOrDefault(); // a game not over waits on someone
            return state.AwaitedDice is { } dice
                ? Foresight.Outcomes(dice).Sum(outcome =>
                    outcome.Chance * this.Value(
                        this.Apply(state, new DiceAction(seat, outcome.Values)),
                        depth,
                        double.NegativeInfinity,
                        double.PositiveInfinity
                    )
                )
                : this.Decided(state, seat, depth, alpha, beta);
        }

        /// <summary>
        /// What <paramref name="seat"/> would make of the state: the best for the bot, if the bot's own; the
        /// worst, if an enemy's; and as an ally would choose for itself, one step ahead, if an ally's. Where it
        /// can do nothing, the state's value; where it can do only one thing, what that leads to, the look none the
        /// shorter for it (passing at the end of a turn, say), up to <see cref="MostForced"/> such in a line.
        /// </summary>
        private double Decided(GameState state, SeatId seat, int depth, double alpha, double beta)
        {
            DecisionRequest deciding = this._request with
            {
                State = state,
                Seat = seat,
                LegalActions = this._request.Rules.LegalActions(state),
            };
            ImmutableList<GameState> next =
                [.. Playable.Of(deciding).Where(IsPlay).Select(action => this.Apply(state, action))];
            if (next.IsEmpty)
            {
                return this.Judge(state);
            }

            if (next.Count == 1 && this._forced < MostForced)
            {
                this._forced++;
                double forced = this.Value(next[0], depth, alpha, beta);
                this._forced--;
                return forced;
            }

            if (seat != this._request.Seat && state.Teams.AreAllied(seat, this._request.Seat))
            {
                GameState chosen = next.MaxBy(after => this.JudgeFor(deciding, after))!;
                return this.Value(chosen, depth - 1, alpha, beta);
            }

            bool isBots = seat == this._request.Seat;
            ImmutableList<(GameState After, double Worth)> judged =
            [
                .. next
                    .Select(after => (After: after, Worth: this.Judge(after)))
                    .OrderBy(step => isBots ? -step.Worth : step.Worth),
            ];
            return depth == 1
                ? this.Cut(judged)
                : this.Extreme(judged, depth, alpha, beta, isBots);
        }

        /// <summary>The first of the steps, the most extreme; the look stopping short there, if play goes on.</summary>
        private double Cut(ImmutableList<(GameState After, double Worth)> judged)
        {
            this.IsCut |= judged.Any(step => step.After.Outcome is null);
            return judged[0].Worth;
        }

        /// <summary>
        /// The best of the most promising steps for the bot (<paramref name="isBots"/>), or the worst for an enemy,
        /// each looked at further, but no further once one shows the line worse than one the bot has already.
        /// </summary>
        private double Extreme(
            ImmutableList<(GameState After, double Worth)> judged,
            int depth,
            double alpha,
            double beta,
            bool isBots
        )
        {
            double extreme = isBots
                ? double.NegativeInfinity
                : double.PositiveInfinity;
            foreach ((GameState after, _) in judged.Take(this._width))
            {
                double worth = this.Value(after, depth - 1, alpha, beta);
                extreme = isBots
                    ? Math.Max(extreme, worth)
                    : Math.Min(extreme, worth);
                alpha = isBots
                    ? Math.Max(alpha, worth)
                    : alpha;
                beta = isBots
                    ? beta
                    : Math.Min(beta, worth);
                if (alpha >= beta)
                {
                    break;
                }
            }

            return extreme;
        }

        /// <summary>Whether an action is play on the board, not an offer, a proposal or a vote.</summary>
        private static bool IsPlay(GameAction action) => !NotPlay.Contains(action.GetType());
    }
}
