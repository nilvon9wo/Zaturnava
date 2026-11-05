using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Net.NowhereAtAll.Zaturnava.Bots;

/// <summary>
/// One thing Sage counts, told apart by whose it is (Proposed): the bot's own seat's, its allies'
/// and its enemies'. As a count, how much of it each side has; as a weight, how much each side's counts (an enemy's
/// against the bot: <see cref="Weigh"/>). The same weight for all three weighs as the team's less its enemies' did,
/// before they were told apart; and in JSON, a single number is such a weight, as weights learned before were kept.
/// </summary>
[JsonConverter(typeof(SidedConverter))]
public readonly record struct Sided(double Own, double Ally, double Enemy)
{
    /// <summary>What each side is called, in the order of <see cref="Values"/>.</summary>
    public static ImmutableList<string> Sides { get; } = ["own", "ally", "enemy"];

    /// <summary>The three, in order.</summary>
    public ImmutableList<double> Values => [this.Own, this.Ally, this.Enemy];

    /// <summary>The same for every side: weighing as the team's less its enemies' did.</summary>
    public bool IsAlike => this == Alike(this.Own);

    /// <summary>The team's, less its enemies': the counts weighed alike.</summary>
    public double Net => this.Own + this.Ally - this.Enemy;

    /// <summary>The three weights' mean: as near as one weight for the team's, less its enemies', comes.</summary>
    public double Mean => (this.Own + this.Ally + this.Enemy) / Sides.Count;

    /// <summary>The same weight for every side.</summary>
    public static Sided Alike(double weight) => new(weight, weight, weight);

    /// <summary>The same weight for every side, as a single number weighed before they were told apart.</summary>
    public static Sided FromDouble(double weight) => Alike(weight);

    public static implicit operator Sided(double weight) => Alike(weight);

    /// <summary>The three at <paramref name="index"/> of <paramref name="values"/>, in order.</summary>
    public static Sided At([Required] IReadOnlyList<double> values, int index) =>
        new(values[index], values[index + 1], values[index + 2]);

    /// <summary>What <paramref name="counts"/> are worth to the bot: its own and allies', less enemies'.</summary>
    public double Weigh(Sided counts) =>
        (this.Own * counts.Own) + (this.Ally * counts.Ally) - (this.Enemy * counts.Enemy);
}

/// <summary>A weight alike for every side as a single number; else as its three, by name.</summary>
internal sealed class SidedConverter : JsonConverter<Sided>
{
    public override Sided Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        reader.TokenType == JsonTokenType.Number
            ? Sided.Alike(reader.GetDouble())
            : ToSided(JsonSerializer.Deserialize<Parts>(ref reader, options)!); // the weights' own files, so whole

    public override void Write(Utf8JsonWriter writer, Sided value, JsonSerializerOptions options)
    {
        if (value.IsAlike)
        {
            writer.WriteNumberValue(value.Own);
        }
        else
        {
            JsonSerializer.Serialize(writer, new Parts(value.Own, value.Ally, value.Enemy), options);
        }
    }

    private static Sided ToSided(Parts parts) => new(parts.Own, parts.Ally, parts.Enemy);

    private sealed record Parts(double Own, double Ally, double Enemy);
}
