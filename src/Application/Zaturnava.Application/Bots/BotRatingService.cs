using System.Collections.Immutable;
using Net.NowhereAtAll.Zaturnava.Application.Players;
using Net.NowhereAtAll.Zaturnava.Application.Ports;
using Net.NowhereAtAll.Zaturnava.Application.Rulesets;
using Net.NowhereAtAll.Zaturnava.Application.Running;
using Net.NowhereAtAll.Zaturnava.Engine.Chance;
using Net.NowhereAtAll.Zaturnava.Engine.Decisions;
using Net.NowhereAtAll.Zaturnava.Engine.Rules;
using Net.NowhereAtAll.Zaturnava.Engine.Seats;

namespace Net.NowhereAtAll.Zaturnava.Application.Bots;

/// <summary>
/// Rates the server's bots by having them play each other (Stated, 2026-11-12: every bot, a good mix of rulesets,
/// each result kept as it comes, so that whatever was gathered counts however the run stops). Each game is the
/// pairing played least, at any ruleset, the first in the catalog's order among those alike, so that every pair at
/// every ruleset is played about as often (Proposed); the two bots take turns at the seats, one at the even seats and
/// the other at the odd, and change sides each game. A game that ends goes to the team that won it, or is drawn; one
/// that reaches <see cref="BotRatingSettings.MaximumSteps"/> goes to the side with more points, or is drawn, as the
/// rating script counts them. A bot not rated at a ruleset, once it has played each rated bot there
/// <see cref="BotRatingSettings.MinimumGames"/> times, is rated by those games against their ratings
/// (<see cref="BotRatingFit.Against"/>); those pairings are played first. A rated bot keeps the rating it has.
/// </summary>
public sealed class BotRatingService(
    IRulesetLocating rulesets,
    BotCatalog bots,
    IBotMatchupPersisting matchups,
    IDiceRolling dice,
    BotRatingSettings settings
)
{
    private readonly IRulesetLocating _rulesets = rulesets;
    private readonly BotCatalog _bots = bots;
    private readonly IBotMatchupPersisting _matchups = matchups;
    private readonly IDiceRolling _dice = dice;
    private readonly BotRatingSettings _settings = settings;

    /// <summary>
    /// Rates, at each ruleset, each bot not rated there yet whose every pair with a rated bot has played enough, by
    /// those games, against the rated bots' ratings held as they are (<see cref="BotRatingFit.Against"/>); the anchor,
    /// if not rated, at <see cref="BotRatingFit.AnchorRating"/>. A rated bot keeps its rating (Proposed, 2026-11-13:
    /// every rating on one scale, the one the server shipped with, a new bot rated once its own games are in). Which
    /// rulesets any bot was rated at.
    /// </summary>
    public async Task<ImmutableList<string>> Apply(CancellationToken cancellationToken)
    {
        ImmutableList<BotMatchup> kept = await this._matchups.Matchups(cancellationToken);
        ImmutableList<string> rated = [];
        foreach (string ruleset in this._rulesets.All.Select(ruleset => ruleset.Id))
        {
            ImmutableDictionary<string, int> shown = this._bots.Bots
                .Where(bot => bot.Ratings.ContainsKey(ruleset))
                .ToImmutableDictionary(bot => bot.Id, bot => bot.Ratings[ruleset]);
            ImmutableDictionary<string, int> anchor = shown.ContainsKey(this._settings.Anchor)
                ? []
                : ImmutableDictionary<string, int>.Empty.Add(this._settings.Anchor, (int)BotRatingFit.AnchorRating);
            ImmutableDictionary<string, int> known = shown.AddRange(anchor);
            ImmutableList<BotMatchup> pairings = this.Pairings(ruleset);
            ImmutableList<BotMatchup> games = kept.RemoveAll(matchup => matchup.Ruleset != ruleset);
            bool IsAgainstKnown(BotMatchup pairing, string bot) =>
                pairing.First == bot ? known.ContainsKey(pairing.Second)
                : pairing.Second == bot && known.ContainsKey(pairing.First);
            bool IsReady(string bot) =>
                pairings
                    .Where(pairing => IsAgainstKnown(pairing, bot))
                    .All(pairing => PlayedOf(kept, pairing) >= this._settings.MinimumGames);
            ImmutableDictionary<string, int> fitted = anchor.AddRange(
                this._bots.Bots
                    .Select(bot => bot.Id)
                    .Where(bot => !known.ContainsKey(bot) && IsReady(bot))
                    .Select(bot => KeyValuePair.Create(bot, BotRatingFit.Against(bot, known, games)!.Value))
            );
            if (!fitted.IsEmpty)
            {
                this._bots.Rate(ruleset, fitted);
                rated = rated.Add(ruleset);
            }
        }

        return rated;
    }

    /// <summary>
    /// Plays the pairing played least, those of a bot not yet rated at its ruleset with one that is first, as only
    /// those rate it (Proposed), and keeps how it went; that game, counted for the first bot.
    /// </summary>
    public async Task<BotMatchup> PlayNext(CancellationToken cancellationToken)
    {
        ImmutableList<BotMatchup> kept = await this._matchups.Matchups(cancellationToken);
        ImmutableDictionary<string, BotProfile> profiles = this._bots.Bots.ToImmutableDictionary(bot => bot.Id);
        bool IsRated(string bot, string ruleset) =>
            bot == this._settings.Anchor || profiles[bot].Ratings.ContainsKey(ruleset);
        bool RatesANewBot(BotMatchup pairing) =>
            IsRated(pairing.First, pairing.Ruleset) != IsRated(pairing.Second, pairing.Ruleset);
        BotMatchup next = this._rulesets.All
            .SelectMany(ruleset => this.Pairings(ruleset.Id))
            .OrderBy(pairing => !RatesANewBot(pairing))
            .ThenBy(pairing => PlayedOf(kept, pairing))
            .First();
        IRulesApplying rules = this._rulesets.Find(next.Ruleset);
        bool isFirstAtEven = PlayedOf(kept, next) % 2 == 0;
        ImmutableList<string> seated =
        [
            .. Enumerable.Range(0, rules.CreateInitialState().Seats.Count)
                .Select(seat => seat % 2 == 0 == isFirstAtEven ? next.First : next.Second),
        ];
        Played played = await DecisionLoop.PlayAll(
            rules,
            [.. seated.Select(this._bots.Find)],
            this._dice,
            this._settings.MaximumSteps,
            cancellationToken
        );
        GameResult result = ResultOf(played.State, seated, next.First);
        BotMatchup game = next with
        {
            Won = result == GameResult.Won ? 1 : 0,
            Lost = result == GameResult.Lost ? 1 : 0,
            Drawn = result == GameResult.Drawn ? 1 : 0,
        };
        await this._matchups.Add(game, cancellationToken);
        return game;
    }

    /// <summary>Every pair of the bots at the ruleset, each once, the lesser id first, none played yet.</summary>
    private ImmutableList<BotMatchup> Pairings(string ruleset)
    {
        ImmutableList<string> ids = [.. this._bots.Bots.Select(bot => bot.Id)];
        return
        [
            .. ids.SelectMany(first =>
                ids.Where(second => string.CompareOrdinal(first, second) < 0)
                    .Select(second => new BotMatchup(ruleset, first, second))
            ),
        ];
    }

    private static int PlayedOf(ImmutableList<BotMatchup> kept, BotMatchup pairing) =>
        kept.Where(pairing.IsPairing).Sum(matchup => matchup.Played);

    /// <summary>How the game went for <paramref name="first"/>'s seats: by who won, if it ended; else points.</summary>
    private static GameResult ResultOf(GameState state, ImmutableList<string> seated, string first)
    {
        bool IsFirsts(SeatId seat) => seated[seat.Value] == first;
        int margin = state.Outcome is { } outcome
            ? outcome.Winners.IsEmpty ? 0 : outcome.Winners.Any(IsFirsts) ? 1 : -1
            : state.Scores.Sum(score => IsFirsts(score.Key) ? score.Value : -score.Value);
        return margin > 0 ? GameResult.Won : margin < 0 ? GameResult.Lost : GameResult.Drawn;
    }
}
