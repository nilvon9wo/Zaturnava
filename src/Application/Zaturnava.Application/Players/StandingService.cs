using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using Net.NowhereAtAll.Zaturnava.Application.Bots;
using Net.NowhereAtAll.Zaturnava.Application.Games;
using Net.NowhereAtAll.Zaturnava.Application.Ports;
using Net.NowhereAtAll.Zaturnava.Application.Rulesets;
using Net.NowhereAtAll.Zaturnava.Engine.Rules;
using Net.NowhereAtAll.Zaturnava.Engine.Seats;

namespace Net.NowhereAtAll.Zaturnava.Application.Players;

/// <summary>
/// Where the players stand, game by game (Proposed but for experience, which is as asked): each game over counts
/// once, in the order the games ended, for every signed-in player at it, but not a seat their own AI played. Each
/// team is rated as the mean of its seats': a player's, their Elo at the game; a bot's, its rating at the ruleset;
/// anyone else's, <see cref="Standing.FirstElo"/>. A player's Elo moves by <see cref="K"/> times how far their result
/// (1 won, a half drawn, 0 lost) is from what their team's rating led to expect against the other teams, on average:
/// only for a game that counts toward Elo (<see cref="CountsTowardElo"/>).
/// Each time a player is on the winning team, they gain as experience their own points plus their team's; and in every
/// game, they add their own points to those they have scored.
/// </summary>
public sealed class StandingService(
    IRulesetLocating rulesets,
    IBotLocating bots,
    IGamePersisting games,
    IPlayerPersisting players,
    TimeProvider time
)
{
    /// <summary>How long a game must wait untouched for a player's move to count as abandoned (Stated).</summary>
    public static readonly TimeSpan AbandonedAfter = TimeSpan.FromHours(24);

    /// <summary>How far one game may move a rating (Proposed).</summary>
    public const double K = 32;

    /// <summary>The most games counted at a time; any more, the next time.</summary>
    public const int MostRatedAtOnce = 50;

    /// <summary>The most players a leaderboard lists (Proposed).</summary>
    public const int MostLeaders = 100;

    private const double EloSpread = 400;

    private readonly IRulesetLocating _rulesets = rulesets;
    private readonly IBotLocating _bots = bots;
    private readonly IGamePersisting _games = games;
    private readonly IPlayerPersisting _players = players;
    private readonly TimeProvider _time = time;

    private enum Result
    {
        Lost,
        Drawn,
        Won,
    }

    /// <summary>
    /// Counts the games over, not yet counted, in their players' standings, up to <see cref="MostRatedAtOnce"/>: a
    /// game of a ruleset no longer offered, or that its rules no longer replay, counts for no one. How many there were.
    /// </summary>
    public async Task<int> RateFinished(CancellationToken cancellationToken)
    {
        ImmutableList<GameRecord> finished = await this._games.Unrated(MostRatedAtOnce, cancellationToken);
        foreach (GameRecord game in finished)
        {
            if (this.Replayed(game) is { State.Outcome: { } outcome } snapshot)
            {
                await this.Rate(snapshot, outcome, cancellationToken);
            }

            await this._games.MarkRated(game.Id, cancellationToken);
        }

        return finished.Count;
    }

    /// <summary>
    /// The players standing highest at <paramref name="game"/> (a ruleset's game id), each by name, best first, with
    /// how many of its games they have abandoned: at most <see cref="MostLeaders"/>.
    /// </summary>
    public async Task<ImmutableList<Leader>> Leaders([Required] string game, CancellationToken cancellationToken)
    {
        ImmutableList<Standing> leaders = await this._players.Leaders(game, MostLeaders, cancellationToken);
        ImmutableDictionary<PlayerId, string> names =
            await this._players.Names([.. leaders.Select(standing => standing.Player)], cancellationToken);
        ImmutableDictionary<PlayerId, int> abandoned = await this.Abandoned(game, cancellationToken);
        return
        [
            .. leaders
                .Where(standing => names.ContainsKey(standing.Player))
                .Select(standing =>
                    new Leader(names[standing.Player], standing)
                    {
                        Abandoned = abandoned.GetValueOrDefault(standing.Player),
                    }
                ),
        ];
    }

    /// <summary>
    /// How many games of <paramref name="game"/> each player has abandoned, as they stand now (Stated): games not
    /// over, untouched for <see cref="AbandonedAfter"/>, with no guest at them (such a game is let go instead), waiting
    /// for that player to decide at a seat they play themselves, not their AI. Once they move, it counts no more.
    /// </summary>
    private async Task<ImmutableDictionary<PlayerId, int>> Abandoned(string game, CancellationToken cancellationToken)
    {
        ImmutableList<GameRecord> idle =
            await this._games.Idle(this._time.GetUtcNow() - AbandonedAfter, cancellationToken);
        return idle
            .Where(record => !record.Controllers.Any(controller => controller.IsGuest))
            .Select(record => this.AwaitedPlayer(record, game))
            .OfType<PlayerId>()
            .GroupBy(player => player)
            .ToImmutableDictionary(players => players.Key, players => players.Count());
    }

    /// <summary>The player a game of <paramref name="game"/> waits for, at a seat they play themselves.</summary>
    private PlayerId? AwaitedPlayer(GameRecord record, string game) =>
        this.Replayed(record) is { } snapshot
        && snapshot.Rules.GameId == game
        && snapshot.State.DecidingSeat is { } seat
        && record.Controllers[seat.Value] is HumanSeat { IsAi: false } human
            ? human.Claimant
            : null;

    private GameSnapshot? Replayed(GameRecord game) =>
        this._rulesets.All.FirstOrDefault(ruleset => ruleset.Id == game.RulesetId) is { } rules
            ? GameSnapshot.TryOf(game, rules)
            : null;

    private async Task Rate(GameSnapshot snapshot, GameOutcome outcome, CancellationToken cancellationToken)
    {
        GameState state = snapshot.State;
        string game = snapshot.Rules.GameId;
        ImmutableDictionary<SeatId, PlayerId> rated = state.Seats
            .Select(seat => (seat.Id, Player: RatedPlayer(snapshot.Record.Controllers[seat.Id.Value])))
            .Where(entry => entry.Player is not null)
            .DistinctBy(entry => entry.Player)
            .ToImmutableDictionary(entry => entry.Id, entry => entry.Player!.Value);
        ImmutableDictionary<PlayerId, Standing> stored =
            await this._players.Standings(game, [.. rated.Values], cancellationToken);
        ImmutableDictionary<SeatId, Standing> before = rated.ToImmutableDictionary(
            entry => entry.Key,
            entry => stored.GetValueOrDefault(entry.Value) ?? new Standing(entry.Value, game)
        );
        ImmutableDictionary<int, double> teams = state.Seats
            .GroupBy(seat => state.Teams.TeamOf(seat.Id))
            .ToImmutableDictionary(
                team => team.Key,
                team => team.Average(seat => this.RatingOf(snapshot, before, seat.Id))
            );
        bool isRated = CountsTowardElo(snapshot.Record);
        ImmutableList<Standing> after =
        [
            .. before.Select(entry => After(state, outcome, teams, entry.Key, entry.Value, isRated)),
        ];
        await this._players.Keep(after, cancellationToken);
    }

    /// <summary>
    /// Whether the game counts toward its players' Elo (Stated): only if its players stayed the same throughout, no
    /// seat changing hands once it began (<see cref="GameRecord.HasChangedHands"/>); no seat was a guest's, whose
    /// rating no one knows; and no player had several seats, their own and their AIs', however many, as one side could
    /// then play badly to raise the other. Every other count stands, whether or not it does (Proposed).
    /// </summary>
    private static bool CountsTowardElo(GameRecord record) =>
        !record.HasChangedHands
        && !record.Controllers.Any(controller => controller.IsGuest)
        && record.Controllers
            .OfType<HumanSeat>()
            .GroupBy(seat => seat.Claimant)
            .All(seats => seats.Count() == 1);

    /// <summary>The player whose standing a seat counts in: a signed-in player's, unless their AI played it.</summary>
    private static PlayerId? RatedPlayer(SeatController seat) =>
        seat is HumanSeat { Claimant: { } player, IsAi: false } ? player : null;

    /// <summary>A seat's rating: its player's, a bot's at the ruleset, or else the first.</summary>
    private double RatingOf(GameSnapshot snapshot, ImmutableDictionary<SeatId, Standing> rated, SeatId seat) =>
        rated.TryGetValue(seat, out Standing? standing)
            ? standing.Elo
            : snapshot.Record.Controllers[seat.Value] is BotSeat bot
                ? this.BotRatingOf(bot.BotId, snapshot.Rules.Id)
                : Standing.FirstElo;

    private double BotRatingOf(string bot, string ruleset) =>
        this._bots.Bots
            .Where(profile => profile.Id == bot && profile.Ratings.ContainsKey(ruleset))
            .Select(profile => (double)profile.Ratings[ruleset])
            .DefaultIfEmpty(Standing.FirstElo)
            .First();

    /// <summary>
    /// Where the player at <paramref name="seat"/> stands, the game over: their Elo as it was, unless the game
    /// <paramref name="isRated"/>.
    /// </summary>
    private static Standing After(
        GameState state,
        GameOutcome outcome,
        ImmutableDictionary<int, double> teams,
        SeatId seat,
        Standing before,
        bool isRated
    )
    {
        int team = state.Teams.TeamOf(seat);
        double expected = teams
            .Where(other => other.Key != team)
            .Average(other => 1 / (1 + Math.Pow(10, (other.Value - teams[team]) / EloSpread)));
        Result result = ResultOf(state, outcome, seat);
        int points = state.Scores.GetValueOrDefault(seat)
            + state.Scores.Where(score => state.Teams.AreAllied(score.Key, seat)).Sum(score => score.Value);
        return before with
        {
            Elo = isRated ? before.Elo + (K * (((double)result / 2) - expected)) : before.Elo,
            Experience = before.Experience + (result == Result.Won ? points : 0),
            Points = before.Points + state.Scores.GetValueOrDefault(seat),
            Won = before.Won + (result == Result.Won ? 1 : 0),
            Lost = before.Lost + (result == Result.Lost ? 1 : 0),
            Drawn = before.Drawn + (result == Result.Drawn ? 1 : 0),
        };
    }

    /// <summary>How the game ended for the seat: drawn, if no one won; else won, if its team did, or lost.</summary>
    private static Result ResultOf(GameState state, GameOutcome outcome, SeatId seat) =>
        outcome.Winners.IsEmpty
            ? Result.Drawn
            : outcome.Winners.Any(winner => state.Teams.AreAllied(winner, seat))
                ? Result.Won
                : Result.Lost;
}
