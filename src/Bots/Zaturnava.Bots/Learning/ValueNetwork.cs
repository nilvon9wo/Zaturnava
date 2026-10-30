using System.ComponentModel.DataAnnotations;

namespace Net.NowhereAtAll.Zaturnava.Bots.Learning;

/// <summary>
/// A small neural network judging a position, as <see cref="PositionEncoder"/> gives it: one hidden layer of
/// <see cref="Hidden"/> units, and two outputs, each also weighing the inputs directly, as a plain weighted sum
/// would: so that what is simply more or less (pieces, points) counts the same in positions no game it learned
/// from came to. The first is how the game will end for the seat's team, from
/// -1 (lost) through 0 (drawn) to 1 (won); the second, by how many points its team will be ahead at the end, in
/// <see cref="MarginScale"/>s. It learns a position at a time (<see cref="Learn"/>), towards how a game went.
/// </summary>
public sealed class ValueNetwork
{
    /// <summary>The points a margin of 1 is: about as many as a game is won by.</summary>
    public const double MarginScale = 30;

    private const int Outputs = 2;

    private readonly double[] _inputWeights;
    private readonly double[] _hiddenBiases;
    private readonly double[] _outputWeights;
    private readonly double[] _outputBiases;
    private readonly double[] _directWeights;

    private ValueNetwork(
        int inputs,
        int hidden,
        double[] inputWeights,
        double[] hiddenBiases,
        double[] outputWeights,
        double[] outputBiases,
        double[] directWeights
    )
    {
        this.Inputs = inputs;
        this.Hidden = hidden;
        this._inputWeights = inputWeights;
        this._hiddenBiases = hiddenBiases;
        this._outputWeights = outputWeights;
        this._outputBiases = outputBiases;
        this._directWeights = directWeights;
    }

    public int Inputs { get; }

    public int Hidden { get; }

    /// <summary>
    /// A network that has learned nothing yet: small weights to and from its hidden units, at random; no
    /// biases, and no direct weights.
    /// </summary>
    public static ValueNetwork Untrained(
        [Range(1, int.MaxValue)] int inputs,
        [Range(1, int.MaxValue)] int hidden,
        [Required] Random random
    ) =>
        new(
            inputs,
            hidden,
            Small(inputs * hidden, 1 / Math.Sqrt(hidden), random),
            new double[hidden],
            Small(Outputs * hidden, 1 / Math.Sqrt(hidden), random),
            new double[Outputs],
            new double[inputs * Outputs]
        );

    /// <summary>
    /// A network that judges as a weighted sum of its inputs does, each weighed as <paramref name="weighing"/>
    /// says (one not there weighing nothing), in <paramref name="scale"/>s: the outcome expected, the sum's tanh,
    /// and the margin, the sum itself. Its hidden units have small weights in, at random, but none out yet, so
    /// they change nothing until it learns: what it learns corrects the sum.
    /// </summary>
    public static ValueNetwork Judging(
        [Range(1, int.MaxValue)] int inputs,
        [Range(1, int.MaxValue)] int hidden,
        [Required] IEnumerable<(int Index, double Weight)> weighing,
        double scale,
        [Required] Random random
    )
    {
        double[] directWeights = new double[inputs * Outputs];
        foreach ((int index, double weight) in weighing)
        {
            directWeights[index * Outputs] = weight / scale;
            directWeights[(index * Outputs) + 1] = weight / scale;
        }

        return new ValueNetwork(
            inputs,
            hidden,
            Small(inputs * hidden, 1 / Math.Sqrt(hidden), random),
            new double[hidden],
            new double[Outputs * hidden],
            new double[Outputs],
            directWeights
        );
    }

    /// <summary>The network <paramref name="data"/> keeps.</summary>
    /// <exception cref="ArgumentException">
    /// The data does not hold a network of its own size, or holds a weight that is no number (one that learned
    /// past all bounds).
    /// </exception>
    public static ValueNetwork From([Required] ValueNetworkData data)
    {
        double[] inputWeights = Unpacked(data.InputWeights);
        double[] hiddenBiases = Unpacked(data.HiddenBiases);
        double[] outputWeights = Unpacked(data.OutputWeights);
        double[] outputBiases = Unpacked(data.OutputBiases);
        double[] directWeights = Unpacked(data.DirectWeights);
        bool isWhole = inputWeights.Concat(hiddenBiases).Concat(outputWeights).Concat(outputBiases)
                .Concat(directWeights)
                .All(double.IsFinite)
            && inputWeights.Length == data.Inputs * data.Hidden
            && hiddenBiases.Length == data.Hidden
            && outputWeights.Length == Outputs * data.Hidden
            && outputBiases.Length == Outputs
            && directWeights.Length == data.Inputs * Outputs;
        return isWhole
            ? new ValueNetwork(
                data.Inputs,
                data.Hidden,
                inputWeights,
                hiddenBiases,
                outputWeights,
                outputBiases,
                directWeights
            )
            : throw new ArgumentException("The network's data is not of the size it says.", nameof(data));
    }

