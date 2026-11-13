using System.Collections.Immutable;
using Net.NowhereAtAll.Zaturnava.Engine.Actions;
using Net.NowhereAtAll.Zaturnava.Engine.Boards;
using Net.NowhereAtAll.Zaturnava.Engine.Chance;
using Net.NowhereAtAll.Zaturnava.Engine.Events;
using Net.NowhereAtAll.Zaturnava.Engine.Pieces;
using Net.NowhereAtAll.Zaturnava.Engine.Positions;
using Net.NowhereAtAll.Zaturnava.Engine.Rules;
using Net.NowhereAtAll.Zaturnava.Engine.Seats;
using Net.NowhereAtAll.Zaturnava.Topology.SquareGrid;

namespace Net.NowhereAtAll.Zaturnava.Games.Chaturaji2027;

/// <summary>
/// The players drafting their armies before play, as <see cref="Drafting"/> says, in turn and in view of each other
/// (Proposed): each seat in turn places its palace, if the game has palaces, and sets its throne on a square of it (of
/// its kingdom, if not), then, a turn at a time, buys a piece at its worth and places it on a free square of its
/// kingdom, until it is done, keeping what is left, or has the rest of its army filled at random, which leaves it done
/// too. Once every seat is done, where the pieces stand is where the
/// game starts, what each seat kept starts its score, and the seats roll for who goes first. Drafted all at once
/// (<see cref="ArmyDraft.IsSimultaneous"/>), any seat not done may take its next step whenever it likes, the turn
/// passing on only when the seat whose turn it is takes one; and, until it is done, it may change its mind: take back
/// a piece, its price refunded, or move its throne or palace (Stated, 2026-11-13).
/// </summary>
internal sealed class ArmyDrafting
{
    private static readonly PieceKindId Square = new("kingdom-square");

    private readonly Drafting _drafting;
    /// <summary>What a kind costs in a game of an edition (<see cref="GameState.Edition"/>).</summary>
    private readonly Func<int, PieceKindId, int> _priceOf;
    private readonly DiceRequest _rollOff;
    private readonly ImmutableDictionary<SeatId, ImmutableDictionary<CellId, ArmySquare>> _kingdoms;

    /// <summary>Each seat's kingdom, and every square a palace of its may stand on outside it, by cell.</summary>
    private readonly ImmutableDictionary<SeatId, ImmutableDictionary<CellId, ArmySquare>> _squares;

    /// <summary>Each seat's kingdom, and every square a palace of its may stand on outside it, by square.</summary>
    private readonly ImmutableDictionary<SeatId, ImmutableDictionary<ArmySquare, CellId>> _cells;

    /// <summary>Every square of the kingdom a palace may stand about: one whose 3 × 3 is all in the kingdom.</summary>
    private readonly ImmutableList<ArmySquare> _centres;

    /// <summary>
    /// Every square of the kingdom a palace may stand about, if it may stand partly outside a kingdom too small for it
    /// (<see cref="PalaceSquares.EdgeCentres(ImmutableList{ArmySquare}, int)"/>).
    /// </summary>
    private readonly ImmutableList<ArmySquare> _edgeCentres;

    public ArmyDrafting(
        Drafting drafting,
        SquareGridTopology grid,
        int side,
        Func<int, PieceKindId, int> priceOf,
        DiceRequest rollOff
    )
    {
        this._drafting = drafting;
        this._priceOf = priceOf;
        this._rollOff = rollOff;
        this.Limit = drafting.Kingdom.Count
            * drafting.Terrains
                .SelectMany(terrain => drafting.Offered(terrain, int.MaxValue))
                .Select(kind => priceOf(int.MaxValue, kind))
                .DefaultIfEmpty()
                .Max();
        this._kingdoms = Enumerable.Range(0, Pinwheel.SeatCount).ToImmutableDictionary(
            seat => new SeatId(seat),
            seat => CellsOf(grid, side, seat, drafting.Kingdom)
        );
        this._centres = PalaceSquares.Centres(drafting.Kingdom);
        this._edgeCentres = PalaceSquares.EdgeCentres(drafting.Kingdom, side);
        ImmutableList<ArmySquare> reached =
            [.. drafting.Kingdom.Union(this._edgeCentres.SelectMany(PalaceSquares.Window))];
        this._squares = Enumerable.Range(0, Pinwheel.SeatCount).ToImmutableDictionary(
            seat => new SeatId(seat),
            seat => CellsOf(grid, side, seat, reached)
        );
        this._cells = this._squares.ToImmutableDictionary(
            kingdom => kingdom.Key,
            kingdom => kingdom.Value.ToImmutableDictionary(entry => entry.Value, entry => entry.Key)
        );
    }

