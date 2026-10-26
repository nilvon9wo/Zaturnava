using System.ComponentModel.DataAnnotations;
using Net.NowhereAtAll.Zaturnava.Engine.Boards;
using Net.NowhereAtAll.Zaturnava.Engine.Pieces;
using Net.NowhereAtAll.Zaturnava.Engine.Seats;

namespace Net.NowhereAtAll.Zaturnava.Engine.Actions;

/// <summary>Let the captured piece <see cref="Piece"/> escape, back onto the board at <see cref="Home"/>.</summary>
public sealed record EscapeAction(SeatId Actor, PieceId Piece, CellId Home) : GameAction(Actor)
{
    public const string Kind = "escape";
    public const string PieceArgument = "piece";
    public const string HomeArgument = "home";

    public override ActionDescriptor Describe()
    {
        string piece = Text(this.Piece.Value);
        string home = Text(this.Home.Value);
        return new ActionDescriptor(Kind, this.ActorArguments().Add(PieceArgument, piece).Add(HomeArgument, home));
    }

    public static EscapeAction Parse([Required] ActionDescriptor descriptor)
    {
        SeatId actor = descriptor.Actor();
        PieceId piece = new(descriptor.IntegerArgument(PieceArgument));
        CellId home = descriptor.CellArgument(HomeArgument);
        return new EscapeAction(actor, piece, home);
    }
}