    /// <summary>The network, to keep: what <see cref="From"/> reads back.</summary>
    public ValueNetworkData ToData([Required] string rulesetId) =>
        new(
            ValueNetworkData.CurrentFormat,
            rulesetId,
            this.Inputs,
            this.Hidden,
            Packed(this._inputWeights),
            Packed(this._hiddenBiases),
            Packed(this._outputWeights),
            Packed(this._outputBiases),
            Packed(this._directWeights)
        );

    /// <summary>How the game will end for the seat's team, and by how many margins it will lead.</summary>
    public (double Outcome, double Margin) Evaluate([Required] IReadOnlyList<(int Index, double Value)> input)
    {
        double[] hidden = this.HiddenOf(input);
        return (Math.Tanh(this.Output(0, input, hidden)), this.Output(1, input, hidden));
    }

    /// <summary>
    /// Moves the network a step of <paramref name="rate"/> towards judging the position as
    /// <paramref name="outcome"/> and <paramref name="margin"/>; returns how far off it was before, squared.
    /// </summary>
    public double Learn(
        [Required] IReadOnlyList<(int Index, double Value)> input,
        double outcome,
        double margin,
        double rate
    )
    {
        double[] hidden = this.HiddenOf(input);
        double judged = Math.Tanh(this.Output(0, input, hidden));
        double led = this.Output(1, input, hidden);
        double outcomeError = (judged - outcome) * (1 - (judged * judged));
        double marginError = led - margin;
        double[] hiddenErrors =
        [
            .. hidden.Select((value, unit) =>
                ((outcomeError * this._outputWeights[unit]) + (marginError * this._outputWeights[this.Hidden + unit]))
                * (1 - (value * value))
            ),
        ];
        for (int unit = 0; unit < this.Hidden; unit++)
        {
            this._outputWeights[unit] -= rate * outcomeError * hidden[unit];
            this._outputWeights[this.Hidden + unit] -= rate * marginError * hidden[unit];
            this._hiddenBiases[unit] -= rate * hiddenErrors[unit];
        }

        this._outputBiases[0] -= rate * outcomeError;
        this._outputBiases[1] -= rate * marginError;
        foreach ((int index, double value) in input)
        {
            for (int unit = 0; unit < this.Hidden; unit++)
            {
                this._inputWeights[(index * this.Hidden) + unit] -= rate * value * hiddenErrors[unit];
            }

            this._directWeights[index * Outputs] -= rate * value * outcomeError;
            this._directWeights[(index * Outputs) + 1] -= rate * value * marginError;
        }

        return ((judged - outcome) * (judged - outcome)) + ((led - margin) * (led - margin));
    }

    private double[] HiddenOf(IReadOnlyList<(int Index, double Value)> input)
    {
        double[] sums = [.. this._hiddenBiases];
        foreach ((int index, double value) in input)
        {
            for (int unit = 0; unit < this.Hidden; unit++)
            {
                sums[unit] += value * this._inputWeights[(index * this.Hidden) + unit];
            }
        }

        return [.. sums.Select(Math.Tanh)];
    }

    private double Output(int output, IReadOnlyList<(int Index, double Value)> input, double[] hidden) =>
        this._outputBiases[output]
        + hidden.Select((value, unit) => value * this._outputWeights[(output * this.Hidden) + unit]).Sum()
        + input.Sum(entry => entry.Value * this._directWeights[(entry.Index * Outputs) + output]);

    private static double[] Small(int count, double size, Random random) =>
        [.. Enumerable.Range(0, count).Select(_ => ((random.NextDouble() * 2) - 1) * size)];

    /// <summary>The numbers as single-precision floats in base 64: far smaller than as text.</summary>
    private static string Packed(double[] values)
    {
        byte[] bytes = new byte[values.Length * sizeof(float)];
        Buffer.BlockCopy(values.Select(value => (float)value).ToArray(), 0, bytes, 0, bytes.Length);
        return Convert.ToBase64String(bytes);
    }

    private static double[] Unpacked(string packed)
    {
        byte[] bytes = Convert.FromBase64String(packed);
        float[] values = new float[bytes.Length / sizeof(float)];
        Buffer.BlockCopy(bytes, 0, values, 0, values.Length * sizeof(float));
        return [.. values.Select(value => (double)value)];
    }
}

/// <summary>
/// A <see cref="ValueNetwork"/> as kept: for which ruleset, its size, and its weights and biases, packed; the
/// direct weights, from each input to each output in turn, last.
/// <see cref="Format"/> changes only if the shape does.
/// </summary>
public sealed record ValueNetworkData(
    int Format,
    string RulesetId,
    int Inputs,
    int Hidden,
    string InputWeights,
    string HiddenBiases,
    string OutputWeights,
    string OutputBiases,
    string DirectWeights
)
{
    public const int CurrentFormat = 1;
}