    /// <summary>Where each of <paramref name="squares"/>, as seat 0 sees them, is for the seat.</summary>
    private static ImmutableDictionary<CellId, ArmySquare> CellsOf(
        SquareGridTopology grid,
        int side,
        int seat,
        ImmutableList<ArmySquare> squares
    )
    {
        ImmutableList<ArmyPiece> placed =
            [.. squares.Select(square => new ArmyPiece(Square, square.Column, square.Row))];
        return Pinwheel
            .EachOwn(side, [.. Enumerable.Range(0, seat + 1).Select(own => own == seat ? placed : [])])
            .Zip(squares)
            .ToImmutableDictionary(
                turned => grid.CellAt(turned.First.Column, turned.First.Row),
                turned => turned.Second
            );
    }

    /// <summary>
    /// Whether each army has a palace (specification §5): a 3 × 3 of its kingdom, holding its throne. Only a kingdom
    /// with room for one has (Proposed); one too small (Chaturaji 2027's 4 × 2) has one standing partly outside it, if
    /// <paramref name="atEdge"/> and it may (<see cref="PalaceSquares.EdgeCentres(ImmutableList{ArmySquare}, int)"/>),
    /// else none.
    /// </summary>
    public bool HasPalaces(bool atEdge) => !this.Centres(atEdge).IsEmpty;

    /// <summary>
    /// Each seat's palace about its throne in <paramref name="thrones"/> (<see cref="Around"/>), standing partly
    /// outside a kingdom too small for one, if <paramref name="atEdge"/>.
    /// </summary>
    public ImmutableDictionary<SeatId, ImmutableList<CellId>> PalacesAround(
        ImmutableDictionary<SeatId, CellId> thrones,
        bool atEdge
    ) =>
        thrones.ToImmutableDictionary(
            throne => throne.Key,
            throne => this.Around(throne.Key, this._kingdoms[throne.Key][throne.Value], this.Centres(atEdge))
        );

    private ImmutableList<ArmySquare> Centres(bool atEdge) => atEdge ? this._edgeCentres : this._centres;

    /// <summary>The squares a palace may stand about in the game.</summary>
    private ImmutableList<ArmySquare> CentresIn(GameState state) => this.Centres(state.HasEdgePalaces);

    /// <summary>
    /// The seat's palace holding <paramref name="throne"/>, about one of <paramref name="centres"/>, as
    /// <see cref="PalaceSquares"/> finds it.
    /// </summary>
    private ImmutableList<CellId> Around(SeatId seat, ArmySquare throne, ImmutableList<ArmySquare> centres) =>
        this.PalaceOf(seat, PalaceSquares.CentreAbout(centres, throne));

    /// <summary>The seat's palace about <paramref name="centre"/>: its cells, in order.</summary>
    private ImmutableList<CellId> PalaceOf(SeatId seat, ArmySquare centre) =>
        [.. PalaceSquares.Window(centre).Select(square => this._cells[seat][square]).OrderBy(cell => cell.Value)];

    /// <summary>The centre of the seat's <paramref name="palace"/>, as seat 0 sees its kingdom.</summary>
    private ArmySquare CentreOf(SeatId seat, ImmutableList<CellId> palace)
    {
        ImmutableList<ArmySquare> squares = [.. palace.Select(cell => this._squares[seat][cell])];
        return new ArmySquare(squares.Min(square => square.Column) + 1, squares.Min(square => square.Row) + 1);
    }

    /// <summary>
    /// The most any army may have to spend (Stated, 2026-11-12): every square of its kingdom filled with the dearest
    /// kind offered on any terrain, so that a game's points are earned in play, none given away unspendable.
    /// </summary>
    public int Limit { get; }

