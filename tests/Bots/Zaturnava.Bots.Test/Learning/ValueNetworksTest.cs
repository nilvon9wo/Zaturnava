using Net.NowhereAtAll.Zaturnava.Bots.Learning;

namespace Net.NowhereAtAll.Zaturnava.Bots.Test.Learning;

public sealed class ValueNetworksTest
{
    [Fact]
    public void Learned_WhenAsked_HasAWholeNetworkForChaturaji2027()
    {
        // Act
        ValueNetworkData data = ValueNetworks.Learned["chaturaji-2027"];

        // Assert
        ValueNetwork network = ValueNetwork.From(data);
        Assert.Equal(
            (ValueNetworkData.CurrentFormat, "chaturaji-2027", data.Inputs),
            (data.Format, data.RulesetId, network.Inputs)
        );
    }
}
