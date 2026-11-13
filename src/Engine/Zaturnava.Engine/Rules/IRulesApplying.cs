using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using Net.NowhereAtAll.Zaturnava.Engine.Actions;
using Net.NowhereAtAll.Zaturnava.Engine.Boards;
using Net.NowhereAtAll.Zaturnava.Engine.Moves;
using Net.NowhereAtAll.Zaturnava.Engine.Pieces;
using Net.NowhereAtAll.Zaturnava.Engine.Seats;

namespace Net.NowhereAtAll.Zaturnava.Engine.Rules;

/// <summary>
/// A ruleset - one variant of the game - as a pure reducer: <c>(state, action) -> (state, events)</c>,
/// with no I/O and no clock. Board, pieces, seats, teams and every rule are the ruleset's to choose;
/// nothing outside it knows which it chose. A ruleset never rolls dice: it asks for them
/// (<see cref="GameState.AwaitedDice"/>), and whoever hosts the game rolls them and applies how they
/// came up as a <see cref="DiceAction"/>, so that a replay or a test supplies the rolls that happened.
/// </summary>
public interface IRulesApplying
{
    string Id { get; }

    string Name { get; }

    /// <summary>
    /// The game the ruleset plays, by id: one game may be played at several sizes, each its own ruleset. A ruleset
    /// that is the only one of its game is its own.
    /// </summary>
    string GameId => this.Id;

    /// <summary>What the game is called, whatever its size.</summary>
    string GameName => this.Name;

    /// <summary>The size the game is played at ("small"); none, for a game played at one size only.</summary>
    string? Size => null;

    /// <summary>
    /// Whether the game is a preview: playable, but not yet ready for the public, so a lobby hides it unless asked.
    /// </summary>
    bool IsPreview => false;

    ICellNavigating Topology { get; }

    /// <summary>
    /// The board the ruleset is played on, if a game's creator may set it to another size (<see cref="Sized"/>); none,
    /// if they may not.
    /// </summary>
    BoardSize? SizableBoard => null;

    /// <summary>
    /// The ruleset played on a board of <paramref name="board"/>'s size, of the same id, so that its games are listed
    /// and rated together; none, if it may not be set to that size, or to any (<see cref="SizableBoard"/>).
    /// </summary>
    IRulesApplying? Sized([Required] BoardSize board) => null;

    /// <summary>Each seat's throne, if the ruleset has thrones; empty if it has none.</summary>
    ImmutableDictionary<SeatId, CellId> Thrones { get; }

    /// <summary>
    /// Each seat's throne in the game as it stands: the game's own, if it was dealt; else the ruleset's.
    /// </summary>
    ImmutableDictionary<SeatId, CellId> ThronesOf([Required] GameState state) => state.Thrones ?? this.Thrones;

    GameState CreateInitialState();

    /// <summary>
    /// Whether each game is dealt anew as it begins (<see cref="Deal"/>): its armies picked and placed at random,
    /// say, so that no two games begin alike. A ruleset that is not begins every game as
    /// <see cref="CreateInitialState"/> does.
    /// </summary>
    bool IsDealt => false;

    /// <summary>
    /// The new game <paramref name="initial"/>, dealt by <paramref name="seed"/>: the same seed always deals the same
    /// game, so that a game replays from the seed it began with; every army alike, laid out alike, if
    /// <paramref name="areAlike"/>; its terrain too (<see cref="GameState.Terrain"/>: its rivers, say), if
    /// <paramref name="dealsTerrain"/>, which a game dealt before terrain was never does; and its palaces
    /// (<see cref="GameState.Palaces"/>), if <paramref name="dealsPalaces"/>, which one dealt before palaces were never
    /// does; with the pieces a palace holds in its armies (<see cref="GameState.HasPalacePieces"/>), if
    /// <paramref name="dealsPalacePieces"/> too, which one dealt before they could be never does; and, where a kingdom
    /// is too small for a palace, one standing partly outside it (<see cref="GameState.HasEdgePalaces"/>), if
    /// <paramref name="dealsEdgePalaces"/> too, which one dealt before they could never does; its armies drawn from
    /// the ruleset's pieces as they were at <paramref name="edition"/> (<see cref="GameState.Edition"/>), which is 0
    /// for one dealt before there was another. As it is, for a ruleset that is not dealt.
    /// </summary>
    GameState Deal(
        [Required] GameState initial,
        int seed,
        bool areAlike = false,
        bool dealsTerrain = false,
        bool dealsPalaces = false,
        bool dealsPalacePieces = false,
        bool dealsEdgePalaces = false,
        int edition = 0
    ) =>
        initial;

