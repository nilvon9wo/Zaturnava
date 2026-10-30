using System.ComponentModel.DataAnnotations;
using Net.NowhereAtAll.Zaturnava.Engine.Decisions;
using Net.NowhereAtAll.Zaturnava.Engine.Rules;

namespace Net.NowhereAtAll.Zaturnava.Bots;

/// <summary>
/// How a position is judged: what <see cref="Foresight"/> looks ahead to.
/// </summary>
internal interface IPositionValuing
{
    /// <summary>
    /// What <paramref name="state"/> is worth to the team of the seat deciding <paramref name="request"/>,
    /// more being better.
    /// </summary>
    double ValueOf([Required] DecisionRequest request, [Required] GameState state);
}

/// <summary>A position judged as Sage judges it: <see cref="Appraisal"/>, with <paramref name="weights"/>.</summary>
internal sealed class Appraiser(SageWeights weights) : IPositionValuing
{
    private readonly SageWeights _weights = weights;

    public double ValueOf([Required] DecisionRequest request, [Required] GameState state) =>
        Appraisal.ValueOf(request, state, this._weights);
}