    /// <summary>
    /// The game <paramref name="dealt"/> (its terrain), its board emptied for its armies to be drafted, every seat with
    /// the whole budget to spend, the settings' or the variant's, but never above <see cref="Limit"/>; the first seat
    /// to draft first; any army filled at random drawn by <paramref name="seed"/>. If the settings name a drafter, it
    /// alone drafts, the others done already.
    /// </summary>
    public GameState Begun(GameState dealt, int seed, DraftSettings settings) =>
        dealt with
        {
            Position = Position.Empty,
            Thrones = [],
            Palaces = dealt.Palaces is null ? null : this.SolePalaces(dealt),
            Start = null,
            Draft = new ArmyDraft(
                seed,
                dealt.Seats.ToImmutableDictionary(
                    seat => seat.Id,
                    _ => Math.Min(settings.Budget ?? this._drafting.BudgetIn(dealt.Edition), this.Limit)
                ),
                settings.Drafter is { } drafter
                    ? [.. dealt.Seats.Select(seat => seat.Id).Where(seat => seat != drafter)]
                    : []
            )
            {
                Drafter = settings.Drafter,
                IsSimultaneous = settings is { IsSimultaneous: true, Drafter: null },
            },
            DecidingSeat = settings.Drafter ?? dealt.Seats[0].Id,
            RollOff = [],
            AwaitedDice = null,
        };

    /// <summary>
    /// Every seat's palace, where its kingdom has room for only one (Proposed: nothing to choose, so not asked); else
    /// none yet, each to be placed in the draft.
    /// </summary>
    private ImmutableDictionary<SeatId, ImmutableList<CellId>> SolePalaces(GameState dealt) =>
        this.CentresIn(dealt) is [var sole]
            ? dealt.Seats.ToImmutableDictionary(seat => seat.Id, seat => this.PalaceOf(seat.Id, sole))
            : [];

    /// <summary>
    /// What <paramref name="seat"/> may do in its turn of the draft: in a game with palaces, place its palace about a
    /// square of its kingdom, if it has none; set its throne on a square of its palace, or of its kingdom where there
    /// are none, if it has no throne; else buy any kind offered that it can pay for, on any free square of its
    /// kingdom, or be done; and, drafting all at once, take back any piece it drafted, or move its throne or palace
    /// (<see cref="ChangesOf"/>). Whichever, it may have the rest filled at random. Nothing, once it is done.
    /// </summary>
    public ImmutableList<GameAction> ChoicesOf(GameState state, SeatId seat)
    {
        ImmutableList<CellId> kingdom = [.. this._kingdoms[seat].Keys.OrderBy(cell => cell.Value)];
        return (state.Draft!.Done.Contains(seat), state.Thrones!.ContainsKey(seat), state.Palaces) switch
        {
            (true, _, _) => [],
            (_, true, _) => this.BuyingOf(state, seat, kingdom),
            (_, _, { } palaces) when !palaces.ContainsKey(seat) =>
            [
                .. this.CentresIn(state)
                    .Select(centre => this._cells[seat][centre])
                    .OrderBy(cell => cell.Value)
                    .Select(cell => (GameAction)new PalaceAction(seat, cell)),
                new FillArmyAction(seat),
            ],
            var (_, _, palaces) =>
            [
                .. (palaces?[seat] ?? kingdom)
                    .Where(this._kingdoms[seat].ContainsKey)
                    .Select(cell => (GameAction)new ThroneAction(seat, cell)),
                new FillArmyAction(seat),
            ],
        };
    }

    /// <summary>
    /// Every kind offered that the seat can pay for, on any free square of its kingdom; a kind a palace holds, on a
    /// free square of its palace only.
    /// </summary>
    private ImmutableList<GameAction> BuyingOf(GameState state, SeatId seat, ImmutableList<CellId> kingdom)
    {
        ImmutableList<CellId> free = [.. kingdom.Where(cell => state.Position.PieceAt(cell) is null)];
        ImmutableList<CellId> palace = state.Palaces?.GetValueOrDefault(seat) ?? [];
        ImmutableList<CellId> freeInPalace = [.. free.Where(palace.Contains)];
        int left = state.Draft!.Left[seat];
        return
        [
            .. this.Offered(state)
                .Where(kind => this._priceOf(state.Edition, kind) <= left)
                .SelectMany(kind => (this._drafting.PalaceBound.Contains(kind) ? freeInPalace : free)
                    .Select(cell => (GameAction)new DraftAction(seat, kind, cell))
                ),
            .. state.Draft.IsSimultaneous ? this.ChangesOf(state, seat, kingdom) : [],
            new FillArmyAction(seat),
            new PassAction(seat),
        ];
    }