    /// <summary>
    /// Whether a game's players may draft their own armies (<see cref="Drafted"/>), rather than have them dealt. None
    /// may, unless the ruleset says so.
    /// </summary>
    bool IsDraftable => false;

    /// <summary>
    /// What each seat has to spend on its army, if its players draft it, unless the game's creator sets another
    /// (<see cref="DraftSettings.Budget"/>); none, for a ruleset whose players may not draft.
    /// </summary>
    int? DraftBudget => null;

    /// <summary>
    /// The most each seat may have to spend on its army, if its players draft it, whatever the game's creator sets:
    /// a budget above it is held to it; none, for a ruleset whose players may not draft.
    /// </summary>
    int? DraftBudgetLimit => null;

    /// <summary>
    /// The game <paramref name="dealt"/> (its terrain, say), but with its armies to be drafted by its players before
    /// play begins (<see cref="GameState.Draft"/>), as <paramref name="settings"/> set
    /// (<see cref="DraftSettings.Default"/>, if none), any army filled at random drafted by <paramref name="seed"/>. As
    /// it is, for a ruleset whose players may not.
    /// </summary>
    GameState Drafted([Required] GameState dealt, int seed, DraftSettings? settings = null) => dealt;

    /// <summary>
    /// Every kind a player may draft, while the armies are drafted, whether or not they can pay for it yet: the
    /// catalogue they choose from. None, at any other time, or for a ruleset whose players may not draft.
    /// </summary>
    ImmutableList<PieceKindId> DraftOffered([Required] GameState state) => [];

    /// <summary>
    /// The lines drawn on the board in the game as it stands, such as its rivers; none, unless the ruleset says
    /// otherwise.
    /// </summary>
    ImmutableList<BoardLine> LinesOf([Required] GameState state) => [];

    /// <summary>
    /// What a player should know of the board in the game as it stands beyond its cells and lines: "A river runs
    /// north to south, around the kingdoms"; null, if nothing.
    /// </summary>
    string? TerrainOf([Required] GameState state) => null;

    /// <summary>What the deciding seat may do; empty once the game is over, and while it waits on dice.</summary>
    ImmutableList<GameAction> LegalActions([Required] GameState state);

    /// <exception cref="IllegalActionException">The action is not among the legal actions.</exception>
    Transition Apply([Required] GameState state, [Required] GameAction action);

    /// <summary>
    /// Whether the deciding seat may offer a ransom now. Offers are not among the legal actions:
    /// there are too many; <see cref="Apply"/> refuses one that may not be made.
    /// </summary>
    bool MayOffer([Required] GameState state);

    /// <summary>Whether the prisoner may be given back now, in a ransom or released.</summary>
    bool MayGoBack([Required] GameState state, [Required] Prisoner prisoner);

    /// <summary>
    /// Whether <paramref name="seat"/> may propose ending the game now. A proposal may come from any seat,
    /// whoever decides: <see cref="Apply"/> takes one from a seat this allows.
    /// </summary>
    bool MayProposeEnd([Required] GameState state, SeatId seat);

    /// <summary>
    /// Whether <paramref name="seat"/>, having given up its turns, may take them back now. Like a proposal,
    /// it may, whoever decides: <see cref="Apply"/> takes a rejoining from a seat this allows.
    /// </summary>
    bool MayRejoin([Required] GameState state, SeatId seat);

    /// <summary>
    /// Whether <paramref name="voter"/> may vote whether to stop waiting for <paramref name="absent"/>, both
    /// still playing. Like a proposal, a vote is cast whoever decides; <see cref="Apply"/> takes one this
    /// allows. When to ask for votes, and whose, is the host's to say: it knows who is a person.
    /// </summary>
    bool MayVoteAway([Required] GameState state, SeatId voter, SeatId absent);

