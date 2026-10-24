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
}