    /// <summary>
    /// How the seat, drafting all at once, may change its mind: take back any piece it drafted; move its throne to any
    /// other square it might have set it on; move its palace about any other square, if the palace there would still
    /// hold its throne and every piece of its that a palace holds.
    /// </summary>
    private ImmutableList<GameAction> ChangesOf(GameState state, SeatId seat, ImmutableList<CellId> kingdom)
    {
        ImmutableList<CellId> own = [.. kingdom.Where(cell => state.Position.PieceAt(cell) is not null)];
        CellId throne = state.Thrones![seat];
        ImmutableList<CellId>? palace = state.Palaces?.GetValueOrDefault(seat);
        ImmutableList<CellId> held =
        [
            .. own.Where(cell => this._drafting.PalaceBound.Contains(state.Position.PieceAt(cell)!.Kind)),
            throne,
        ];
        return
        [
            .. own.Select(cell => (GameAction)new UndraftAction(seat, cell)),
            .. (palace ?? kingdom)
                .Where(cell => this._kingdoms[seat].ContainsKey(cell) && cell != throne)
                .Select(cell => (GameAction)new ThroneAction(seat, cell)),
            .. palace is null
                ? []
                : this.CentresIn(state)
                    .Select(centre => this._cells[seat][centre])
                    .OrderBy(cell => cell.Value)
                    .Where(centre =>
                    {
                        ImmutableList<CellId> moved = this.PalaceOf(seat, this._kingdoms[seat][centre]);
                        return !moved.SequenceEqual(palace) && held.All(moved.Contains);
                    })
                    .Select(centre => (GameAction)new PalaceAction(seat, centre)),
        ];
    }

    /// <summary>
    /// Every kind offered on the game's terrain, and, in a game with palaces, those a palace holds; dearest first, then
    /// by key.
    /// </summary>
    public ImmutableList<PieceKindId> Offered(GameState state) =>
    [
        .. this._drafting.Offered(state.Terrain, state.Edition)
            .Union(state.Palaces is null ? [] : this._drafting.PalaceBound)
            .OrderByDescending(kind => this._priceOf(state.Edition, kind))
            .ThenBy(kind => kind.Value, StringComparer.Ordinal),
    ];

    /// <summary>
    /// The action applied, telling what the seat did: placed its palace, set its throne, bought a piece, had its army
    /// filled at random; that it is done, if it is, and what it kept; and, once one seat has drafted for all, that the
    /// others are given its army.
    /// </summary>
    /// <exception cref="IllegalActionException">
    /// The action is not among the seat's choices, or not the deciding seat's in a draft taken in turn.
    /// </exception>
    public Transition Applied(GameState state, GameAction action)
    {
        SeatId seat = action.Actor;
        (GameState acted, ImmutableList<GameEvent> told) =
            (seat == state.DecidingSeat!.Value || state.Draft!.IsSimultaneous)
            && this.ChoicesOf(state, seat).Contains(action)
            ? action switch
            {
                PalaceAction palace => Told(
                    state with
                    {
                        Palaces = state.Palaces!.SetItem(seat, this.PalaceOf(seat, this._kingdoms[seat][palace.Cell])),
                    },
                    new PalacePlaced(seat, palace.Cell)
                ),
                ThroneAction throne => Told(
                    state with { Thrones = state.Thrones!.SetItem(seat, throne.Cell) },
                    new ThroneSet(seat, throne.Cell)
                ),
                DraftAction draft => Told(
                    this.DoneIfSpent(this.Bought(state, seat, draft.Piece, draft.Cell), seat),
                    new PieceDrafted(seat, draft.Piece, draft.Cell, this._priceOf(state.Edition, draft.Piece))
                ),
                UndraftAction undraft => this.RefundedAndTold(state, seat, undraft.Cell),
                FillArmyAction => this.FilledAndTold(state, seat),
                _ => Told(Done(state, seat)),
            }
            : throw new IllegalActionException(action);
        GameState next = seat == state.DecidingSeat!.Value ? this.Next(acted, seat) : acted;
        ImmutableList<GameEvent> finished = acted.Draft!.Done.Contains(seat)
            ? told.Add(new DraftFinished(seat, acted.Draft.Left[seat]))
            : told;
        return new Transition(
            next,
            next.Draft is null && state.Draft!.Drafter is { } drafter
                ? finished.Add(new ArmiesCopied(drafter))
                : finished
        );
    }

