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
/// them; one not named weighs nothing). What each is worth to winning is for playing to show, not for
/// guessing: <see cref="Default"/> is a starting guess, and <see cref="Learned"/> what Sage, playing itself,
/// has tuned it to for each ruleset. Two weights are equal when they weigh the same things the same.
/// </summary>
public sealed record SageWeights(
    double Score,
    double Material,
    double Leaders,
    double Prisoners,
    double Thrones,
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

    private static readonly ImmutableList<string> Counted =
        ["score", "material", "leaders", "prisoners", "thrones", "exposure", "chances", "mobility"];

    public double Mobility { get; init; }

    public ImmutableSortedDictionary<string, double> Measures { get; init; } =
        ImmutableSortedDictionary<string, double>.Empty.WithComparers(StringComparer.Ordinal);

    /// <summary>What each weight is called, in the order of <see cref="Values"/>: the measures last.</summary>
    [JsonIgnore]
    public ImmutableList<string> Names => [.. Counted, .. this.Measures.Keys];

    /// <summary>The weights in order, for one tuning them in turn.</summary>
    [JsonIgnore]
    public ImmutableList<double> Values =>
    [
        this.Score, this.Material, this.Leaders, this.Prisoners, this.Thrones, this.Exposure, this.Chances,
        this.Mobility, .. this.Measures.Values,
    ];

    /// <summary>The same weights, but the one at <paramref name="index"/> in <see cref="Values"/>.</summary>
    public SageWeights With(int index, double value)
    {
        ImmutableList<double> values = this.Values.SetItem(index, value);
        return new SageWeights(values[0], values[1], values[2], values[3], values[4], values[5], values[6])
        {
            Mobility = values[7],
            Measures = this.Measures.SetItems(
                this.Measures.Keys.Select((name, at) => KeyValuePair.Create(name, values[Counted.Count + at]))
            ),
        };
    }

    /// <summary>The same weights, with each of <paramref name="measures"/> not weighed yet weighing nothing.</summary>
    public SageWeights Weighing([Required] IEnumerable<string> measures) =>
        this with
        {
            Measures = this.Measures.SetItems(
                measures.Where(name => !this.Measures.ContainsKey(name)).Select(name => KeyValuePair.Create(name, 0d))
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
