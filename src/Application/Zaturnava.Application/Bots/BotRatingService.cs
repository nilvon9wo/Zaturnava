using System.Collections.Immutable;
using Net.NowhereAtAll.Zaturnava.Application.Players;
using Net.NowhereAtAll.Zaturnava.Application.Ports;
using Net.NowhereAtAll.Zaturnava.Application.Rulesets;
using Net.NowhereAtAll.Zaturnava.Application.Running;
using Net.NowhereAtAll.Zaturnava.Engine.Chance;
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
/// rating script counts them. A bot not shipped rated at a ruleset (<see cref="BotCatalog.Shipped"/>), once it has
/// played each shipped bot there <see cref="BotRatingSettings.MinimumGames"/> times, is rated by those games against
/// their ratings (<see cref="BotRatingFit.Against"/>), anew after every game, so that more games rate it better;
/// those pairings are played first. A shipped bot keeps the rating it shipped with.
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
    /// Rates, at each ruleset, each bot not shipped rated there whose every pair with a shipped bot has played enough,
    /// by all those games, against the shipped ratings held as they are (<see cref="BotRatingFit.Against"/>); where
    /// only the anchor is, by every bot's games fitted together (<see cref="BotRatingFit.Fitted"/>), once its every
    /// pair has played enough, as against the anchor alone every bot that beats it rates alike; the anchor, if not
    /// shipped, at <see cref="BotRatingFit.AnchorRating"/>. A shipped bot keeps its rating (Proposed, 2026-11-13:
    /// every rating on one scale, the one the server shipped with; a new bot rated anew as its games come in, not
    /// held to its first few, which say little). Which rulesets any bot was rated at.
    /// </summary>
    public async Task<ImmutableList<string>> Apply(CancellationToken cancellationToken)
    {
        ImmutableList<BotMatchup> kept = await this._matchups.Matchups(cancellationToken);
        ImmutableList<string> rated = [];
        foreach (string ruleset in this._rulesets.All.Select(ruleset => ruleset.Id))
        {
            ImmutableDictionary<string, int> held = this.HeldAt(ruleset);
            ImmutableDictionary<string, int> anchor = held.RemoveRange(this.ShippedAt(ruleset).Keys);
            bool isAnchorAlone = held.Count == 1;
            ImmutableList<BotMatchup> pairings = this.Pairings(ruleset);
            ImmutableList<BotMatchup> games = kept.RemoveAll(matchup => matchup.Ruleset != ruleset);
            bool IsReady(string bot) =>
                pairings
                    .Where(pairing => IsOf(pairing, bot) && this.RatesANewBot(pairing))
                    .All(pairing => PlayedOf(kept, pairing) >= this._settings.MinimumGames);
            ImmutableList<string> ready =
                [.. this._bots.Bots.Select(bot => bot.Id).Where(bot => !held.ContainsKey(bot) && IsReady(bot))];
            ImmutableDictionary<string, int> together = isAnchorAlone && !ready.IsEmpty
                ? BotRatingFit.Fitted(this._settings.Anchor, games)
                : [];
            ImmutableDictionary<string, int> fitted = anchor.AddRange(
                ready.Select(bot => KeyValuePair.Create(
                    bot,
                    isAnchorAlone ? together[bot] : BotRatingFit.Against(bot, held, games)!.Value
                ))
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
    /// Plays the pairing played least, those that rate a bot not shipped rated at its ruleset first, as only those
    /// rate it (Proposed): one with a shipped bot, or, where only the anchor is, any; and keeps how it went; that game,
    /// counted for the first bot.
    /// </summary>
    public async Task<BotMatchup> PlayNext(CancellationToken cancellationToken)
    {
        ImmutableList<BotMatchup> kept = await this._matchups.Matchups(cancellationToken);

        BotMatchup next = this._rulesets.All
            .SelectMany(ruleset => this.Pairings(ruleset.Id))
            .OrderBy(pairing => !this.RatesANewBot(pairing))
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

    private ImmutableDictionary<string, int> ShippedAt(string ruleset) =>
        this._bots.Shipped.GetValueOrDefault(ruleset, []);

    /// <summary>The ratings held at the ruleset as they are: those shipped, and the anchor's.</summary>
    private ImmutableDictionary<string, int> HeldAt(string ruleset) =>
        this.ShippedAt(ruleset).SetItem(
            this._settings.Anchor,
            this.ShippedAt(ruleset).GetValueOrDefault(this._settings.Anchor, (int)BotRatingFit.AnchorRating)
        );

    /// <summary>
    /// Whether the pairing's games rate a bot not held: one held, the other not; or any, where only the anchor is.
    /// </summary>
    private bool RatesANewBot(BotMatchup pairing)
    {
        ImmutableDictionary<string, int> held = this.HeldAt(pairing.Ruleset);
        return held.Count == 1 || held.ContainsKey(pairing.First) != held.ContainsKey(pairing.Second);
    }

    private static bool IsOf(BotMatchup pairing, string bot) => pairing.First == bot || pairing.Second == bot;

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
