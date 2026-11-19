using System.ComponentModel.DataAnnotations;
using Net.NowhereAtAll.Zaturnava.Engine.Boards;
using Net.NowhereAtAll.Zaturnava.Engine.Seats;

namespace Net.NowhereAtAll.Zaturnava.Engine.Actions;

/// <summary>
/// Move the piece on <see cref="From"/> to <see cref="To"/>, capturing whatever is there; or, where the rules let the
/// move do something else instead, as swapping places with an ally does, that, unless it <see cref="Captures"/>.
/// </summary>
public sealed record MoveAction(SeatId Actor, CellId From, CellId To) : GameAction(Actor)
{
    public const string Kind = "move";
    public const string FromArgument = "from";
    public const string ToArgument = "to";

    /// <summary>The argument saying the move captures, set only where the move might do something else.</summary>
    public const string CapturesArgument = "captures";

    /// <summary>
    /// The move captures what stands where it goes, though the rules would have it swap places with it, say (the
    /// user's request of 2026-11-19: "Swap or capture?"). Said only where it matters, so that every move described
    /// before still means what it did.
    /// </summary>
    public bool Captures { get; init; }

    public override ActionDescriptor Describe()
    {
        string from = Text(this.From.Value);
        string to = Text(this.To.Value);
        ActionDescriptor described =
            new(Kind, this.ActorArguments().Add(FromArgument, from).Add(ToArgument, to));
        return this.Captures
            ? described with { Arguments = described.Arguments.Add(CapturesArgument, "true") }
            : described;
    }

    public static MoveAction Parse([Required] ActionDescriptor descriptor)
    {
        SeatId actor = descriptor.Actor();
        CellId from = descriptor.CellArgument(FromArgument);
        CellId to = descriptor.CellArgument(ToArgument);
        return new MoveAction(actor, from, to)
        {
            Captures = descriptor.Arguments.TryGetValue(CapturesArgument, out string? captures) && captures == "true",
        };
    }
}
