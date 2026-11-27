using System.ComponentModel.DataAnnotations;
using Net.NowhereAtAll.Zaturnava.Contracts;

namespace Net.NowhereAtAll.Zaturnava.Presentation;

/// <summary>How the game would end for a seat's team, were it ended now on points.</summary>
public enum EndStanding
{
    Victory,
    Draw,
    Defeat,
}

/// <summary>
/// What ending the game now would mean for a seat, for the button that proposes it and the question that asks it
/// (2026-11-26: "let's make the negotiated End Game a less neutral experience"): whether its team would win, draw or
/// lose, by points alone, as points alone decide; and how sure that outcome is, from 0, as open as can be, to 1, past
/// doubt, by points and the pieces each side has left together, so that the others are likely to agree. A side ten
/// times as strong is past doubt; and one with ten times the points and ten times the pieces, a sure thing.
/// </summary>
public sealed record EndOutlook(EndStanding Standing, double Certainty, bool IsSure)
{
    /// <summary>How many times stronger a side must be for the outcome to be past doubt.</summary>
    public const double Overwhelming = 10;

    /// <summary>What the button proposing an end says: "Declare Victory".</summary>
    public string Proposal =>
        this.Standing switch
        {
            EndStanding.Victory => "Declare Victory",
            EndStanding.Defeat => "Concede Defeat",
            _ => "Propose Draw",
        };

    /// <summary>What a player asked to end the game is asked: "Accept Defeat?".</summary>
    public string Question => $"Accept {this.Standing}?";

    /// <summary>
    /// The outlook for <paramref name="seat"/>'s team: its points against the best of every other team's (a draw,
    /// with no other team), and, but for a draw, which is as open as can be, the stronger of the two against the
    /// weaker, points and the worth of the pieces on the board taken together.
    /// </summary>
    public static EndOutlook Of([Required] GameViewDto view, int seat)
    {
        int team = view.Seats[seat].Team;
        Side mine = SideOf(view, team);
        Side? rival = view.Seats
            .Select(other => other.Team)
            .Where(other => other != team)
            .Distinct()
            .Select(other => SideOf(view, other))
            .OrderByDescending(other => other.Points)
            .ThenByDescending(other => other.Material)
            .FirstOrDefault();
        return mine.Points.CompareTo(rival?.Points ?? mine.Points) switch
        {
            > 0 => Yielded(Decided(EndStanding.Victory, mine, rival!), WeakestBeaten(view, team)),
            < 0 => Decided(EndStanding.Defeat, rival!, mine),
            _ => new EndOutlook(EndStanding.Draw, 0, false),
        };
    }

    /// <summary>
    /// A victory, as sure as every enemy army is beaten, each having to agree (the user, 2026-11-27, of a Grand Legions
    /// game in which Yellow had scored nothing: "the red army still has literally all their pieces but the 'Declare
    /// Victory' button is already visibly green"): no surer than the share of its worth the least beaten has lost, nor
    /// past doubt until each has lost all but a tenth (<see cref="Overwhelming"/>).
    /// </summary>
    private static EndOutlook Yielded(EndOutlook outlook, double beaten) =>
        outlook with
        {
            Certainty = Math.Min(outlook.Certainty, beaten),
            IsSure = outlook.IsSure && beaten >= 1 - (1 / Overwhelming),
        };

    /// <summary>
    /// How beaten the least beaten army not of <paramref name="team"/> is, of those still playing: the share of its
    /// worth taken prisoner by anyone, against what it has on the board besides; one with nothing either way is beaten
    /// wholly, as is the game with no such army.
    /// </summary>
    private static double WeakestBeaten(GameViewDto view, int team) =>
        view.Seats
            .Where(seat => seat.Team != team && !seat.IsRetired)
            .Select(army =>
            {
                int lost = view.Seats
                    .SelectMany(holder => holder.Prisoners)
                    .Where(prisoner => prisoner.Owner == army.Id)
                    .Sum(prisoner => WorthOf(view, prisoner.Kind));
                int left = view.Pieces.Where(piece => piece.Owner == army.Id).Sum(piece => WorthOf(view, piece.Kind));
                return lost + left == 0 ? 1 : lost / (double)(lost + left);
            })
            .Append(1)
            .Min();

    private static int WorthOf(GameViewDto view, string kind) =>
        view.Kinds.Find(each => each.Kind == kind)?.Value ?? 0;

    private static EndOutlook Decided(EndStanding standing, Side ahead, Side behind) =>
        new(
            standing,
            Math.Clamp(Math.Log10(Ratio(ahead.Points + ahead.Material, behind.Points + behind.Material)), 0, 1),
            Ratio(ahead.Points, behind.Points) >= Overwhelming && Ratio(ahead.Material, behind.Material) >= Overwhelming
        );

    /// <summary>How many times <paramref name="more"/> is <paramref name="less"/>, nothing counted as one.</summary>
    private static double Ratio(int more, int less) => Math.Max(1, more) / (double)Math.Max(1, less);

    private static Side SideOf(GameViewDto view, int team)
    {
        HashSet<int> seats = [.. view.Seats.Where(seat => seat.Team == team).Select(seat => seat.Id)];
        return new Side(
            view.Seats.Where(seat => seats.Contains(seat.Id)).Sum(seat => seat.Score),
            view.Pieces
                .Where(piece => seats.Contains(piece.Owner))
                .Sum(piece => WorthOf(view, piece.Kind))
        );
    }

    /// <summary>A team's points, and what its pieces on the board are worth.</summary>
    private sealed record Side(int Points, int Material);
}
