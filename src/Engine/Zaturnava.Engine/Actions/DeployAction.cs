using System.ComponentModel.DataAnnotations;
using Net.NowhereAtAll.Zaturnava.Engine.Boards;
using Net.NowhereAtAll.Zaturnava.Engine.Pieces;
using Net.NowhereAtAll.Zaturnava.Engine.Seats;

namespace Net.NowhereAtAll.Zaturnava.Engine.Actions;

/// <summary>Place the hired mercenary <see cref="Piece"/> on the board, at <see cref="Home"/>.</summary>
public sealed record DeployAction(SeatId Actor, PieceId Piece, CellId Home) : GameAction(Actor)
{
    public const string Kind = "deploy";
    public const string PieceArgument = "piece";
    public const string HomeArgument = "home";

    public override ActionDescriptor Describe()
    {
        string piece = Text(this.Piece.Value);
        string home = Text(this.Home.Value);
        return new ActionDescriptor(Kind, this.ActorArguments().Add(PieceArgument, piece).Add(HomeArgument, home));
    }

    public static DeployAction Parse([Required] ActionDescriptor descriptor) =>
        new(
            descriptor.Actor(),
            new PieceId(descriptor.IntegerArgument(PieceArgument)),
            descriptor.CellArgument(HomeArgument)
        );
}
