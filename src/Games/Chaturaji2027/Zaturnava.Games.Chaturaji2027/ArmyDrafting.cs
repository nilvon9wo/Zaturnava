using System.Collections.Immutable;
using Net.NowhereAtAll.Zaturnava.Engine.Actions;
using Net.NowhereAtAll.Zaturnava.Engine.Boards;
using Net.NowhereAtAll.Zaturnava.Engine.Chance;
using Net.NowhereAtAll.Zaturnava.Engine.Pieces;
using Net.NowhereAtAll.Zaturnava.Engine.Positions;
using Net.NowhereAtAll.Zaturnava.Engine.Rules;
using Net.NowhereAtAll.Zaturnava.Engine.Seats;
using Net.NowhereAtAll.Zaturnava.Topology.SquareGrid;

namespace Net.NowhereAtAll.Zaturnava.Games.Chaturaji2027;

/// <summary>
/// The players drafting their armies before play, as <see cref="Drafting"/> says, in turn and in view of each other
/// (Proposed): each seat in turn sets its throne on a square of its kingdom, then, a turn at a time, buys a piece at
/// its worth and places it on a free square of its kingdom, until it is done, keeping what is left, or has the rest of
/// its army filled at random, which leaves it done too. Once every seat is done, where the pieces stand is where the
/// game starts, what each seat kept starts its score, and the seats roll for who goes first.
/// </summary>
internal sealed class ArmyDrafting
{
    private static readonly PieceKindId Square = new("kingdom-square");

    private readonly Drafting _drafting;
    private readonly Func<PieceKindId, int> _priceOf;
    private readonly DiceRequest _rollOff;
    private readonly ImmutableDictionary<SeatId, ImmutableDictionary<CellId, ArmySquare>> _kingdoms;

    public ArmyDrafting(
        Drafting drafting,
        SquareGridTopology grid,
        int side,
        Func<PieceKindId, int> priceOf,
        DiceRequest rollOff
    )
    {
        this._drafting = drafting;
        this._priceOf = priceOf;
        this._rollOff = rollOff;
        this.Limit = drafting.Kingdom.Count
            * drafting.Terrains.SelectMany(drafting.Offered).Select(priceOf).DefaultIfEmpty().Max();
        ImmutableList<ArmyPiece> squares =
            [.. drafting.Kingdom.Select(square => new ArmyPiece(Square, square.Column, square.Row))];
        this._kingdoms = Enumerable.Range(0, Pinwheel.SeatCount).ToImmutableDictionary(
            seat => new SeatId(seat),
            seat => Pinwheel
                .EachOwn(side, [.. Enumerable.Range(0, seat + 1).Select(own => own == seat ? squares : [])])
                .Zip(drafting.Kingdom)
                .ToImmutableDictionary(
                    placed => grid.CellAt(placed.First.Column, placed.First.Row),
                    placed => placed.Second
                )
        );
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
            Start = null,
            Draft = new ArmyDraft(
                seed,
                dealt.Seats.ToImmutableDictionary(
                    seat => seat.Id,
                    _ => Math.Min(settings.Budget ?? this._drafting.Budget, this.Limit)
                ),
                settings.Drafter is { } drafter
                    ? [.. dealt.Seats.Select(seat => seat.Id).Where(seat => seat != drafter)]
                    : []
            )
            {
                Drafter = settings.Drafter,
            },
            DecidingSeat = settings.Drafter ?? dealt.Seats[0].Id,
            RollOff = [],
            AwaitedDice = null,
        };

    /// <summary>
    /// What <paramref name="seat"/> may do in its turn of the draft: set its throne on a square of its kingdom, if it
    /// has none; else buy any kind offered that it can pay for, on any free square of its kingdom, or be done. Either
    /// way, it may have the rest filled at random.
    /// </summary>
    public ImmutableList<GameAction> ChoicesOf(GameState state, SeatId seat)
    {
        ImmutableList<CellId> kingdom = [.. this._kingdoms[seat].Keys.OrderBy(cell => cell.Value)];
        ImmutableList<CellId> free = [.. kingdom.Where(cell => state.Position.PieceAt(cell) is null)];
        int left = state.Draft!.Left[seat];
        return state.Thrones!.ContainsKey(seat)
            ?
            [
                .. this.Offered(state)
                    .Where(kind => this._priceOf(kind) <= left)
                    .SelectMany(kind => free.Select(cell => (GameAction)new DraftAction(seat, kind, cell))),
                new FillArmyAction(seat),
                new PassAction(seat),
            ]
            : [.. kingdom.Select(cell => (GameAction)new ThroneAction(seat, cell)), new FillArmyAction(seat)];
    }

