using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Net.NowhereAtAll.Zaturnava.Engine.Rules;

namespace Net.NowhereAtAll.Zaturnava.Bots;

/// <summary>
/// How much each thing Sage counts in a position weighs (<see cref="Appraisal"/>): the points scored, the
/// pieces on the board, the leaders among them, the prisoners held, the thrones held, the worst the enemy
/// could win next, the most the team could, how many cells its pieces attack (<see cref="Mobility"/>), and
/// the ruleset's own measures, by name (<see cref="Measures"/>, as <see cref="IPositionMeasuring"/> offers
/// them; one not named weighs nothing). All but the worst and the most are weighed apart for the bot's own seat,
/// its allies and its enemies (<see cref="Sided"/>). What each is worth to winning is for playing to show, not for
/// guessing: <see cref="Default"/> is a starting guess, and <see cref="Learned"/> what Sage, playing itself,
/// has tuned it to for each ruleset. Two weights are equal when they weigh the same things the same.
/// </summary>
public sealed record SageWeights(
    Sided Score,
    Sided Material,
    Sided Leaders,
    Sided Prisoners,
    Sided Thrones,
    double Exposure,
    double Chances
)
{
    /// <summary>Where the learned weights are kept, in this assembly: one JSON file for each ruleset's id.</summary>
    private const string LearnedFolder = "Weights/";

    public static SageWeights Default { get; } = new(
        Score: 1,
        Material: 1,
        Leaders: 2,
        Prisoners: 0.5,
        Thrones: 3,
        Exposure: 1,
        Chances: 0.3
    );

    /// <summary>The weights Sage has learned for each ruleset, by its id; a ruleset not there has none yet.</summary>
    public static ImmutableDictionary<string, SageWeights> Learned { get; } = LearnedIn(typeof(SageWeights).Assembly);

    /// <summary>Where <see cref="Mobility"/>'s weights start, in <see cref="Values"/>: the measures' follow.</summary>
    private const int MobilityAt = 17;

    public Sided Mobility { get; init; }

    public ImmutableSortedDictionary<string, Sided> Measures { get; init; } =
        ImmutableSortedDictionary<string, Sided>.Empty.WithComparers(StringComparer.Ordinal);

    /// <summary>
    /// What each weight is called, in the order of <see cref="Values"/>: a sided one's three by its side
    /// ("material.own", "material.ally", "material.enemy"); the measures last.
    /// </summary>
    [JsonIgnore]
    public ImmutableList<string> Names =>
    [
        .. SidesOf("score"), .. SidesOf("material"), .. SidesOf("leaders"), .. SidesOf("prisoners"),
        .. SidesOf("thrones"), "exposure", "chances", .. SidesOf("mobility"), .. this.Measures.Keys.SelectMany(SidesOf),
    ];

    /// <summary>The weights in order, for one tuning them in turn.</summary>
    [JsonIgnore]
    public ImmutableList<double> Values =>
    [
        .. this.Score.Values, .. this.Material.Values, .. this.Leaders.Values, .. this.Prisoners.Values,
        .. this.Thrones.Values, this.Exposure, this.Chances, .. this.Mobility.Values,
        .. this.Measures.Values.SelectMany(weight => weight.Values),
    ];

    /// <summary>The same weights, but the one at <paramref name="index"/> in <see cref="Values"/>.</summary>
    public SageWeights With(int index, double value)
    {
        ImmutableList<double> values = this.Values.SetItem(index, value);
        return new SageWeights(
            Sided.At(values, 0),
            Sided.At(values, 3),
            Sided.At(values, 6),
            Sided.At(values, 9),
            Sided.At(values, 12),
            values[15],
            values[16]
        )
        {
            Mobility = Sided.At(values, MobilityAt),
            Measures = this.Measures.SetItems(
                this.Measures.Keys.Select((name, at) =>
                    KeyValuePair.Create(name, Sided.At(values, MobilityAt + (Sided.Sides.Count * (at + 1))))
                )
            ),
        };
    }

    /// <summary>The same weights, with each of <paramref name="measures"/> not weighed yet weighing nothing.</summary>
    public SageWeights Weighing([Required] IEnumerable<string> measures) =>
        this with
        {
            Measures = this.Measures.SetItems(
                measures
                    .Where(name => !this.Measures.ContainsKey(name))
                    .Select(name => KeyValuePair.Create(name, Sided.Alike(0)))
            ),
        };

    /// <summary>The same weights, weighing every measure <paramref name="rules"/> offer, if they offer any.</summary>
    public SageWeights WeighingMeasuresOf([Required] IRulesApplying rules) =>
        rules is IPositionMeasuring measuring
            ? this.Weighing(measuring.Measures)
            : this;

    public bool Equals(SageWeights? other) =>
        other is not null && this.Names.SequenceEqual(other.Names) && this.Values.SequenceEqual(other.Values);

    public override int GetHashCode() => HashCode.Combine(this.Score, this.Material, this.Measures.Count);

    private static IEnumerable<string> SidesOf(string name) => Sided.Sides.Select(side => $"{name}.{side}");

    private static ImmutableDictionary<string, SageWeights> LearnedIn(Assembly assembly) =>
        assembly
            .GetManifestResourceNames()
            .Where(name => name.StartsWith(LearnedFolder, StringComparison.Ordinal))
            .ToImmutableDictionary(
                name => Path.GetFileNameWithoutExtension(name[LearnedFolder.Length..]),
                name => Read(assembly, name)
            );

    private static SageWeights Read(Assembly assembly, string name)
    {
        using Stream stream = assembly.GetManifestResourceStream(name)!; // listed by the assembly itself
        return JsonSerializer.Deserialize<SageWeights>(stream, JsonSerializerOptions.Web)!; // ours, so whole
    }
}
