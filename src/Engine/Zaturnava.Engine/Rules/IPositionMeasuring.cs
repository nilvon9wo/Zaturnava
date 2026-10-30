using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using Net.NowhereAtAll.Zaturnava.Engine.Seats;

namespace Net.NowhereAtAll.Zaturnava.Engine.Rules;

/// <summary>
/// What a ruleset can tell a bot about a position beyond what every ruleset has (points, pieces, leaders,
/// prisoners, thrones): measures of its own, each named, each a number for one seat's army, more being
/// better for it. A ruleset offers them by implementing this beside <see cref="IRulesApplying"/>; a bot
/// may weigh them, and one that does not know them plays on without.
/// </summary>
public interface IPositionMeasuring
{
    /// <summary>The measures' names, in the order <see cref="MeasuresOf"/> gives them.</summary>
    ImmutableList<string> Measures { get; }

    /// <summary>Each measure, in order, for the army of <paramref name="seat"/>.</summary>
    ImmutableList<double> MeasuresOf([Required] GameState state, SeatId seat);
}
