using Net.NowhereAtAll.Zaturnava.Bots.Learning;

namespace Net.NowhereAtAll.Zaturnava.Bots.Test.Learning;

public sealed class ValueNetworkTest
{
    private const int Inputs = 2;
    private const int Hidden = 3;
    private static readonly (int Index, double Value)[] Shown = [(1, 1d)];

    [Fact]
    public void Untrained_WhenAsked_IsOfTheSizeAsked()
    {
        // Act
        ValueNetwork network = ValueNetwork.Untrained(Inputs, Hidden, new Random(7));

        // Assert
        Assert.Equal((Inputs, Hidden), (network.Inputs, network.Hidden));
    }

    [Fact]
    public void Evaluate_WhenUntrainedAndShownNothing_JudgesItEven()
    {
        // Arrange
        ValueNetwork network = ValueNetwork.Untrained(Inputs, Hidden, new Random(7));

        // Act
        (double outcome, double margin) = network.Evaluate([]);

        // Assert
        Assert.Equal((0d, 0d), (outcome, margin));
    }

    [Fact]
    public void Learn_WhenTaughtOverAndOver_ComesToJudgeAsTaught()
    {
        // Arrange
        ValueNetwork network = ValueNetwork.Untrained(Inputs, Hidden, new Random(7));
        double first = network.Learn(Shown, 0.5, 1, 0.05);

        // Act
        double last = first;
        for (int round = 0; round < 2000; round++)
        {
            last = network.Learn(Shown, 0.5, 1, 0.05);
        }

        // Assert
        (double outcome, double margin) = network.Evaluate(Shown);
        Assert.True(last < first / 100, $"{first} then {last}");
        Assert.Equal(0.5, outcome, 0.02);
        Assert.Equal(1, margin, 0.02);
    }

    [Fact]
    public void From_WhenGivenItsOwnData_JudgesAsItDid()
    {
        // Arrange
        ValueNetwork network = ValueNetwork.Untrained(Inputs, Hidden, new Random(7));
        _ = network.Learn(Shown, 1, -1, 0.1);
        ValueNetworkData data = network.ToData("line");

        // Act
        ValueNetwork read = ValueNetwork.From(data);

        // Assert
        Assert.Equal(data, read.ToData("line"));
        Assert.Equal(network.Evaluate(Shown).Outcome, read.Evaluate(Shown).Outcome, 1e-6);
        Assert.Equal(
            (ValueNetworkData.CurrentFormat, "line", Inputs, Hidden),
            (data.Format, data.RulesetId, data.Inputs, data.Hidden)
        );
    }

    [Theory]
    [InlineData("input weights")]
    [InlineData("hidden biases")]
    [InlineData("output weights")]
    [InlineData("output biases")]
    [InlineData("direct weights")]
    public void From_WhenAPartIsNotOfTheSizeItSays_ThrowsArgumentException(string part)
    {
        // Arrange - 6 input weights, 3 hidden biases, 6 output weights, 2 output biases, 4 direct; one swapped
        ValueNetworkData data = ValueNetwork.Untrained(Inputs, Hidden, new Random(7)).ToData("line");
        ValueNetworkData wrong = part switch
        {
            "input weights" => data with { InputWeights = data.HiddenBiases },
            "hidden biases" => data with { HiddenBiases = data.OutputBiases },
            "output weights" => data with { OutputWeights = data.HiddenBiases },
            "output biases" => data with { OutputBiases = data.HiddenBiases },
            _ => data with { DirectWeights = data.HiddenBiases },
        };

        // Act
        ArgumentException thrown = Assert.Throws<ArgumentException>(() => ValueNetwork.From(wrong));

        // Assert
        Assert.Equal("data", thrown.ParamName);
    }
}
