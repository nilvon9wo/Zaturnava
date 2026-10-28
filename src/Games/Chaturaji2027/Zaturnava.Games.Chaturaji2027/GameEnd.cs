using System.Collections.Immutable;
using System.Globalization;
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
    public const string TeamGaveUp = "a whole team gave up their turns, so points decide";

    /// <summary>How far ahead, on points and on the board both, a team must be to win by giving up its turns.</summary>
    public const int ClearLead = 10;

    /// <summary>The ended game, if the board leaves nothing to play for; otherwise null.</summary>
    public static Transition? AfterMove(Transition transition)
    {
        GameState state = transition.State;
        ImmutableList<Piece> pieces = [.. state.Position.Occupants.Values];
        ImmutableList<int> teams = [.. pieces.Select(piece => state.Teams.TeamOf(piece.Owner)).Distinct()];
        GameOutcome? outcome = (teams.Count, pieces.All(piece => piece.Kind == Chaturaji2027Pieces.Raja)) switch
        {
            (1, _) => new GameOutcome(TeamOf(state, teams[0]), OneTeamLeft),
            (_, true) => new GameOutcome([], OnlyRajasLeft),
            _ => null,
        };
        return outcome is null
            ? null
            : Ended(transition, outcome);
    }

    /// <summary>The ended game, once no one has rolled or moved for <paramref name="rounds"/> rounds.</summary>
    public static Transition? AfterIdlePass(Transition transition, int rounds)
    {
        GameState state = transition.State;
        int playing = state.Seats.Count(seat => IsPlaying(state, seat.Id));
        string reason = string.Create(
            CultureInfo.InvariantCulture,
            $"no one rolled or moved for {rounds} rounds, so points decide"
        );
        return state.IdlePasses >= rounds * playing
            ? ByPoints(transition, reason)
            : null;
    }

    /// <summary>
    /// The ended game, once every seat of a team still playing has given up its turns (rules.md §10), if that
    /// team trails on points, or leads by a <see cref="ClearLead"/> on points and on the board both: points
    /// decide, any proposal waiting with them. Otherwise the game as it was, its enemies playing on, so that a
    /// team cannot win by giving up the moment it leads.
    /// </summary>
    public static Transition AfterGivingUp(Transition transition)
    {
        GameState state = transition.State;
        bool hasTeamGivenUp = state.Outcome is null
            && state.Seats
                .Where(seat => IsPlaying(state, seat.Id))
                .GroupBy(seat => state.Teams.TeamOf(seat.Id))
                .Any(team => team.All(seat => state.Resigned.Contains(seat.Id)) && MayGiveUp(state, team.Key));
        return hasTeamGivenUp
            ? ByPoints(transition with { State = state with { Proposal = null } }, TeamGaveUp)
            : transition;
    }

    /// <summary>The ended game, the team with more points winning; a draw, if the teams are level.</summary>
    public static Transition ByPoints(Transition transition, string reason) =>
        Ended(transition, OutcomeByPoints(transition.State, reason));

    /// <summary>Whether the seat still takes its turns: it has pieces left, and has not retired.</summary>
    public static bool IsPlaying(GameState state, SeatId seat) =>
        !state.Retired.Contains(seat) && state.Position.Occupants.Values.Any(piece => piece.Owner == seat);

    /// <summary>Whether giving up its turns ends the game for the team: it trails, or leads by far.</summary>
    private static bool MayGiveUp(GameState state, int team) =>
        (PointsLead(state, team), BoardLead(state, team)) switch
        {
            ( < 0, _) => true,
            ( >= ClearLead, >= ClearLead) => true,
            _ => false,
        };

    /// <summary>The points of the team, less its enemies'.</summary>
    private static int PointsLead(GameState state, int team) =>
        state.Seats.Sum(seat => Side(state, team, seat.Id) * state.Scores[seat.Id]);

    /// <summary>What the pieces of the team on the board would score if taken, less its enemies'.</summary>
    private static int BoardLead(GameState state, int team) =>
        state.Position.Occupants.Values
            .Where(piece => !state.Retired.Contains(piece.Owner))
            .Sum(piece => Side(state, team, piece.Owner) * Chaturaji2027Pieces.Points[piece.Kind]);

    private static int Side(GameState state, int team, SeatId seat) => state.Teams.TeamOf(seat) == team ? 1 : -1;

    private static GameOutcome OutcomeByPoints(GameState state, string reason)
    {
        ImmutableList<(int Team, int Points)> totals =
        [
            .. state.Seats
                .GroupBy(seat => state.Teams.TeamOf(seat.Id))
                .Select(team => (team.Key, team.Sum(seat => state.Scores[seat.Id]))),
        ];
        int best = totals.Max(total => total.Points);
        ImmutableList<int> leaders = [.. totals.Where(total => total.Points == best).Select(total => total.Team)];
        return new GameOutcome(leaders.Count == 1 ? TeamOf(state, leaders[0]) : [], reason);
    }

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
