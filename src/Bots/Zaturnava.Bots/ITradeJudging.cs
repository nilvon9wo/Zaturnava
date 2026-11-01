using System.ComponentModel.DataAnnotations;
using Net.NowhereAtAll.Zaturnava.Engine.Decisions;
using Net.NowhereAtAll.Zaturnava.Engine.Pieces;

namespace Net.NowhereAtAll.Zaturnava.Bots;

/// <summary>
/// How a bot weighs a ransom, whether it makes the offer or answers it: each bot in its own way; and, whatever
/// its way, only a trade its team can afford (<see cref="Trading"/>).
/// </summary>
internal interface ITradeJudging
{
    /// <summary>What <paramref name="offer"/>, accepted, gains the bot deciding <paramref name="request"/>.</summary>
    double GainOf([Required] DecisionRequest request, [Required] RansomOffer offer);
}

/// <summary>
/// By the worth of the prisoners alone: what comes back to the bot, less what it sends away; and, if it
/// <paramref name="countsPoints"/>, the points accepting scores, for the bot if it receives the offer, else
/// against it.
/// </summary>
internal sealed class ByWorth(bool countsPoints) : ITradeJudging
{
    /// <summary>Solomon's way: a Pawn for a Pawn is fair, whatever it scores.</summary>
    public static ByWorth Alone { get; } = new(false);

    /// <summary>Midas's way: if it is on the scoreboard, he counts it.</summary>
    public static ByWorth AndPoints { get; } = new(true);

    private readonly bool _countsPoints = countsPoints;

    public double GainOf([Required] DecisionRequest request, [Required] RansomOffer offer) =>
        WorthGainOf(request, offer) + this.PointsOf(request, offer);

    /// <summary>What the prisoners coming back to the bot are worth, less those it sends away.</summary>
    public static int WorthGainOf(DecisionRequest request, RansomOffer offer)
    {
        int given = Trading.WorthOf(request, offer.Given);
        int asked = Trading.WorthOf(request, offer.Asked);
        return offer.From == request.Seat
            ? asked - given
            : given - asked;
    }

    private int PointsOf(DecisionRequest request, RansomOffer offer)
    {
        int points = this._countsPoints
            ? request.Rules.RansomPointsOf(request.State, offer)
            : 0;
        return offer.To == request.Seat
            ? points
            : -points;
    }
}

/// <summary>
/// Argus's way: by worth, as Solomon weighs it, less how much more the worst an enemy could do in reply would be
/// with the prisoners home (<see cref="Careful.Exposure"/>): a piece back where it can be taken at once is worth
/// little.
/// </summary>
internal sealed class ByExposure : ITradeJudging
{
    public static ByExposure Instance { get; } = new();

    public double GainOf([Required] DecisionRequest request, [Required] RansomOffer offer) =>
        ByWorth.WorthGainOf(request, offer)
        - Careful.Exposure(request, Trading.After(request, offer))
        + Careful.Exposure(request, request.State);
}

/// <summary>
/// Sage's way: by the whole position, as <paramref name="valuing"/> weighs it, once the trade is done, against as
/// it stands: pieces, prisoners, thrones, exposure and the rest; but not the points accepting scores, which only
/// Midas cares for.
/// </summary>
internal sealed class ByPosition(IPositionValuing valuing) : ITradeJudging
{
    private readonly IPositionValuing _valuing = valuing;

    public double GainOf([Required] DecisionRequest request, [Required] RansomOffer offer) =>
        this._valuing.ValueOf(request, Trading.After(request, offer) with { Scores = request.State.Scores })
        - this._valuing.ValueOf(request, request.State);
}
