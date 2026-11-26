using System.ComponentModel.DataAnnotations;
using Net.NowhereAtAll.Zaturnava.Engine.Boards;
using Net.NowhereAtAll.Zaturnava.Engine.Pieces;
using Net.NowhereAtAll.Zaturnava.Engine.Seats;

namespace Net.NowhereAtAll.Zaturnava.Engine.Actions;

/// <summary>
/// Buy off the <see cref="Restriction"/> of one's piece on <see cref="Cell"/>, at its lift's price (specification §7).
/// </summary>
public sealed record LiftAction(SeatId Actor, CellId Cell, LiftKind Restriction) : GameAction(Actor)
{
    public const string Kind = "lift";
    public const string RestrictionArgument = "restriction";
    /// <summary>"home", as an escape's: a cell chosen once the button is.</summary>
    public const string CellArgument = "home";
    public const string Movement = "movement";
    public const string Area = "area";

    public override ActionDescriptor Describe() =>
        new(
            Kind,
            this.ActorArguments()
                .Add(RestrictionArgument, ArgumentOf(this.Restriction))
                .Add(CellArgument, Text(this.Cell.Value))
        );

    /// <summary>How <paramref name="restriction"/> is written: "movement" or "area".</summary>
    public static string ArgumentOf(LiftKind restriction) => restriction == LiftKind.Movement ? Movement : Area;

    /// <exception cref="MalformedActionException">The restriction is neither "movement" nor "area".</exception>
    public static LiftAction Parse([Required] ActionDescriptor descriptor) =>
        new(
            descriptor.Actor(),
            descriptor.CellArgument(CellArgument),
            descriptor.Arguments.GetValueOrDefault(RestrictionArgument) switch
            {
                Movement => LiftKind.Movement,
                Area => LiftKind.Area,
                _ => throw new MalformedActionException(
                    $"The '{Kind}' action needs '{Movement}' or '{Area}' for '{RestrictionArgument}'."
                ),
            }
        );
}
