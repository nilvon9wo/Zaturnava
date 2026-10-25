using Net.NowhereAtAll.Zaturnava.Contracts;
using Net.NowhereAtAll.Zaturnava.Engine.Actions;

namespace Net.NowhereAtAll.Zaturnava.Server.Test.Endpoints;

/// <summary>
/// The contracts name the well-known arguments for clients, which cannot see the engine; the server
/// passes the engine's arguments through unchanged, so the two must agree.
/// </summary>
public sealed class ActionArgumentsTest
{
    [Fact]
    public void Names_WhenComparedWithTheEngines_AreTheSame()
    {
        // Arrange - nothing beyond the constants

        // Act
        (string, string, string) contracts = (ActionArguments.Actor, ActionArguments.From, ActionArguments.To);

        // Assert
        Assert.Equal((ActionDescriptor.ActorArgument, MoveAction.FromArgument, MoveAction.ToArgument), contracts);
    }

    [Fact]
    public void PromotionNames_WhenComparedWithTheEngines_AreTheSame()
    {
        // Arrange - nothing beyond the constants

        // Act
        (string, string) contracts = (ActionArguments.Cell, ActionArguments.Into);

        // Assert
        Assert.Equal((PromoteAction.CellArgument, PromoteAction.IntoArgument), contracts);
    }

    [Fact]
    public void OfferNames_WhenComparedWithTheEngines_AreTheSame()
    {
        // Arrange - nothing beyond the constants

        // Act
        (string, string, string, string) contracts =
            (OfferDto.Kind, OfferDto.ReceiverArgument, OfferDto.GivenArgument, OfferDto.AskedArgument);

        // Assert
        Assert.Equal(
            (OfferAction.Kind, OfferAction.ReceiverArgument, OfferAction.GivenArgument, OfferAction.AskedArgument),
            contracts
        );
    }
}
