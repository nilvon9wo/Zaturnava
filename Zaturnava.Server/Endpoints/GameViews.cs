using System.Collections.Immutable;
using Net.NowhereAtAll.Zaturnava.Application.Games;
using Net.NowhereAtAll.Zaturnava.Contracts;
using Net.NowhereAtAll.Zaturnava.Engine.Actions;
using Net.NowhereAtAll.Zaturnava.Engine.Boards;
using Net.NowhereAtAll.Zaturnava.Engine.Decisions;
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
    public static CatalogDto CatalogOf(ImmutableList<IRulesApplying> rulesets, ImmutableList<BotProfile> bots) =>
        new([.. rulesets.Select(RulesetOf)], [.. bots.Select(BotOf)]);

    public static GameSummaryDto SummaryOf(GameRecord record) =>
        new(
            record.Id.Value,
            record.RulesetId,
            record.Version,
            [.. record.Controllers.Select((controller, seat) => (controller, seat))
                .Where(entry => entry.controller.IsOpen)
                .Select(entry => entry.seat)],
            [.. record.Controllers.Select(PlayerOf)],
            record.Actions.Count
        );

    /// <summary>The game as its seats see it, naming each bot as <paramref name="bots"/> do.</summary>
    public static GameViewDto ViewOf(GameSnapshot snapshot, ImmutableList<BotProfile> bots)
    {
        ImmutableDictionary<CellId, SeatId> thrones =
            snapshot.Rules.Thrones.ToImmutableDictionary(throne => throne.Value, throne => throne.Key);
        return new GameViewDto(
            snapshot.Record.Id.Value,
            snapshot.Rules.Id,
            snapshot.Rules.Name,
            snapshot.Record.Version,
            snapshot.State.TurnNumber,
            snapshot.State.DecidingSeat?.Value,
            snapshot.State.ActingFor?.Value,
            [.. snapshot.Rules.Topology.Cells.Select(cell => CellOf(snapshot.Rules.Topology, thrones, cell))],
            [.. snapshot.State.Position.Occupants.OrderBy(entry => entry.Key.Value)
                .Select(entry => PieceOf(snapshot.Rules, entry.Key, entry.Value))],
            [.. snapshot.State.Seats.Select(seat => SeatOf(snapshot, bots, seat.Id))],
            [.. snapshot.LegalActions.Select(action => ActionOf(snapshot, action))],
            [.. snapshot.State.Dice.Select(value => new DieDto(value, snapshot.Rules.NameOfDie(value)))],
            GameLog.LatestOf(snapshot),
            OutcomeOf(snapshot.Rules, snapshot.State.Outcome),
            snapshot.Rules.MayOffer(snapshot.State),
            OfferOf(snapshot),
            snapshot.TimeLeft?.TotalSeconds,
            snapshot.State.PromotionDue?.Value,
            snapshot.State.IsRansomWindow,
            snapshot.State.Returning is { } due ? PrisonerOf(snapshot.Rules, due.Prisoners[0]) : null,
            ThroneHolderOf(snapshot.Rules, snapshot.State)
        );
    }

    /// <summary>The enemy seat whose leader sits on the throne of the seat deciding for itself; null if none.</summary>
    private static int? ThroneHolderOf(IRulesApplying rules, GameState state) =>
        state.ActingFor is null && state.DecidingSeat is { } seat
            ? rules.Thrones
                .Where(throne => throne.Key == seat)
                .Select(throne => state.Position.PieceAt(throne.Value))
                .OfType<Piece>()
                .Where(holder => rules.IsLeader(holder.Kind) && !state.Teams.AreAllied(holder.Owner, seat))
                .Select(holder => (int?)holder.Owner.Value)
                .FirstOrDefault()
            : null;

    private static RulesetDto RulesetOf(IRulesApplying rules) =>
        new(rules.Id, rules.Name, rules.CreateInitialState().Seats.Count);

    private static BotDto BotOf(BotProfile bot) => new(bot.Id, bot.Name, bot.Description, bot.UsesAi);

    private static CellDto CellOf(
        ICellNavigating topology,
        ImmutableDictionary<CellId, SeatId> thrones,
        CellId cell
    )
    {
        CellLayout layout = topology.LayoutOf(cell);
        int? throne = thrones.TryGetValue(cell, out SeatId seat) ? seat.Value : null;
        return new CellDto(cell.Value, layout.X, layout.Y, layout.Shape, throne, topology.NameOf(cell));
    }

    private static PieceDto PieceOf(IRulesApplying rules, CellId cell, Piece piece) =>
        new(piece.Id.Value, piece.Kind.Value, rules.NameOf(piece.Kind), piece.Owner.Value, cell.Value);

    private static SeatDto SeatOf(GameSnapshot snapshot, ImmutableList<BotProfile> bots, SeatId seat)
    {
        SeatController controller = snapshot.Record.Controllers[seat.Value];
        string? botId = controller is BotSeat botSeat ? botSeat.BotId : null;
        return new SeatDto(
            seat.Value,
            snapshot.Rules.NameOf(seat),
            snapshot.State.Teams.TeamOf(seat),
            snapshot.State.Scores.GetValueOrDefault(seat),
            controller.Kind,
            botId is null ? null : BotNameOf(bots, botId),
            [.. snapshot.State.Prisoners.Where(prisoner => prisoner.Holder == seat)
                .Select(prisoner => PrisonerOf(snapshot.Rules, prisoner))],
            botId
        );
    }

    /// <summary>The bot's name; its id, should it no longer be on offer.</summary>
    private static string BotNameOf(ImmutableList<BotProfile> bots, string id) =>
        bots.Find(bot => bot.Id == id)?.Name ?? id;

    private static PrisonerDto PrisonerOf(IRulesApplying rules, Prisoner prisoner) =>
        new(
            prisoner.Piece.Id.Value,
            prisoner.Piece.Kind.Value,
            rules.NameOf(prisoner.Piece.Kind),
            prisoner.Piece.Owner.Value
        );

    /// <summary>The ransom waiting on its answer, naming its prisoners, who are held until it is answered.</summary>
    private static OfferDto? OfferOf(GameSnapshot snapshot) =>
        snapshot.State.Offer is { } offer
            ? new OfferDto(
                offer.From.Value,
                offer.To.Value,
                PrisonersOf(snapshot, offer.Given),
                PrisonersOf(snapshot, offer.Asked)
            )
            : null;

    private static ImmutableList<PrisonerDto> PrisonersOf(GameSnapshot snapshot, ImmutableList<PieceId> pieces) =>
        [
            .. pieces.Select(piece => snapshot.State.Prisoners.First(prisoner => prisoner.Piece.Id == piece))
                .Select(prisoner => PrisonerOf(snapshot.Rules, prisoner)),
        ];

    private static OutcomeDto? OutcomeOf(IRulesApplying rules, GameOutcome? outcome) =>
        outcome is null
            ? null
            : new OutcomeDto([.. outcome.Winners.Select(seat => seat.Value)], GameLog.Story(rules, outcome));

    private static string PlayerOf(SeatController controller) =>
        controller switch
        {
            BotSeat bot => bot.BotId,
            HumanSeat => GameSummaryDto.Person,
            _ => GameSummaryDto.Open,
        };

    private static ActionDto ActionOf(GameSnapshot snapshot, GameAction action)
    {
        ActionDescriptor descriptor = action.Describe();
        return new ActionDto(descriptor.Kind, descriptor.Arguments) { Label = LabelOf(snapshot, action) };
    }

    /// <summary>
    /// What a button says that its kind alone cannot: which Pawn becomes what; which prisoner escapes, and
    /// where to; which ally a turn is handed to; that passing once a turn is over means being done offering
    /// ransoms; and that passing on a promotion due leaves it for later. Null for any other action.
    /// </summary>
    private static string? LabelOf(GameSnapshot snapshot, GameAction action) =>
        (action, snapshot.State.IsRansomWindow, snapshot.State.PromotionDue) switch
        {
            (PromoteAction promote, _, _) => PromotionOf(snapshot, promote),
            (EscapeAction escape, _, _) => EscapeOf(snapshot, escape),
            (HandOverAction handOver, _, _) => $"Let {snapshot.Rules.NameOf(handOver.Regent)} take over",
            (PassAction, true, _) => "Done offering",
            (PassAction, _, { }) => "Promote later",
            _ => null,
        };

    /// <summary>"Escape Green's Raja, held by Red, to h4".</summary>
    private static string EscapeOf(GameSnapshot snapshot, EscapeAction escape)
    {
        IRulesApplying rules = snapshot.Rules;
        Prisoner prisoner = snapshot.State.Prisoners.First(held => held.Piece.Id == escape.Piece);
        return $"Escape {rules.NameOf(prisoner.Piece.Owner)}'s {rules.NameOf(prisoner.Piece.Kind)}, "
            + $"held by {rules.NameOf(prisoner.Holder)}, to {rules.Topology.NameOf(escape.Home)}";
    }

    /// <summary>"Promote the Pawn on c8 to Green's Elephant, held by Red".</summary>
    private static string PromotionOf(GameSnapshot snapshot, PromoteAction promote)
    {
        IRulesApplying rules = snapshot.Rules;
        Piece pawn = snapshot.State.Position.PieceAt(promote.Cell)!; // a legal promotion has a Pawn to promote
        Prisoner into = snapshot.State.Prisoners.First(prisoner => prisoner.Piece.Id == promote.Into);
        return $"Promote the {rules.NameOf(pawn.Kind)} on {rules.Topology.NameOf(promote.Cell)} "
            + $"to {rules.NameOf(into.Piece.Owner)}'s {rules.NameOf(into.Piece.Kind)}, "
            + $"held by {rules.NameOf(into.Holder)}";
    }
}
