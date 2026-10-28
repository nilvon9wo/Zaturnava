using System.Collections.Immutable;
using Net.NowhereAtAll.Zaturnava.Engine.Events;
using Net.NowhereAtAll.Zaturnava.Engine.Rules;
using Net.NowhereAtAll.Zaturnava.Engine.Seats;

namespace Net.NowhereAtAll.Zaturnava.Games.Chaturaji2027;

/// <summary>
/// Who commands whose army (rules.md §6), and which team rules an empire (§5), told as it changes: after
/// each action, what changed is added to its events. A seat has complete control of an army while it plays
/// that army's turn as regent; partial control while it moves that army on its own turns. Nothing is told
/// once the game is over.
/// </summary>
internal sealed class Command(ThroneControl control)
{
    private readonly ThroneControl _control = control;

    public Transition Told(GameState before, Transition after)
    {
        GameState state = after.State;
        if (state.Outcome is not null)
        {
            return after;
        }

        ImmutableSortedDictionary<(int Commander, int Army), CommandBasis> was = this.CommandsOf(before);
        ImmutableSortedDictionary<(int Commander, int Army), CommandBasis> now = this.CommandsOf(state);
        ImmutableSortedSet<int> wereEmpires = this.EmpiresOf(before);
        ImmutableSortedSet<int> empires = this.EmpiresOf(state);
        bool isCreated = after.Events.OfType<EmpireCreated>().Any();
        GameEvent[] told =
        [
            .. was.Keys.Where(pair => !now.ContainsKey(pair))
                .Select(pair => new CommandEnded(new SeatId(pair.Commander), new SeatId(pair.Army))),
            .. now.Where(command => !was.TryGetValue(command.Key, out CommandBasis basis) || basis != command.Value)
                .Select(Begun),
            .. wereEmpires.Except(empires).Select(team => new EmpireEnded(RulersOf(state, team))),
            .. empires.Except(wereEmpires)
                .Where(_ => !isCreated)
                .Select(team => new EmpireBegun(RulersOf(state, team))),
        ];
        return after with { Events = after.Events.AddRange(told) };
    }

    /// <summary>Each army commanded by another seat than its own, by whom, and why; regency first.</summary>
    private ImmutableSortedDictionary<(int Commander, int Army), CommandBasis> CommandsOf(GameState state)
    {
        // a seat acts for another only while one decides
        (SeatId regent, SeatId? actingFor) = state.Proposal is { } proposal
            ? (proposal.Resume, proposal.ResumeActingFor)
            : (state.DecidingSeat.GetValueOrDefault(), state.ActingFor);
        IEnumerable<(SeatId Commander, SeatId Army, CommandBasis Basis)> regency =
            actingFor is { } army && state.Teams.AreAllied(regent, army)
                ? [(regent, army, CommandBasis.Regency)]
                : [];
        IEnumerable<(SeatId Commander, SeatId Army, CommandBasis Basis)> standing = state.Seats
            .Select(seat => seat.Id)
            .SelectMany(
                seat => this._control.CommandedBy(state, seat).Select(command => (seat, command.Army, command.Basis))
            );
        return regency
            .Concat(standing)
            .DistinctBy(command => (command.Commander, command.Army))
            .ToImmutableSortedDictionary(
                command => (command.Commander.Value, command.Army.Value),
                command => command.Basis
            );
    }

    private static CommandBegun Begun(KeyValuePair<(int Commander, int Army), CommandBasis> command) =>
        new(new SeatId(command.Key.Commander), new SeatId(command.Key.Army), command.Value);

    /// <summary>
    /// The teams ruling an empire: none of their enemies has a Raja on the board, but for a retired seat's, and
    /// one of them commands an ally's army.
    /// </summary>
    private ImmutableSortedSet<int> EmpiresOf(GameState state)
    {
        ImmutableList<int> enemyRajas =
        [
            .. state.Position.Occupants.Values
                .Where(piece => piece.Kind == Chaturaji2027Pieces.Raja && !state.Retired.Contains(piece.Owner))
                .Select(piece => state.Teams.TeamOf(piece.Owner)),
        ];
        return
        [
            .. state.Seats
                .Where(seat => this._control.CommandsAnAlly(state, seat.Id))
                .Select(seat => state.Teams.TeamOf(seat.Id))
                .Where(team => enemyRajas.All(rajas => rajas == team)),
        ];
    }

    private static ImmutableList<SeatId> RulersOf(GameState state, int team) =>
        [.. state.Seats.Select(seat => seat.Id).Where(seat => state.Teams.TeamOf(seat) == team)];
}
