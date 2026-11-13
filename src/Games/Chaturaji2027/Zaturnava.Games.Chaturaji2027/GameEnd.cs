using System.Collections.Immutable;
using System.Globalization;
using Net.NowhereAtAll.Zaturnava.Engine.Boards;
using Net.NowhereAtAll.Zaturnava.Engine.Events;
using Net.NowhereAtAll.Zaturnava.Engine.Pieces;
using Net.NowhereAtAll.Zaturnava.Engine.Rules;
using Net.NowhereAtAll.Zaturnava.Engine.Seats;

namespace Net.NowhereAtAll.Zaturnava.Games.Chaturaji2027;

/// <summary>
/// When the game ends (rules.md §10): when only one team has pieces left, which wins outright; when
/// only Rajas are left, an honourable peace; by points, the team with more winning, when the players
/// agree to end it, or, as a safety net, when every seat still playing has passed its turn some rounds in
/// a row without rolling.
/// </summary>
internal static class GameEnd
{
    public const string OneTeamLeft = "only one team has pieces left";
    public const string OnlyRajasLeft = "only Rajas are left, an honourable peace";

    public const string RetiredAndLevel =
        "a team has only retired Rajas left, and at least as many points as its enemies: a draw";

    public const string RetiredAndBehind = "a team has only retired Rajas left, and fewer points than its enemies";
    public const string TeamGaveUp = "a whole team gave up their turns, so points decide";

    /// <summary>
    /// How many rounds before the game would end for want of a capture its players are warned (Proposed).
    /// </summary>
    public static readonly ImmutableList<int> QuietWarnings = [10, 1];

    /// <summary>
    /// The ended game, if the board leaves nothing to play for; otherwise null. A team whose only pieces left are
    /// retired Rajas plays no more: a draw if it has as many points as its enemies or more, else they win - so that
    /// neither retiring a Raja nor taking a retired one decides the game.
    /// </summary>
    public static Transition? AfterMove(Transition transition, Chaturaji2027Variant variant)
    {
        GameState state = transition.State;
        ImmutableList<Piece> pieces = [.. state.Position.Occupants.Values];
        ImmutableList<int> teams = [.. pieces.Select(piece => state.Teams.TeamOf(piece.Owner)).Distinct()];
        int? retired = teams
            .Where(team => pieces
                .Where(piece => state.Teams.TeamOf(piece.Owner) == team)
                .All(piece => state.Retired.Contains(piece.Owner) && variant.Leaders.Contains(piece.Kind))
            )
            .Cast<int?>()
            .FirstOrDefault();
        GameOutcome? outcome = (teams.Count, pieces.All(piece => variant.Leaders.Contains(piece.Kind)), retired) switch
        {
            (1, _, _) => new GameOutcome(TeamOf(state, teams[0]), Chaturaji2027Endings.OneTeamLeft, OneTeamLeft),
            (_, true, _) => new GameOutcome([], Chaturaji2027Endings.OnlyRajasLeft, OnlyRajasLeft),
            (_, _, { } team) => OnlyRetiredLeft(state, team),
            _ => null,
        };
        return outcome is null
            ? null
            : Ended(transition, outcome);
    }

    /// <summary>
    /// The transition, its count of idle passes started again if anything on the board or among the prisoners
    /// changed since <paramref name="before"/> (Stated 2026-11-06): a move, a ransom, an escape, a promotion.
    /// </summary>
    public static Transition Unidled(GameState before, Transition transition) =>
        before.Prisoners.SequenceEqual(transition.State.Prisoners)
        && Placements(before).SetEquals(Placements(transition.State))
            ? transition
            : transition with { State = transition.State with { IdlePasses = 0 } };

    private static ImmutableHashSet<(CellId Cell, Piece Piece)> Placements(GameState state) =>
        [.. state.Position.Occupants.Select(occupant => (occupant.Key, occupant.Value))];

    /// <summary>
    /// Whether no one has moved for every round but the last of the <paramref name="rounds"/> that end the game,
    /// just now: its players are to be warned.
    /// </summary>
    public static bool IsLastIdleRound(GameState state, int rounds) =>
        state.IdlePasses == (rounds - 1) * state.Seats.Count(seat => IsPlaying(state, seat.Id));

    /// <summary>The ended game, once no one has moved for <paramref name="rounds"/> rounds.</summary>
    public static Transition? AfterIdlePass(Transition transition, int rounds)
    {
        GameState state = transition.State;
        int playing = state.Seats.Count(seat => IsPlaying(state, seat.Id));
        string reason = string.Create(
            CultureInfo.InvariantCulture,
            $"no one moved for {rounds} rounds, so points decide"
        );
        return state.IdlePasses >= rounds * playing
            ? ByPoints(transition, Chaturaji2027Endings.Idle, reason)
            : null;
    }