    /// <summary>
    /// What <paramref name="seat"/> may do now whoever decides, for as long as the chance holds, as a leader face-off:
    /// none, unless the ruleset has such chances. <see cref="Apply"/> takes one this offers, from any seat.
    /// </summary>
    ImmutableList<GameAction> ChancesOf([Required] GameState state, SeatId seat) => [];

    /// <summary>How the deciding seat's decision is timed; null if it may take as long as it likes.</summary>
    DecisionTimer? TimerOf([Required] GameState state);

    /// <exception cref="MalformedActionException">The descriptor describes no action of this ruleset.</exception>
    GameAction Parse([Required] ActionDescriptor descriptor);

    /// <summary>What a piece of <paramref name="kind"/> is worth, to one weighing a trade of prisoners.</summary>
    int WorthOf(PieceKindId kind);

    /// <summary>
    /// What a piece of <paramref name="kind"/> is worth keeping on the board, to one weighing an exchange: what it
    /// could become, and where it can go, as well as what taking it scores (a Pawn that may yet be promoted, say,
    /// beside a Boat that reaches few cells); what it is worth (<see cref="WorthOf"/>), unless the ruleset says
    /// otherwise.
    /// </summary>
    int KeepingWorthOf(PieceKindId kind) => this.WorthOf(kind);

    /// <summary>
    /// The points <paramref name="offer"/>'s receiver would score for accepting it, to one weighing the trade:
    /// worth and points alike count toward winning.
    /// </summary>
    int RansomPointsOf([Required] GameState state, [Required] RansomOffer offer);

    /// <summary>
    /// The cells the pieces of <paramref name="seat"/>'s own army could capture on, whatever stands on
    /// them, the dice and whose turn it is aside: for one weighing what a move leaves open.
    /// </summary>
    ImmutableList<Attack> AttacksOf([Required] GameState state, SeatId seat);

    /// <summary>
    /// How far ahead a team is clearly ahead, on points and on what its pieces on the board are worth, as
    /// <see cref="WorthOf"/> counts them: Chaturaji 2027's 10, unless the ruleset says otherwise. For one weighing
    /// whether a game is won or lost.
    /// </summary>
    int ClearLead => 10;

    /// <summary>
    /// How many moves a piece of <paramref name="kind"/> may expect to be let make in one of its seat's turns, as
    /// the dice fall: one, unless the ruleset says otherwise. For one weighing how soon a piece may get somewhere.
    /// </summary>
    double MovesPerTurn(PieceKindId kind) => 1;

    /// <summary>Whether a piece of <paramref name="kind"/> leads its army, as a Chaturaji 2027 Raja does.</summary>
    bool IsLeader(PieceKindId kind);

    /// <summary>
    /// Whether a piece of <paramref name="kind"/>, not leading, is among the most dangerous to let loose, as a
    /// Rook is: for one weighing whether to send it home.
    /// </summary>
    bool IsHeavy(PieceKindId kind);

    string NameOf(PieceKindId kind);

    /// <summary>
    /// How a piece of <paramref name="kind"/> is marked, if by a class's emblem and letters; none, for a kind
    /// drawn as itself (a Raja, a Pawn).
    /// </summary>
    PieceMark? MarkOf(PieceKindId kind) => null;

    /// <summary>
    /// What a player looking at a piece of <paramref name="kind"/> may want to know: how it moves, its powers and
    /// what holds it back; nothing but the kind, with no moves, where the ruleset says nothing of its kinds.
    /// </summary>
    PieceGuide GuideOf(PieceKindId kind) => new(new PieceKind(kind, []));

    string NameOf(SeatId seat);

    /// <summary>What a die showing <paramref name="value"/> lets its roller do, for people: "Horse".</summary>
    string NameOfDie(int value);

    /// <summary>
    /// Whether the deciding seat could use a die rolled showing <paramref name="value"/> now: false if
    /// nothing it may move or promote is let by it. What a roll as a whole allows (bringing a captured
    /// piece back on doubles) is not this die's to tell.
    /// </summary>
    bool MayUseDie([Required] GameState state, int value);
}
