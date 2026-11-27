using System.Collections.Immutable;
using System.Text.Json;
using Net.NowhereAtAll.Zaturnava.Bots;
using Net.NowhereAtAll.Zaturnava.Engine.Actions;
using Net.NowhereAtAll.Zaturnava.Engine.Decisions;
using Net.NowhereAtAll.Zaturnava.Engine.Events;
using Net.NowhereAtAll.Zaturnava.Engine.Pieces;
using Net.NowhereAtAll.Zaturnava.Engine.Rules;
using Net.NowhereAtAll.Zaturnava.Engine.Seats;
using Net.NowhereAtAll.Zaturnava.Games.Chaturaji2027;

namespace Net.NowhereAtAll.Zaturnava.BotRunner.Test;

/// <summary>
/// Not a test of the code, but the bots checked against games played, on request, through the test runner: when
/// <c>artifacts/blunders/histories.json</c> holds games (written by <c>scripts/fetch-histories.py</c>), each Chaturaji
/// 2027 game is replayed, and every move a bot made is looked at: a blunder, if an enemy took the piece it moved before
/// the bot's next turn, its team losing at least <see cref="Loss"/> points more than it won, through the end of that
/// turn (the user's games of 2026-11-27: a Rook given for a Horse, a Pawn left to be taken). A blunder is hung if the
/// piece stood attacked once the bot's turn was over; else an enemy reached it in two moves, on its dice. Each hung
/// blunder's position is then put to the bot as it plays now, if it had more than one move and is among the strong
/// partners, and whether it plays the same is told; all to <c>artifacts/blunders/checked.json</c>, with how many each
/// bot made. How the games ended goes to <c>artifacts/blunders/endings.json</c>. With no games, it is skipped, as on
/// CI.
/// </summary>
public sealed class BlunderRun
{
    /// <summary>How many points more than its team won meanwhile a move must lose to count as a blunder.</summary>
    private const int Loss = 2;

    private static readonly JsonSerializerOptions Json = new(JsonSerializerOptions.Web) { WriteIndented = true };

    private readonly Chaturaji2027Rules _rules = new();

    [Fact]
    public async Task Check_WhenGamesAreGiven_WritesTheBotsBlundersAndWhetherTheyRepeatThem()
    {
        // Arrange
        string folder = Path.Combine(Root(), "artifacts", "blunders");
        string gamesPath = Path.Combine(folder, "histories.json");
        Assert.SkipUnless(File.Exists(gamesPath), "No games are given.");
        using JsonDocument games = JsonDocument.Parse(
            await File.ReadAllTextAsync(gamesPath, TestContext.Current.CancellationToken)
        );
        ImmutableDictionary<string, IBotPlaying> bots = Bots();
        ImmutableList<JsonElement> played =
        [
            .. games.RootElement.EnumerateArray()
                .Where(game => game.GetProperty("rulesetId").GetString() == this._rules.Id),
        ];

        // Act
        await this.WriteEndings(folder, played);
        ImmutableList<Blunder> found = [.. played.SelectMany(this.BlundersIn)];
        ImmutableList<object> checkedNow = [];
        foreach (Blunder blunder in found.Where(blunder => blunder.IsHung && bots.ContainsKey(blunder.Bot)))
        {
            checkedNow = checkedNow.Add(await this.Checked(blunder, bots));
            await Write(folder, found, checkedNow);
        }

        // Assert
        Assert.True(File.Exists(Path.Combine(folder, "checked.json")) || found.IsEmpty);
    }

    /// <summary>
    /// How the games ended, to <c>artifacts/blunders/endings.json</c>: how many pieces were left on the board, counted
    /// by how many games ended with that many or fewer; and how many ended with at most one team keeping a leader.
    /// </summary>
    private async Task WriteEndings(string folder, ImmutableList<JsonElement> played)
    {
        ImmutableList<(int Left, bool IsAttrition, int Steps)> endings =
        [
            .. played.Select(game =>
            {
                ImmutableList<GameAction> actions =
                [
                    .. game.GetProperty("actions").EnumerateArray()
                        .Select(action => this._rules.Parse(Described(action))),
                ];
                GameState end = this.StateAfter(actions);
                ImmutableHashSet<int> teamsLed =
                [
                    .. end.Position.Occupants.Values
                        .Where(piece => this._rules.IsLeader(piece.Kind))
                        .Select(piece => end.Teams.AreAllied(piece.Owner, new SeatId(0)) ? 0 : 1),
                ];
                return (end.Position.Occupants.Count, teamsLed.Count < 2, actions.Count);
            }),
        ];
        int[] bounds = [4, 5, 8, 12, 16, 24, 32];
        var summary = new
        {
            Games = endings.Count,
            AtMostOneTeamLed = endings.Count(ending => ending.IsAttrition),
            PiecesLeft = bounds.Select(bound => new
            {
                AtMost = bound,
                Games = endings.Count(ending => ending.Left <= bound),
            }),
            Each = endings.Select(ending => new { ending.Left, ending.IsAttrition, ending.Steps }),
        };
        await File.WriteAllTextAsync(
            Path.Combine(folder, "endings.json"),
            JsonSerializer.Serialize(summary, Json),
            TestContext.Current.CancellationToken
        );
    }

