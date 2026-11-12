using System.Collections.Immutable;
using Net.NowhereAtAll.Zaturnava.Contracts;

namespace Net.NowhereAtAll.Zaturnava.Web;

/// <summary>
/// How strong a bot a seat is played by, for a player who would rather not choose each bot: its name ("Easy"), and
/// the bots it may seat at the ruleset, easiest first, by their ratings; or bots not rated at it, which are put
/// apart (<see cref="GameStarter.UnratedName"/>), unless none is rated.
/// </summary>
internal sealed record Strength(string Name, string RulesetId, ImmutableList<BotDto> Bots)
{
    public int Lowest => this.RatingOf(this.Bots[0]);

    public int Highest => this.RatingOf(this.Bots[^1]);

    /// <summary>
    /// "Easy bot (Elo 1000-1730)"; one rating alone, if its bots share it; none, for bots not rated at the ruleset
    /// ("Unrated bot").
    /// </summary>
    public string Label =>
        (this.Bots[0].Ratings.ContainsKey(this.RulesetId), this.Lowest == this.Highest) switch
        {
            (false, _) => $"{this.Name} bot",
            (_, true) => $"{this.Name} bot (Elo {this.Lowest})",
            _ => $"{this.Name} bot (Elo {this.Lowest}–{this.Highest})",
        };

    public int RatingOf(BotDto bot) => bot.Ratings.GetValueOrDefault(this.RulesetId);

    /// <summary>How one of its bots is offered: "Argus (Elo 2341)"; its name alone, if it is not rated.</summary>
    public string LabelOf(BotDto bot) =>
        bot.Ratings.TryGetValue(this.RulesetId, out int rating) ? $"{bot.Name} (Elo {rating})" : bot.Name;

    /// <summary>How far the bot's rating is from the middle of this strength's.</summary>
    public int DistanceTo(BotDto bot) => Math.Abs((2 * this.RatingOf(bot)) - this.Lowest - this.Highest);
}
