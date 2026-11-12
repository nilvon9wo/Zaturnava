using System.Collections.Immutable;
using Net.NowhereAtAll.Zaturnava.Application.Games;
using Net.NowhereAtAll.Zaturnava.Application.Scenarios;
using Net.NowhereAtAll.Zaturnava.Contracts;
using Net.NowhereAtAll.Zaturnava.Engine.Actions;
using Net.NowhereAtAll.Zaturnava.Engine.Boards;
using Net.NowhereAtAll.Zaturnava.Engine.Decisions;
using Net.NowhereAtAll.Zaturnava.Engine.Events;
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
    public static CatalogDto CatalogOf(
        ImmutableList<IRulesApplying> rulesets,
        ImmutableList<BotProfile> bots,
        ImmutableList<Scenario> scenarios
    ) =>
        new([.. rulesets.Select(RulesetOf)], [.. bots.Select(BotOf)])
        {
            Scenarios =
            [
                .. scenarios.Select(scenario => new ScenarioDto(
                    scenario.Id,
                    scenario.RulesetId,
                    scenario.Name,
                    scenario.Description
                )),
            ],
        };

    /// <summary>
    /// The game as a lobby lists it, as stored: its seats named by its rules, and each person's seat by what
    /// <paramref name="names"/> call whoever holds it.
    /// </summary>
    public static GameSummaryDto SummaryOf(
        GameListing game,
        IRulesApplying rules,
        ImmutableDictionary<HumanSeat, string>? names = null
    ) =>
        new(
            game.Id.Value,
            game.RulesetId,
            game.Version,
            [.. game.Controllers.Select((controller, seat) => (controller, seat))
                .Where(entry => entry.controller.IsOpen)
                .Select(entry => entry.seat)],
            [.. game.Controllers.Select(controller => PersonOf(controller, names) ?? controller.Player)],
            game.Moves
        )
        {
            SeatNames = [.. game.Controllers.Select((_, seat) => rules.NameOf(new SeatId(seat)))],
            PlayerIds =
            [
                .. game.Controllers.Select(controller =>
                    controller is HumanSeat { IsAi: false, Claimant: { } claimant } ? claimant.Value : null
                ),
            ],
            IsOver = game.IsOver,
            Created = game.Created,
            LastChanged = game.LastChanged,
            AwaySeats = [.. game.Away.Select(seat => seat.Value)],
            IsForPlayers = game.Controllers.Any(controller => controller is OpenSeat { IsForPlayers: true }),
        };

    /// <summary>A game's history, on the wire: the same shape self-play writes it in.</summary>
    public static GameHistoryDto HistoryOf(GameHistory history) =>
        new(
            history.Format,
            history.GameId,
            history.RulesetId,
            [
                .. history.Seats.Select(seat =>
                    new HistorySeatDto(seat.Id, seat.Name, seat.Team, seat.Player, seat.Score)
                ),
            ],
            [.. history.Actions.Select(action => new ActionDto(action.Kind, action.Arguments))],
            history.Outcome is { } outcome
                ? new HistoryOutcomeDto(outcome.Winners, outcome.Ending, outcome.Reason)
                : null
        );

    /// <summary>
    /// The game as its seats see it, naming each bot as <paramref name="bots"/> do, and whoever holds each person's
    /// seat as <paramref name="names"/> do.
    /// </summary>
    public static GameViewDto ViewOf(
        GameSnapshot snapshot,
        ImmutableList<BotProfile> bots,
        ImmutableDictionary<HumanSeat, string>? names = null
    )
    {
        ImmutableDictionary<CellId, SeatId> thrones =
            snapshot.Rules
                .ThronesOf(snapshot.State)
                .ToImmutableDictionary(throne => throne.Value, throne => throne.Key);
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
            [.. snapshot.State.Seats.Select(seat => SeatOf(snapshot, bots, seat.Id, names))],
            [.. snapshot.LegalActions.Select(action => ActionOf(snapshot, action))],
            [.. snapshot.State.Dice.Select(value => DieOf(snapshot, value))],
            GameLog.LatestOf(snapshot),
            OutcomeOf(snapshot.Rules, snapshot.State.Outcome),
            snapshot.Rules.MayOffer(snapshot.State),
            OfferOf(snapshot),
            snapshot.TimeLeft?.TotalSeconds,
            snapshot.State.PromotionDue?.Value,
            snapshot.State.IsRansomWindow,
            snapshot.State.Returning is { } due ? PrisonerOf(snapshot.Rules, due.Prisoners[0]) : null,
            ThroneHolderOf(snapshot.Rules, snapshot.State),
            ProposalOf(snapshot.State),
            AwayOf(snapshot.Away),
            VacancyOf(snapshot.Vacancy)
        )
        {
            Kinds = KindsOf(snapshot.Rules, snapshot.State),
            Lines =
            [
                .. snapshot.Rules.LinesOf(snapshot.State)
                    .Select(line => new LineDto(line.Kind, line.FromX, line.FromY, line.ToX, line.ToY)),
            ],
            Terrain = snapshot.Rules.TerrainOf(snapshot.State),
            IsDealt = snapshot.Rules.IsDealt,
            AreArmiesAlike = ScenarioStart.AreAlike(snapshot.Record.Actions),
            IsForPlayers = snapshot.Record.Controllers.Any(controller => controller.IsForPlayers),
        };
    }

    /// <summary>
    /// Each kind of piece on the board or held prisoner, and each its own promotion may make it, by name, as one
    /// looking at it is told.
    /// </summary>
    private static ImmutableList<PieceKindDto> KindsOf(IRulesApplying rules, GameState state) =>
    [
        .. state.Position.Occupants.Values
            .Concat(state.Prisoners.Select(prisoner => prisoner.Piece))
            .Select(piece => piece.Kind)
            .Distinct()
            .SelectMany(kind => rules.GuideOf(kind).PromotesTo.Prepend(kind))
            .Distinct()
            .Select(kind => KindOf(rules, kind))
            .OrderBy(kind => kind.Name, StringComparer.Ordinal),
    ];

    private static PieceKindDto KindOf(IRulesApplying rules, PieceKindId kind)
    {
        PieceGuide guide = rules.GuideOf(kind);
        return new PieceKindDto(kind.Value, rules.NameOf(kind), rules.WorthOf(kind))
        {
            Class = guide.Class,
            Moves = TextOf(guide.Kind.Movement.Where(rule => rule.CanMove), rules.NameOf),
            Captures = TextOf(guide.Kind.Movement.Where(rule => rule.CanCapture), rules.NameOf),
            Powers = guide.Powers,
            SpecialPowers = guide.SpecialPowers,
            PromotesTo = [.. guide.PromotesTo.Select(rules.NameOf)],
            IsPromotionPlayed = guide.IsPromotionPlayed,
            PromotesWhere = guide.PromotesWhere,
            Restrictions = guide.Restrictions,
            Mark = MarkOf(rules, kind),
            Diagram = new MoveDiagramDto(
                guide.Diagram.Side,
                guide.Diagram.Moves,
                guide.Diagram.Captures,
                guide.Diagram.Screens
            )
            {
                RiverRow = guide.Diagram.RiverRow,
            },
        };
    }

    private static ImmutableList<string> TextOf(
        IEnumerable<MovementRule> movement,
        Func<PieceKindId, string> nameOf
    ) =>
        [.. movement.Select(rule => MovementText.Of(rule, nameOf)).Distinct()];

    private static VacancyDto? VacancyOf(Vacancy? vacancy) =>
        vacancy is null
            ? null
            : new VacancyDto(
                vacancy.FirstArrival?.Value,
                vacancy.UntilFirst.TotalSeconds,
                vacancy.UntilAnyone.TotalSeconds
            );

    private static DieDto DieOf(GameSnapshot snapshot, int value) =>
        new(value, snapshot.Rules.NameOfDie(value), snapshot.Rules.MayUseDie(snapshot.State, value));

    private static AwayDto? AwayOf(AwayVoting? away) =>
        away is null
            ? null
            : new AwayDto(
                away.Absent.Value,
                away.IsOpen,
                [.. away.Voters.Select(voter => voter.Value)],
                [.. away.Voters.Where(away.Stopping.Contains).Select(voter => voter.Value)],
                away.FirstArrival?.Value,
                away.UntilAnyoneReplaces.TotalSeconds
            );

    private static EndProposalDto? ProposalOf(GameState state) =>
        state.Proposal is { } proposal
            ? new EndProposalDto(proposal.Proposer.Value, proposal.Resigns)
            : null;

    /// <summary>
    /// The enemy whose leader sits on the throne of the seat whose turn the deciding seat plays, its own or an
    /// ally's; null if none does, or the deciding seat is that enemy, sharing the turn.
    /// </summary>
    private static int? ThroneHolderOf(IRulesApplying rules, GameState state) =>
        state.DecidingSeat is { } deciding
            ? HolderOf(rules, state, deciding, state.ActingFor ?? deciding)
            : null;

    private static int? HolderOf(IRulesApplying rules, GameState state, SeatId deciding, SeatId seat) =>
        rules.ThronesOf(state)
            .Where(throne => throne.Key == seat)
            .Select(throne => state.Position.PieceAt(throne.Value))
            .OfType<Piece>()
            .Where(holder =>
                rules.IsLeader(holder.Kind)
                && !state.Teams.AreAllied(holder.Owner, seat)
                && holder.Owner != deciding
                && !state.Retired.Contains(holder.Owner)
            )
            .Select(holder => (int?)holder.Owner.Value)
            .FirstOrDefault();

    private static RulesetDto RulesetOf(IRulesApplying rules)
    {
        GameState state = rules.CreateInitialState();
        return new RulesetDto(rules.Id, rules.Name, state.Seats.Count)
        {
            Game = rules.GameId,
            GameName = rules.GameName,
            Size = rules.Size,
            IsPreview = rules.IsPreview,
            IsDealt = rules.IsDealt,
            SeatNames = [.. state.Seats.Select(seat => rules.NameOf(seat.Id))],
            SeatTeams = [.. state.Seats.Select(seat => state.Teams.TeamOf(seat.Id))],
        };
    }

    private static BotDto BotOf(BotProfile bot) =>
        new(bot.Id, bot.Name, bot.Description, bot.UsesAi, bot.IsPaid) { Ratings = bot.Ratings };

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
        new(piece.Id.Value, piece.Kind.Value, rules.NameOf(piece.Kind), piece.Owner.Value, cell.Value)
        {
            Mark = MarkOf(rules, piece.Kind),
        };

    private static PieceMarkDto? MarkOf(IRulesApplying rules, PieceKindId kind) =>
        rules.MarkOf(kind) is { } mark ? new PieceMarkDto(mark.Emblem, mark.Letters) { Class = mark.Class } : null;

    private static SeatDto SeatOf(
        GameSnapshot snapshot,
        ImmutableList<BotProfile> bots,
        SeatId seat,
        ImmutableDictionary<HumanSeat, string>? names
    )
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
                .Select(prisoner => PrisonerOf(snapshot.Rules, prisoner) with
                {
                    MayGoBack = snapshot.Rules.MayGoBack(snapshot.State, prisoner),
                })],
            botId,
            snapshot.State.Resigned.Contains(seat),
            snapshot.Rules.MayProposeEnd(snapshot.State, seat),
            snapshot.Rules.MayRejoin(snapshot.State, seat),
            snapshot.State.Absence.MissedBy(seat),
            bots.Any(bot => bot.Id == botId && bot.IsPaid),
            snapshot.State.Retired.Contains(seat),
            PersonOf(controller, names),
            controller.IsGuest,
            controller is HumanSeat { IsAi: true },
            controller is HumanSeat { Claimant: { } claimant } ? claimant.Value : null
        )
        {
            Chances = [.. snapshot.Rules.ChancesOf(snapshot.State, seat).Select(chance => ChanceOf(snapshot, chance))],
            Forward = new DirectionDto(
                snapshot.State.SeatOf(seat).Forward.First,
                snapshot.State.SeatOf(seat).Forward.Second
            ),
        };
    }

    /// <summary>A chance the seat has, whoever decides: a move one only ever takes <i>en passant</i>.</summary>
    private static ActionDto ChanceOf(GameSnapshot snapshot, GameAction chance) =>
        chance is MoveAction move
            ? ActionOf(snapshot, chance) with { Label = InPassingOf(snapshot, move) }
            : ActionOf(snapshot, chance);

    /// <summary>"En passant: your Pawn on c4 to d5, taking Yellow's Pawn on d4".</summary>
    private static string InPassingOf(GameSnapshot snapshot, MoveAction move)
    {
        IRulesApplying rules = snapshot.Rules;
        Piece mover = snapshot.State.Position.PieceAt(move.From)!; // a chance moves a piece on the board
        Piece taken = rules.Apply(snapshot.State, move).Events.OfType<PieceCaptured>().First().Piece;
        CellId cell = snapshot.State.Passages.First(passage => passage.Piece == taken).To;
        return $"En passant: your {rules.NameOf(mover.Kind)} on {rules.Topology.NameOf(move.From)} to "
            + $"{rules.Topology.NameOf(move.To)}, taking {rules.NameOf(taken.Owner)}'s {rules.NameOf(taken.Kind)} on "
            + $"{rules.Topology.NameOf(cell)}";
    }

    /// <summary>
    /// What to call whoever holds a person's seat, as <paramref name="names"/> do; <c>guest</c>, if they say
    /// nothing of it. Null for a seat no person holds.
    /// </summary>
    private static string? PersonOf(SeatController controller, ImmutableDictionary<HumanSeat, string>? names) =>
        controller is HumanSeat person
            ? (names ?? []).GetValueOrDefault(person, GameSummaryDto.Guest)
            : null;

    /// <summary>The bot's name; its id, should it no longer be on offer.</summary>
    private static string BotNameOf(ImmutableList<BotProfile> bots, string id) =>
        bots.Find(bot => bot.Id == id)?.Name ?? id;

    private static PrisonerDto PrisonerOf(IRulesApplying rules, Prisoner prisoner) =>
        new(
            prisoner.Piece.Id.Value,
            prisoner.Piece.Kind.Value,
            rules.NameOf(prisoner.Piece.Kind),
            prisoner.Piece.Owner.Value
        )
        {
            Mark = MarkOf(rules, prisoner.Piece.Kind),
        };

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
            : new OutcomeDto(
                [.. outcome.Winners.Select(seat => seat.Value)],
                outcome.Ending,
                GameLog.Story(rules, outcome)
            );

    private static ActionDto ActionOf(GameSnapshot snapshot, GameAction action)
    {
        ActionDescriptor descriptor = action.Describe();
        return new ActionDto(descriptor.Kind, descriptor.Arguments)
        {
            Label = LabelOf(snapshot, action),
            Choice = action is EscapeAction escape ? PrisonerOf(snapshot, escape) : null,
        };
    }

    /// <summary>
    /// What a button says that its kind alone cannot: which Pawn becomes what; which prisoner escapes, and
    /// where to; which ally a turn is handed to; the answers to a proposal to end the game; that passing once
    /// a turn is over means being done offering ransoms; that passing on a promotion due leaves it for
    /// later; that a move onto a piece swaps places with it rather than taking it; and what retiring means. Null
    /// for any other action.
    /// </summary>
    private static string? LabelOf(GameSnapshot snapshot, GameAction action) =>
        (action, snapshot.State.IsRansomWindow, snapshot.State.PromotionDue) switch
        {
            (PromoteAction promote, _, _) => PromotionOf(snapshot, promote),
            (PromoteOwnAction promote, _, _) => OwnPromotionOf(snapshot, promote),
            (EscapeAction escape, _, _) => EscapeOf(snapshot, escape),
            (HandOverAction handOver, _, _) => $"Let {snapshot.Rules.NameOf(handOver.Regent)} take over",
            (AnswerEndAction { Agrees: true }, _, _) => "Agree to end the game",
            (AnswerEndAction, _, _) => "Keep playing",
            (PassAction, true, _) => "Done offering",
            (PassAction, _, { }) => "Promote later",
            (MoveAction move, _, _) => SwapOf(snapshot, move),
            (RetireAction, _, _) => "Retire from the game: taking your Raja will score no one",
            (FaceOffAction faceOff, _, _) => FaceOffOf(snapshot, faceOff),
            _ => null,
        };

    /// <summary>"Face-off: your General (Janggi) on e1 and Yellow's Raja on e8, each taken by the other's side".
    /// </summary>
    private static string FaceOffOf(GameSnapshot snapshot, FaceOffAction faceOff)
    {
        IRulesApplying rules = snapshot.Rules;
        Piece caller = snapshot.State.Position.PieceAt(faceOff.Caller)!; // a chance names leaders on the board
        Piece faced = snapshot.State.Position.PieceAt(faceOff.Faced)!;
        return $"Face-off: your {rules.NameOf(caller.Kind)} on {rules.Topology.NameOf(faceOff.Caller)} and "
            + $"{rules.NameOf(faced.Owner)}'s {rules.NameOf(faced.Kind)} on {rules.Topology.NameOf(faceOff.Faced)}, "
            + "each taken by the other's side";
    }

    /// <summary>"Swap places with Green's Pawn", if the move swaps with the piece it goes to; the rules say.</summary>
    private static string? SwapOf(GameSnapshot snapshot, MoveAction move) =>
        snapshot.State.Position.PieceAt(move.To) is { } other
        && snapshot.Rules.Apply(snapshot.State, move).Events[0] is PawnsSwapped
            ? $"Swap places with {snapshot.Rules.NameOf(other.Owner)}'s {snapshot.Rules.NameOf(other.Kind)}"
            : null;

    /// <summary>"Escape Green's Raja, held by Red, to h4".</summary>
    private static string EscapeOf(GameSnapshot snapshot, EscapeAction escape) =>
        $"{PrisonerOf(snapshot, escape)}, to {snapshot.Rules.Topology.NameOf(escape.Home)}";

    /// <summary>"Escape Green's Raja, held by Red": the escape, wherever to.</summary>
    private static string PrisonerOf(GameSnapshot snapshot, EscapeAction escape)
    {
        IRulesApplying rules = snapshot.Rules;
        Prisoner prisoner = snapshot.State.Prisoners.First(held => held.Piece.Id == escape.Piece);
        return $"Escape {rules.NameOf(prisoner.Piece.Owner)}'s {rules.NameOf(prisoner.Piece.Kind)}, "
            + $"held by {rules.NameOf(prisoner.Holder)}";
    }

    /// <summary>"Promote the Pawn (Western) on c8 to a Queen (Western)", by its own promotion.</summary>
    private static string OwnPromotionOf(GameSnapshot snapshot, PromoteOwnAction promote)
    {
        IRulesApplying rules = snapshot.Rules;
        Piece piece = snapshot.State.Position.PieceAt(promote.Cell)!; // a legal promotion has a piece to promote
        return $"Promote the {rules.NameOf(piece.Kind)} on {rules.Topology.NameOf(promote.Cell)} "
            + $"to a {rules.NameOf(promote.Becomes)}";
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