    /// <summary>
    /// The transition, with the turns since a piece was last taken counted: the game ends on points once no piece
    /// has been taken for <paramref name="rounds"/> rounds, its players warned as each of
    /// <see cref="QuietWarnings"/> rounds are left.
    /// </summary>
    public static Transition AfterQuietTurns(GameState before, Transition transition, int rounds)
    {
        GameState state = transition.State;
        int turns = transition.Events.Any(happened => happened is PieceCaptured)
            ? 0
            : before.TurnsSinceCapture + transition.Events.Count(happened => happened is TurnPassed);
        Transition counted = transition with { State = state with { TurnsSinceCapture = turns } };
        int playing = state.Seats.Count(seat => IsPlaying(state, seat.Id));
        int? left = QuietWarnings
            .Where(warning => IsReached(before.TurnsSinceCapture, turns, (rounds - warning) * playing))
            .Cast<int?>()
            .FirstOrDefault();
        string reason = string.Create(
            CultureInfo.InvariantCulture,
            $"no piece was taken for {rounds} rounds, so points decide"
        );
        return (state.Outcome, turns >= rounds * playing, left) switch
        {
            (not null, _, _) => counted,
            (_, true, _) => ByPoints(counted, Chaturaji2027Endings.Quiet, reason),
            (_, _, { } roundsLeft) => counted with
            {
                Events = counted.Events.Add(
                    new QuietWarned(state.DecidingSeat!.Value, rounds - roundsLeft, roundsLeft) // the game goes on
                ),
            },
            _ => counted,
        };
    }

    /// <summary>
    /// The transition, with the position its turn ended in kept, and counted if seen before: the game ends on
    /// points once every turn of <paramref name="rounds"/> rounds has (Stated 2026-11-06), its players warned as
    /// the last begins. A turn passed idly is left to the idle count; a capture forgets every position before it,
    /// as none can come again.
    /// </summary>
    public static Transition AfterRepeats(Transition transition, int rounds)
    {
        Transition forgotten = transition.Events.Any(happened => happened is PieceCaptured)
            ? transition with { State = transition.State with { Seen = [], RepeatedTurns = 0 } }
            : transition;
        return transition.State.Outcome is null && transition.Events.Any(happened => happened is TurnPassed)
            ? Repeated(forgotten, rounds)
            : forgotten;
    }

    private static Transition Repeated(Transition transition, int rounds)
    {
        GameState state = transition.State;
        long key = KeyOf(state);
        int repeated = (state.IdlePasses > 0, state.Seen.Contains(key)) switch
        {
            (true, _) => state.RepeatedTurns,
            (_, true) => state.RepeatedTurns + 1,
            _ => 0,
        };
        Transition counted = transition with
        {
            State = state with { Seen = state.Seen.Add(key), RepeatedTurns = repeated },
        };
        int playing = state.Seats.Count(seat => IsPlaying(state, seat.Id));
        string reason = string.Create(
            CultureInfo.InvariantCulture,
            $"for {rounds} rounds every turn ended where the game had been before, so points decide"
        );
        return (repeated >= rounds * playing, IsReached(state.RepeatedTurns, repeated, (rounds - 1) * playing)) switch
        {
            (true, _) => ByPoints(counted, Chaturaji2027Endings.Repeated, reason),
            (_, true) => counted with
            {
                Events = counted.Events.Add(new RepetitionWarned(state.DecidingSeat!.Value, rounds - 1)),
            },
            _ => counted,
        };
    }

    /// <summary>
    /// The position, its prisoners and the seat to play, as a key: two hashes of the pieces where they stand, each
    /// alike whatever order they are listed in.
    /// </summary>
    private static long KeyOf(GameState state)
    {
        int prisoners = state.Prisoners.Aggregate(0, (key, prisoner) => key ^ prisoner.GetHashCode());
        int high = state.Position.Occupants.Aggregate(
            0,
            (key, occupant) => key ^ HashCode.Combine(occupant.Key, occupant.Value)
        );
        int low = state.Position.Occupants.Aggregate(
            0,
            (key, occupant) => key ^ HashCode.Combine(occupant.Value, occupant.Key, prisoners)
        );
        return ((long)HashCode.Combine(high, prisoners, state.DecidingSeat) << 32)
            | (uint)HashCode.Combine(low, state.DecidingSeat);
    }

    /// <summary>Whether a count, <paramref name="from"/> one to <paramref name="to"/>, just reached a mark.</summary>
    private static bool IsReached(int from, int to, int mark) => from < mark && to >= mark;