    private static async Task Write(string folder, ImmutableList<Blunder> found, ImmutableList<object> checkedNow)
    {
        var summary = found
            .GroupBy(blunder => blunder.Bot)
            .Select(made => new
            {
                Bot = made.Key,
                Found = made.Count(),
                Hung = made.Count(blunder => blunder.IsHung),
            })
            .OrderByDescending(made => made.Found);
        await File.WriteAllTextAsync(
            Path.Combine(folder, "checked.json"),
            JsonSerializer.Serialize(new { Summary = summary, Checked = checkedNow }, Json),
            TestContext.Current.CancellationToken
        );
    }

    /// <summary>
    /// The blunders in one game: each move of a bot's seat that lost the piece it moved before its next turn, its team
    /// worse off by <see cref="Loss"/> or more meanwhile.
    /// </summary>
    private IEnumerable<Blunder> BlundersIn(JsonElement game)
    {
        string id = game.GetProperty("gameId").GetString() ?? "?";
        ImmutableDictionary<int, string> players = game.GetProperty("seats").EnumerateArray().ToImmutableDictionary(
            seat => seat.GetProperty("id").GetInt32(),
            seat => seat.GetProperty("player").GetString() ?? "?"
        );
        ImmutableList<GameAction> actions =
            [.. game.GetProperty("actions").EnumerateArray().Select(action => this._rules.Parse(Described(action)))];
        ImmutableList<(GameState Before, ImmutableList<GameEvent> Events)> steps = this.Replayed(actions);
        for (int at = 0; at < steps.Count; at++)
        {
            if (actions[at] is MoveAction move
                && players.GetValueOrDefault(move.Actor.Value) is { } bot
                && bot is not ("person" or "open")
                && steps[at].Before.Position.PieceAt(move.From) is { } moved
                && this.LostAfter(steps, actions, at, moved) is { } lost
                && lost.Net <= -Loss)
            {
                yield return new Blunder(
                    id, at, bot, moved.Kind.Value, move.From.Value, move.To.Value, lost.Net, lost.IsHung, actions
                );
            }
        }
    }

    /// <summary>
    /// If the piece moved was taken by an enemy before the mover's next turn: what the mover's team won less what it
    /// lost, from the move to the end of that next turn (so that taking back counts); and whether the piece stood
    /// attacked by an enemy once the mover's turn was over, a loss one move deep, which a bot weighing only the next
    /// move should see, rather than one an enemy reached in two moves on its dice. Else null.
    /// </summary>
    private (int Net, bool IsHung)? LostAfter(
        ImmutableList<(GameState Before, ImmutableList<GameEvent> Events)> steps,
        ImmutableList<GameAction> actions,
        int at,
        Piece moved
    )
    {
        SeatId seat = actions[at].Actor;
        TeamGraph teams = steps[at].Before.Teams;
        int net = 0;
        bool isTaken = false;
        bool? isHung = null;
        int phase = 0; // 0 the mover's turn, 1 the others', 2 the mover's next turn
        for (int next = at; next < actions.Count; next++)
        {
            SeatId actor = actions[next].Actor;
            if ((phase is 0 or 2) == (actor != seat))
            {
                phase++;
                if (phase == 3)
                {
                    break;
                }
            }

            if (phase == 1 && isHung is null)
            {
                isHung = this.IsAttacked(steps[next].Before, seat, moved);
            }

            foreach (PieceCaptured captured in steps[next].Events.OfType<PieceCaptured>())
            {
                bool isOurs = teams.AreAllied(captured.Piece.Owner, seat);
                bool isByUs = teams.AreAllied(captured.CapturedBy, seat);
                net += (isByUs ? captured.Points : 0) - (isOurs && !isByUs ? captured.Points : 0);
                isTaken |= phase == 1 && captured.Piece.Id == moved.Id && !isByUs;
            }
        }

        return isTaken ? (net, isHung == true) : null;
    }

