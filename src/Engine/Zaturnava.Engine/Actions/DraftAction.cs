using System.ComponentModel.DataAnnotations;
using Net.NowhereAtAll.Zaturnava.Engine.Boards;
using Net.NowhereAtAll.Zaturnava.Engine.Pieces;
using Net.NowhereAtAll.Zaturnava.Engine.Seats;

namespace Net.NowhereAtAll.Zaturnava.Engine.Actions;

/// <summary>
/// Buy a piece of kind <see cref="Piece"/> for one's army, at its price, and place it on <see cref="Cell"/>.
/// </summary>
public sealed record DraftAction(SeatId Actor, PieceKindId Piece, CellId Cell) : GameAction(Actor)
{
    public const string Kind = "draft";
    public const string PieceArgument = "piece";
    /// <summary>"home", as an escape's: a cell chosen once the button is.</summary>
    public const string CellArgument = "home";

    public override ActionDescriptor Describe() =>
        new(
            Kind,
            this.ActorArguments().Add(PieceArgument, this.Piece.Value).Add(CellArgument, Text(this.Cell.Value))
        );

    /// <exception cref="MalformedActionException">The argument naming the piece is missing.</exception>
    public static DraftAction Parse([Required] ActionDescriptor descriptor)
    {
        SeatId actor = descriptor.Actor();
        CellId cell = descriptor.CellArgument(CellArgument);
        string piece = descriptor.Arguments.GetValueOrDefault(PieceArgument, string.Empty);
        return piece.Length > 0
            ? new DraftAction(actor, new PieceKindId(piece), cell)
            : throw new MalformedActionException($"The '{Kind}' action needs a kind for '{PieceArgument}'.");
    }
}
