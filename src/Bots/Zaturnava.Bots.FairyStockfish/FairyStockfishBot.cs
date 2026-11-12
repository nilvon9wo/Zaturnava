using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using Net.NowhereAtAll.Zaturnava.Engine.Actions;
using Net.NowhereAtAll.Zaturnava.Engine.Decisions;

namespace Net.NowhereAtAll.Zaturnava.Bots.FairyStockfish;

/// <summary>
/// Fairy-Stockfish's advice, one seat at a time (Proposed; the user's wish of 2026-11-12 to try it): of the moves the
/// seat may make now (the dice have chosen which pieces may move), the one Fairy-Stockfish would make, the game seen
/// from the seat's side (<see cref="FairyPosition"/>), thinking for <see cref="Thinking"/>; never passing, nor
/// offering a trade, while it has a choice of moves. Anything else it is asked, or a game Fairy-Stockfish cannot
/// see, is <paramref name="fallback"/>'s to decide: drafting, ransom, ending a game, a single move or none, a piece
/// it cannot define; and so is a move it does not name.
/// </summary>
public sealed class FairyStockfishBot(UciEngine engine, IBotPlaying fallback) : IBotPlaying
{
    public const string BotId = "fairy-stockfish";

    /// <summary>How long it thinks over each move (Proposed).</summary>
    public static readonly TimeSpan Thinking = TimeSpan.FromMilliseconds(200);

    private readonly UciEngine _engine = engine;
    private readonly IBotPlaying _fallback = fallback;

    /// <summary>
    /// The engine wherever it is found (<see cref="EnginePath"/>), started once it is first asked, its variants
    /// written to a folder of this process's own; Athena deciding the rest. As a bot assembly is loaded (with a
    /// <see cref="Random"/>).
    /// </summary>
    /// <exception cref="FileNotFoundException">Once first asked, if the engine is nowhere to be found.</exception>
    public FairyStockfishBot(Random random)
        : this(
            EngineAt(
                EnginePath.Find(
                    Environment.GetEnvironmentVariable(EnginePath.Variable),
                    new DirectoryInfo(AppContext.BaseDirectory)
                )
            ),
            new AthenaBot(random)
        )
    {
    }

    public BotProfile Profile { get; } = new(
        BotId,
        "Fairy-Stockfish",
        "Experimental: asks Fairy-Stockfish, a strong chess engine for many variants, which of the moves the dice "
            + "allow to make, seeing its own team against the rest as two sides of one board. Anything else, such "
            + "as drafting or ransom, it leaves to Athena. A plain algorithm: no AI.",
        UsesAi: false
    );

    public async Task<GameAction> Decide([Required] DecisionRequest request, CancellationToken cancellationToken)
    {
        ImmutableList<MoveAction> moves = [.. request.LegalActions.OfType<MoveAction>()];
        FairyPosition? position = moves.Count > 1
            ? FairyPosition.Of(request.Rules, request.State, request.Seat)
            : null;
        if (position is null)
        {
            return await this._fallback.Decide(request, cancellationToken);
        }

        ImmutableDictionary<string, MoveAction> named = moves.ToImmutableDictionary(
            move => FairyPosition.SquareOf(request.Rules.Topology.LayoutOf(move.From))
                + FairyPosition.SquareOf(request.Rules.Topology.LayoutOf(move.To))
        );
        string? best = await this._engine.BestMove(position, [.. named.Keys], Thinking, cancellationToken);
        return best is not null ? named[best] : await this._fallback.Decide(request, cancellationToken);
    }

    public Task<GameAction?> Seize([Required] DecisionRequest request, CancellationToken cancellationToken) =>
        this._fallback.Seize(request, cancellationToken);

    /// <summary>
    /// The engine at <paramref name="path"/>, run in a <c>variants</c> folder beside it, where its variants are
    /// written; failing once first asked, if there is none.
    /// </summary>
    public static UciEngine EngineAt(string? path)
    {
        string folder = Path.Combine(Path.GetDirectoryName(path) ?? Path.GetTempPath(), "variants");
        return new UciEngine(
            new LazyUciChannel(() =>
                new ProcessUciChannel(
                    path ?? throw new FileNotFoundException("Fairy-Stockfish is not where it is looked for."),
                    folder
                )
            ),
            folder
        );
    }
}
