using System.Collections.Immutable;
using System.Globalization;
using Net.NowhereAtAll.Zaturnava.Contracts;

namespace Net.NowhereAtAll.Zaturnava.Web;

/// <summary>
/// How strong a bot a seat is played by, for a player who would rather not choose each bot: its name ("Easy"), and
/// the bots it may seat at the ruleset, easiest first, by their ratings; or bots not rated at it, which are put
/// apart (<see cref="GameStarter.UnratedName"/>), unless none is rated. Its ratings shown by the
/// <see cref="System"/> chosen on the leaderboard; ordered by Elo, which a bot's other ratings are counted from, and
/// so order alike.
/// </summary>
internal sealed record Strength(string Name, string RulesetId, ImmutableList<BotDto> Bots)
{
    /// <summary>The rating system its ratings are shown by: Elo, unless chosen otherwise on the leaderboard.</summary>
    public RatingSystem System { get; init; }

    public int Lowest => this.RatingOf(this.Bots[0]);

    public int Highest => this.RatingOf(this.Bots[^1]);

    /// <summary>
    /// "Easy bot (Elo 1000-1730)", or by another system ("TrueSkill 12.3-24.6"); one rating alone, if its bots share
    /// it; none, for bots not rated at the ruleset ("Unrated bot").
    /// </summary>
    public string Label
    {
        get
        {
            (string lowest, string highest) = (this.Shown(this.Bots[0]), this.Shown(this.Bots[^1]));
            return (this.Bots[0].Ratings.ContainsKey(this.RulesetId), lowest == highest) switch
            {
                (false, _) => $"{this.Name} bot",
                (_, true) => $"{this.Name} bot ({this.SystemName} {lowest})",
                _ => $"{this.Name} bot ({this.SystemName} {lowest}–{highest})",
            };
        }
    }

    public int RatingOf(BotDto bot) => bot.Ratings.GetValueOrDefault(this.RulesetId);

    /// <summary>How one of its bots is offered: "Argus (Elo 2341)"; its name alone, if it is not rated.</summary>
    public string LabelOf(BotDto bot) =>
        bot.Ratings.ContainsKey(this.RulesetId) ? $"{bot.Name} ({this.SystemName} {this.Shown(bot)})" : bot.Name;

    /// <summary>How far the bot's rating is from the middle of this strength's.</summary>
    public int DistanceTo(BotDto bot) => Math.Abs((2 * this.RatingOf(bot)) - this.Lowest - this.Highest);

    private string SystemName => RatingSystems.NameOf(this.System);

    /// <summary>
    /// The bot's rating at the ruleset by the system chosen: its TrueSkill, as the server counts it from its Elo
    /// wherever it is rated, or its Elo, which its Glicko-2 rating is.
    /// </summary>
    private string Shown(BotDto bot) =>
        this.System == RatingSystem.TrueSkill
            ? bot.TrueSkills.GetValueOrDefault(this.RulesetId).ToString("0.0", CultureInfo.InvariantCulture)
            : this.RatingOf(bot).ToString(CultureInfo.InvariantCulture);
}
