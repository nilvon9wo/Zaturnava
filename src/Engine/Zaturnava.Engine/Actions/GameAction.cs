using System.Collections.Immutable;
using System.Globalization;
using Net.NowhereAtAll.Zaturnava.Engine.Seats;

namespace Net.NowhereAtAll.Zaturnava.Engine.Actions;

/// <summary>A decision a seat makes. Each kind of action describes itself as plain data.</summary>
public abstract record GameAction(SeatId Actor)
{
    public abstract ActionDescriptor Describe();

    protected ImmutableSortedDictionary<string, string> ActorArguments() =>
        ImmutableSortedDictionary<string, string>.Empty.Add(ActionDescriptor.ActorArgument, Text(this.Actor.Value));

    protected static string Text(int value) => value.ToString(CultureInfo.InvariantCulture);
}
