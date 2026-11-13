using System.ComponentModel.DataAnnotations;
using Net.NowhereAtAll.Zaturnava.Engine.Pieces;
using Net.NowhereAtAll.Zaturnava.Engine.Seats;

namespace Net.NowhereAtAll.Zaturnava.Engine.Actions;

/// <summary>
/// Hire the mercenary <see cref="Piece"/>, a prisoner the actor holds, for points: it becomes the actor's own, to be
/// placed on the board later (<see cref="DeployAction"/>, <see cref="Rules.GameState.Hired"/>).
/// </summary>
public sealed record HireAction(SeatId Actor, PieceId Piece) : GameAction(Actor)
{
    public const string Kind = "hire";
    public const string PieceArgument = "piece";

    public override ActionDescriptor Describe() =>
        new(Kind, this.ActorArguments().Add(PieceArgument, Text(this.Piece.Value)));

    public static HireAction Parse([Required] ActionDescriptor descriptor) =>
        new(descriptor.Actor(), new PieceId(descriptor.IntegerArgument(PieceArgument)));
}