    private static (GameState State, ImmutableList<GameEvent> Told) Told(
        GameState state,
        params ImmutableList<GameEvent> told
    ) =>
        (state, told);

    /// <summary>The seat's army filled at random, and done, telling how many pieces it gained.</summary>
    private (GameState State, ImmutableList<GameEvent> Told) FilledAndTold(GameState state, SeatId seat)
    {
        GameState filled = this.Filled(state, seat);
        int added = filled.Position.Occupants.Count - state.Position.Occupants.Count;
        return Told(Done(filled, seat), new ArmyFilledAtRandom(seat, added));
    }

    /// <summary>The piece on <paramref name="cell"/> taken back, its price refunded, telling so.</summary>
    private (GameState State, ImmutableList<GameEvent> Told) RefundedAndTold(GameState state, SeatId seat, CellId cell)
    {
        PieceKindId kind = state.Position.PieceAt(cell)!.Kind;
        int price = this._priceOf(state.Edition, kind);
        return Told(
            state with
            {
                Position = new Position(state.Position.Occupants.Remove(cell)),
                Draft = state.Draft! with { Left = state.Draft.Left.SetItem(seat, state.Draft.Left[seat] + price) },
            },
            new PieceUndrafted(seat, kind, cell, price)
        );
    }

    /// <summary>
    /// The seat done, once it can buy nothing more, for want of points or of a free square: it has no choice left
    /// to make (Stated, 2026-11-12). Drafting all at once, it may still change its mind, so it is done only once it
    /// says so.
    /// </summary>
    private GameState DoneIfSpent(GameState state, SeatId seat) =>
        state.Draft!.IsSimultaneous || this.ChoicesOf(state, seat).OfType<DraftAction>().Any()
            ? state
            : Done(state, seat);

    private static GameState Done(GameState state, SeatId seat) =>
        state with { Draft = state.Draft! with { Done = state.Draft.Done.Add(seat) } };

    /// <summary>
    /// The id a new piece takes: one past the highest of any on the board, so that none is taken twice once a piece
    /// is taken back; as many as there are, while none has been (every id below it taken, in order).
    /// </summary>
    private static PieceId NextId(Position position) =>
        new(position.Occupants.Values.Select(piece => piece.Id.Value).DefaultIfEmpty(-1).Max() + 1);

    /// <summary>The piece bought and placed, its price spent.</summary>
    private GameState Bought(GameState state, SeatId seat, PieceKindId kind, CellId cell) =>
        state with
        {
            Position = state.Position.Place(cell, new Piece(NextId(state.Position), kind, seat)),
            Draft = state.Draft! with
            {
                Left = state.Draft.Left.SetItem(seat, state.Draft.Left[seat] - this._priceOf(state.Edition, kind)),
            },
        };

    /// <summary>
    /// The rest of the seat's army filled at random, its throne set if it was not: in the middle of its palace, if it
    /// has one (Proposed); and its palace about its throne, if it has none.
    /// </summary>
    private GameState Filled(GameState state, SeatId seat)
    {
        ImmutableDictionary<CellId, ArmySquare> kingdom = this._kingdoms[seat];
        ImmutableDictionary<ArmySquare, CellId> cells = this._cells[seat];
        ImmutableList<CellId>? placed = state.Palaces?.GetValueOrDefault(seat);
        ImmutableList<ArmyPiece> army =
        [
            .. kingdom
                .Select(entry => (Square: entry.Value, Cell: entry.Key, Piece: state.Position.PieceAt(entry.Key)))
                .Where(entry => entry.Piece is not null)
                .OrderBy(entry => entry.Cell.Value)
                .Select(entry => new ArmyPiece(entry.Piece!.Kind, entry.Square.Column, entry.Square.Row)),
        ];
        ArmySquare? throne = state.Thrones!.TryGetValue(seat, out CellId set)
            ? kingdom[set]
            : placed is { } palace
                ? this.CentreOf(seat, palace)
                : null;
        Random random = new(state.Draft!.Seed + (seat.Value * Pinwheel.SeatCount) + army.Count);
        ArmyFilled filled = this._drafting.Fill(
            new ArmyFill(state.Terrain, state.Draft.Left[seat], army, throne, random)
            {
                Palace = placed is { } own ? [.. own.Where(kingdom.ContainsKey).Select(cell => kingdom[cell])] : null,
                HasPalacePieces = state.HasPalacePieces,
                HasEdgePalaces = state.HasEdgePalaces,
                Edition = state.Edition,
            }
        );
        GameState thrones = state with
        {
            Thrones = state.Thrones.SetItem(seat, cells[filled.Throne]),
            Palaces = state.Palaces is { } palaces && !palaces.ContainsKey(seat)
                ? palaces.SetItem(seat, this.Around(seat, filled.Throne, this.CentresIn(state)))
                : state.Palaces,
        };
        return filled.Added.Aggregate(
            thrones,
            (bought, piece) => this.Bought(bought, seat, piece.Kind, cells[new ArmySquare(piece.Column, piece.Row)])
        );
    }