    /// <summary>Every kind offered on the game's terrain, dearest first, then by key.</summary>
    public ImmutableList<PieceKindId> Offered(GameState state) =>
    [
        .. this._drafting.Offered(state.Terrain)
            .OrderByDescending(this._priceOf)
            .ThenBy(kind => kind.Value, StringComparer.Ordinal),
    ];

    /// <exception cref="IllegalActionException">The action is not among the seat's choices.</exception>
    public Transition Applied(GameState state, GameAction action)
    {
        SeatId seat = state.DecidingSeat!.Value;
        GameState acted = this.ChoicesOf(state, seat).Contains(action)
            ? action switch
            {
                ThroneAction throne => state with { Thrones = state.Thrones!.SetItem(seat, throne.Cell) },
                DraftAction draft => this.DoneIfSpent(this.Bought(state, seat, draft.Piece, draft.Cell), seat),
                FillArmyAction => Done(this.Filled(state, seat), seat),
                _ => Done(state, seat),
            }
            : throw new IllegalActionException(action);
        return new Transition(this.Next(acted, seat), []);
    }

    /// <summary>
    /// The seat done, once it can buy nothing more, for want of points or of a free square: it has no choice left
    /// to make (Stated, 2026-11-12).
    /// </summary>
    private GameState DoneIfSpent(GameState state, SeatId seat) =>
        this.ChoicesOf(state, seat).OfType<DraftAction>().Any() ? state : Done(state, seat);

    private static GameState Done(GameState state, SeatId seat) =>
        state with { Draft = state.Draft! with { Done = state.Draft.Done.Add(seat) } };

    /// <summary>The piece bought and placed, its price spent.</summary>
    private GameState Bought(GameState state, SeatId seat, PieceKindId kind, CellId cell) =>
        state with
        {
            Position = state.Position.Place(cell, new Piece(new PieceId(state.Position.Occupants.Count), kind, seat)),
            Draft = state.Draft! with
            {
                Left = state.Draft.Left.SetItem(seat, state.Draft.Left[seat] - this._priceOf(kind)),
            },
        };

    /// <summary>The rest of the seat's army filled at random, its throne set if it was not.</summary>
    private GameState Filled(GameState state, SeatId seat)
    {
        ImmutableDictionary<CellId, ArmySquare> kingdom = this._kingdoms[seat];
        ImmutableDictionary<ArmySquare, CellId> cells =
            kingdom.ToImmutableDictionary(entry => entry.Value, entry => entry.Key);
        ImmutableList<ArmyPiece> army =
        [
            .. kingdom
                .Select(entry => (Square: entry.Value, Cell: entry.Key, Piece: state.Position.PieceAt(entry.Key)))
                .Where(entry => entry.Piece is not null)
                .OrderBy(entry => entry.Cell.Value)
                .Select(entry => new ArmyPiece(entry.Piece!.Kind, entry.Square.Column, entry.Square.Row)),
        ];
        ArmySquare? throne = state.Thrones!.TryGetValue(seat, out CellId set) ? kingdom[set] : null;
        Random random = new(state.Draft!.Seed + (seat.Value * Pinwheel.SeatCount) + army.Count);
        ArmyFilled filled = this._drafting.Fill(
            new ArmyFill(state.Terrain, state.Draft.Left[seat], army, throne, random)
        );
        GameState thrones = state with { Thrones = state.Thrones.SetItem(seat, cells[filled.Throne]) };
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
        int left = drafted.Draft.Left[drafter];
        return drafted.Seats
            .Select(seat => seat.Id)
            .Where(seat => seat != drafter)
            .Aggregate(drafted, (copied, seat) => this.Given(copied, seat, army, throne, left));
    }

    /// <summary><paramref name="seat"/> given the army, its throne, and what was kept, in its own kingdom.</summary>
    private GameState Given(
        GameState state,
        SeatId seat,
        ImmutableList<(ArmySquare Square, PieceKindId Kind)> army,
        ArmySquare throne,
        int left
    )
    {
        ImmutableDictionary<ArmySquare, CellId> cells =
            this._kingdoms[seat].ToImmutableDictionary(entry => entry.Value, entry => entry.Key);
        return army.Aggregate(
            state with
            {
                Thrones = state.Thrones!.SetItem(seat, cells[throne]),
                Draft = state.Draft! with { Left = state.Draft.Left.SetItem(seat, left) },
            },
            (given, piece) => given with
            {
                Position = given.Position.Place(
                    cells[piece.Square],
                    new Piece(new PieceId(given.Position.Occupants.Count), piece.Kind, seat)
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