    /// <summary>
    /// The ended game, once every seat of a team still playing has given up its turns (rules.md §10), if that
    /// team trails on points, or leads by a <see cref="Scoring.ClearLead"/> on points and on the board both: points
    /// decide, any proposal waiting with them. Otherwise the game as it was, its enemies playing on, so that a
    /// team cannot win by giving up the moment it leads.
    /// </summary>
    public static Transition AfterGivingUp(Transition transition, Chaturaji2027Variant variant)
    {
        GameState state = transition.State;
        bool hasTeamGivenUp = state.Outcome is null
            && state.Seats
                .Where(seat => IsPlaying(state, seat.Id))
                .GroupBy(seat => state.Teams.TeamOf(seat.Id))
                .Any(team =>
                    team.All(seat => state.Resigned.Contains(seat.Id)) && MayGiveUp(state, team.Key, variant)
                );
        return hasTeamGivenUp
            ? ByPoints(
                transition with { State = state with { Proposal = null } },
                Chaturaji2027Endings.TeamGaveUp,
                TeamGaveUp
            )
            : transition;
    }

    /// <summary>The ended game, the team with more points winning; a draw, if the teams are level.</summary>
    public static Transition ByPoints(Transition transition, string ending, string reason) =>
        Ended(transition, OutcomeByPoints(transition.State, ending, reason));

    /// <summary>
    /// Whether the seat still plays, taking its turns (rules.md §10): it has not retired, and has pieces on the
    /// board, or pieces held prisoner, which may yet escape, or prisoners of its own, to offer back.
    /// </summary>
    public static bool IsPlaying(GameState state, SeatId seat) =>
        !state.Retired.Contains(seat)
        && (state.Position.Occupants.Values.Any(piece => piece.Owner == seat)
            || state.Prisoners.Any(prisoner => prisoner.Piece.Owner == seat || prisoner.Holder == seat)
            || state.Fled.Any(piece => piece.Owner == seat));

    /// <summary>Whether giving up its turns ends the game for the team: it trails, or leads by far.</summary>
    private static bool MayGiveUp(GameState state, int team, Chaturaji2027Variant variant)
    {
        int points = PointsLead(state, team);
        int clear = variant.Scoring.ClearLead;
        return points < 0 || (points >= clear && BoardLead(state, team, variant) >= clear);
    }

    /// <summary>The points of the team, less its enemies'.</summary>
    public static int PointsLead(GameState state, int team) =>
        state.Seats.Sum(seat => Side(state, team, seat.Id) * state.Scores[seat.Id]);

    /// <summary>What the pieces of the team on the board would score if taken, less its enemies'.</summary>
    private static int BoardLead(GameState state, int team, Chaturaji2027Variant variant) =>
        state.Position.Occupants.Values
            .Where(piece => !state.Retired.Contains(piece.Owner))
            .Sum(piece => Side(state, team, piece.Owner) * variant.Points[piece.Kind]);

    private static int Side(GameState state, int team, SeatId seat) => state.Teams.TeamOf(seat) == team ? 1 : -1;

    private static GameOutcome OutcomeByPoints(GameState state, string ending, string reason)
    {
        ImmutableList<(int Team, int Points)> totals =
        [
            .. state.Seats
                .GroupBy(seat => state.Teams.TeamOf(seat.Id))
                .Select(team => (team.Key, team.Sum(seat => state.Scores[seat.Id]))),
        ];
        int best = totals.Max(total => total.Points);
        ImmutableList<int> leaders = [.. totals.Where(total => total.Points == best).Select(total => total.Team)];
        return new GameOutcome(leaders.Count == 1 ? TeamOf(state, leaders[0]) : [], ending, reason);
    }

    /// <summary>A team left only retired Rajas: a draw if it is not behind on points, else its enemies win.</summary>
    private static GameOutcome OnlyRetiredLeft(GameState state, int team) =>
        PointsLead(state, team) >= 0
            ? new GameOutcome([], Chaturaji2027Endings.OnlyRetiredLeft, RetiredAndLevel)
            : new GameOutcome(
                [.. state.Seats.Select(seat => seat.Id).Where(seat => state.Teams.TeamOf(seat) != team)],
                Chaturaji2027Endings.OnlyRetiredLeft,
                RetiredAndBehind
            );

    private static ImmutableList<SeatId> TeamOf(GameState state, int team) =>
        [.. state.Seats.Select(seat => seat.Id).Where(seat => state.Teams.TeamOf(seat) == team)];

    private static Transition Ended(Transition transition, GameOutcome outcome)
    {
        GameState ended = transition.State with
        {
            DecidingSeat = null,
            ActingFor = null,
            AwaitedDice = null,
            Dice = [],
            Outcome = outcome,
        };
        return new Transition(ended, transition.Events.Add(new GameEnded(outcome)));
    }
}