    /// <summary>The next seat after <paramref name="seat"/> still drafting; or, if none is, the game begun.</summary>
    private GameState Next(GameState state, SeatId seat)
    {
        ImmutableList<Seat> seats = state.Seats;
        int at = seats.FindIndex(each => each.Id == seat);
        SeatId? next = Enumerable.Range(1, seats.Count)
            .Select(step => seats[(at + step) % seats.Count].Id)
            .Where(each => !state.Draft!.Done.Contains(each))
            .Select(each => (SeatId?)each)
            .FirstOrDefault();
        return next is { } drafting ? state with { DecidingSeat = drafting } : this.Started(this.Copied(state));
    }

    /// <summary>
    /// If one seat drafted for all, its army, throne and what it kept, given alike to every other seat, each in its
    /// own kingdom; else the armies as drafted.
    /// </summary>
    private GameState Copied(GameState drafted)
    {
        if (drafted.Draft!.Drafter is not { } drafter)
        {
            return drafted;
        }

        ImmutableDictionary<CellId, ArmySquare> own = this._kingdoms[drafter];
        ImmutableList<(ArmySquare Square, PieceKindId Kind)> army =
        [
            .. own.OrderBy(entry => entry.Key.Value)
                .Where(entry => drafted.Position.PieceAt(entry.Key) is not null)
                .Select(entry => (entry.Value, drafted.Position.PieceAt(entry.Key)!.Kind)),
        ];
        ArmySquare throne = own[drafted.Thrones![drafter]];
        ArmySquare? centre = drafted.Palaces is { } palaces ? this.CentreOf(drafter, palaces[drafter]) : null;
        int left = drafted.Draft.Left[drafter];
        return drafted.Seats
            .Select(seat => seat.Id)
            .Where(seat => seat != drafter)
            .Aggregate(drafted, (copied, seat) => this.Given(copied, seat, army, (throne, centre), left));
    }

    /// <summary>
    /// <paramref name="seat"/> given the army, its throne and palace, and what was kept, in its own kingdom.
    /// </summary>
    private GameState Given(
        GameState state,
        SeatId seat,
        ImmutableList<(ArmySquare Square, PieceKindId Kind)> army,
        (ArmySquare Throne, ArmySquare? Centre) home,
        int left
    )
    {
        ImmutableDictionary<ArmySquare, CellId> cells = this._cells[seat];
        return army.Aggregate(
            state with
            {
                Thrones = state.Thrones!.SetItem(seat, cells[home.Throne]),
                Palaces = home.Centre is { } centre
                    ? state.Palaces!.SetItem(seat, this.PalaceOf(seat, centre))
                    : state.Palaces,
                Draft = state.Draft! with { Left = state.Draft.Left.SetItem(seat, left) },
            },
            (given, piece) => given with
            {
                Position = given.Position.Place(
                    cells[piece.Square],
                    new Piece(NextId(given.Position), piece.Kind, seat)
                ),
            }
        );
    }

    /// <summary>
    /// The game begun from the armies drafted: where they stand, where it starts; what each seat kept, its score, as
    /// unspent, earned by no one; and the seats rolling for who goes first.
    /// </summary>
    private GameState Started(GameState drafted) =>
        drafted with
        {
            Start = drafted.Position,
            Scores = drafted.Scores.SetItems(
                drafted.Draft!.Left.Select(left => KeyValuePair.Create(left.Key, drafted.Scores[left.Key] + left.Value))
            ),
            Unspent = drafted.Draft.Left,
            Draft = null,
            DecidingSeat = drafted.Seats[0].Id,
            RollOff = [.. drafted.Seats.Select(seat => seat.Id)],
            AwaitedDice = this._rollOff,
        };
}
