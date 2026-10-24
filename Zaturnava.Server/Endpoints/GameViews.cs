using System.Collections.Immutable;
using Net.NowhereAtAll.Zaturnava.Application.Games;
using Net.NowhereAtAll.Zaturnava.Contracts;
using Net.NowhereAtAll.Zaturnava.Engine.Actions;
using Net.NowhereAtAll.Zaturnava.Engine.Boards;
using Net.NowhereAtAll.Zaturnava.Engine.Pieces;
using Net.NowhereAtAll.Zaturnava.Engine.Rules;
using Net.NowhereAtAll.Zaturnava.Engine.Seats;

namespace Net.NowhereAtAll.Zaturnava.Server.Endpoints;

/// <summary>
/// The application's games as the contracts show them. Everything a client draws comes from the
/// ruleset (cell layouts, names), so a client draws any board and any pieces without knowing them.
/// A person's token hash never leaves the server.
/// </summary>
internal static class GameViews
{
    public static CatalogDto CatalogOf(ImmutableList<IRulesApplying> rulesets, ImmutableList<string> botIds) =>
        new([.. rulesets.Select(RulesetOf)], botIds);

    public static GameSummaryDto SummaryOf(GameRecord record) =>
        new(
            record.Id.Value,
            record.RulesetId,
            record.Version,
            [.. record.Controllers.Select((controller, seat) => (controller, seat))
                .Where(entry => entry.controller.IsOpen)
                .Select(entry => entry.seat)]
        );

    public static GameViewDto ViewOf(GameSnapshot snapshot) =>
        new(
            snapshot.Record.Id.Value,
            snapshot.Rules.Id,
            snapshot.Rules.Name,
            snapshot.Record.Version,
            snapshot.State.TurnNumber,
            snapshot.State.DecidingSeat?.Value,
            [.. snapshot.Rules.Topology.Cells.Select(cell => CellOf(snapshot.Rules.Topology, cell))],
            [.. snapshot.State.Position.Occupants.OrderBy(entry => entry.Key.Value)
                .Select(entry => PieceOf(snapshot.Rules, entry.Key, entry.Value))],
            [.. snapshot.State.Seats.Select(seat => SeatOf(snapshot, seat.Id))],
            [.. snapshot.LegalActions.Select(action => ActionOf(action.Describe()))],
            snapshot.State.Dice
        );

    private static RulesetDto RulesetOf(IRulesApplying rules) =>
        new(rules.Id, rules.Name, rules.CreateInitialState().Seats.Count);

    private static CellDto CellOf(ICellNavigating topology, CellId cell)
    {
        CellLayout layout = topology.LayoutOf(cell);
        return new CellDto(cell.Value, layout.X, layout.Y, layout.Shape);
    }

    private static PieceDto PieceOf(IRulesApplying rules, CellId cell, Piece piece) =>
        new(piece.Id.Value, piece.Kind.Value, rules.NameOf(piece.Kind), piece.Owner.Value, cell.Value);

    private static SeatDto SeatOf(GameSnapshot snapshot, SeatId seat)
    {
        SeatController controller = snapshot.Record.Controllers[seat.Value];
        return new SeatDto(
            seat.Value,
            snapshot.Rules.NameOf(seat),
            snapshot.State.Teams.TeamOf(seat),
            snapshot.State.Scores.GetValueOrDefault(seat),
            controller.Kind,
            controller is BotSeat ? controller.Detail : null
        );
    }

    private static ActionDto ActionOf(ActionDescriptor descriptor) => new(descriptor.Kind, descriptor.Arguments);
}