    /// <summary>Whether an enemy of <paramref name="seat"/> attacks the piece, wherever it stands.</summary>
    private bool IsAttacked(GameState state, SeatId seat, Piece piece) =>
        state.Position.Occupants.FirstOrDefault(occupant => occupant.Value.Id == piece.Id) is { Value: not null } at
        && state.Seats
            .Where(other => !state.Teams.AreAllied(other.Id, seat))
            .Any(other => this._rules.AttacksOf(state, other.Id).Any(attack => attack.Target == at.Key));

    /// <summary>The blunder's position put to the bot that made it, as it plays now; whether it plays the same.</summary>
    private async Task<object> Checked(Blunder blunder, ImmutableDictionary<string, IBotPlaying> bots)
    {
        GameState state = this.StateAfter(blunder.Actions.Take(blunder.At));
        GameAction made = blunder.Actions[blunder.At];
        ImmutableList<GameAction> legal = this._rules.LegalActions(state);
        int moves = legal.Count(action => action is MoveAction or EscapeAction);
        GameAction? now = moves > 1 && bots.TryGetValue(blunder.Bot, out IBotPlaying? bot)
            ? await bot.Decide(
                new DecisionRequest(this._rules, state, made.Actor, legal),
                TestContext.Current.CancellationToken
            )
            : null;
        return new
        {
            blunder.Game,
            blunder.At,
            blunder.Bot,
            blunder.Kind,
            blunder.From,
            blunder.To,
            blunder.Net,
            blunder.IsHung,
            Moves = moves,
            Now = now is null ? null : Told(now),
            Repeated = now?.Equals(made),
        };
    }

    /// <summary>An action as its kind and its arguments, as the histories give them.</summary>
    private static string Told(GameAction action)
    {
        ActionDescriptor described = action.Describe();
        return string.Join(' ', [described.Kind, .. described.Arguments.Select(argument => $"{argument.Key}={argument.Value}")]);
    }

    private GameState StateAfter(IEnumerable<GameAction> actions) =>
        actions.Aggregate(this._rules.CreateInitialState(), (state, action) => this._rules.Apply(state, action).State);

    /// <summary>Each action's state before it, and the events it told.</summary>
    private ImmutableList<(GameState Before, ImmutableList<GameEvent> Events)> Replayed(
        ImmutableList<GameAction> actions
    )
    {
        GameState state = this._rules.CreateInitialState();
        ImmutableList<(GameState Before, ImmutableList<GameEvent> Events)>.Builder steps =
            ImmutableList.CreateBuilder<(GameState Before, ImmutableList<GameEvent> Events)>();
        foreach (GameAction action in actions)
        {
            Transition transition = this._rules.Apply(state, action);
            steps.Add((state, transition.Events));
            state = transition.State;
        }

        return steps.ToImmutable();
    }

    private static ActionDescriptor Described(JsonElement action) =>
        new(
            action.GetProperty("kind").GetString()!,
            action.GetProperty("arguments").EnumerateObject().ToImmutableSortedDictionary(
                argument => argument.Name,
                argument => argument.Value.GetString() ?? string.Empty,
                StringComparer.Ordinal
            )
        );

    /// <summary>The bots that may have played, by id, each as it plays now.</summary>
    private static ImmutableDictionary<string, IBotPlaying> Bots() =>
        new IBotPlaying[]
        {
            new AthenaBot(new Random(1)), new TacticianBot(new Random(1)), new SageBot(new Random(1)),
            new ArgusBot(new Random(1)),
        }
            .ToImmutableDictionary(bot => bot.Profile.Id);

    /// <summary>The repository's root: the nearest folder above the tests holding the solution.</summary>
    private static string Root()
    {
        DirectoryInfo folder = new(AppContext.BaseDirectory);
        while (!File.Exists(Path.Combine(folder.FullName, "Zaturnava.slnx")))
        {
            folder = folder.Parent!;
        }

        return folder.FullName;
    }

    /// <summary>A bot's move that lost its piece: where in which game, the piece, and how much its team lost.</summary>
    private sealed record Blunder(
        string Game,
        int At,
        string Bot,
        string Kind,
        int From,
        int To,
        int Net,
        bool IsHung,
        ImmutableList<GameAction> Actions
    );
}
