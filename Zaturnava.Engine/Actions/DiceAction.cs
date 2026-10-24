using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using Net.NowhereAtAll.Zaturnava.Engine.Seats;

namespace Net.NowhereAtAll.Zaturnava.Engine.Actions;

/// <summary>
/// How the dice <see cref="GameAction.Actor"/> rolled came up. Chance's action, not a seat's: the
/// host applies it when the game waits on dice, so that the game's actions record every roll and
/// replay exactly. A seat submitting one is refused.
/// </summary>
public sealed record DiceAction(SeatId Actor, ImmutableList<int> Values) : GameAction(Actor)
{
    public const string Kind = "dice";
    public const string ValuesArgument = "values";

    private const char Separator = ',';

    public override ActionDescriptor Describe()
    {
        string values = string.Join(Separator, this.Values.Select(Text));
        return new ActionDescriptor(Kind, this.ActorArguments().Add(ValuesArgument, values));
    }

    /// <exception cref="MalformedActionException">The values are missing or not integers.</exception>
    public static DiceAction Parse([Required] ActionDescriptor descriptor)
    {
        SeatId actor = descriptor.Actor();
        string[] texts = descriptor.Arguments.GetValueOrDefault(ValuesArgument, string.Empty).Split(Separator);
        ImmutableList<int> values = [.. texts.Select(Value).OfType<int>()];
        return values.Count == texts.Length
            ? new DiceAction(actor, values)
            : throw new MalformedActionException($"The '{Kind}' action needs integer '{ValuesArgument}', such as 3,5.");
    }

    public bool Equals(DiceAction? other) =>
        other is not null && this.Actor == other.Actor && this.Values.SequenceEqual(other.Values);

    public override int GetHashCode() => HashCode.Combine(this.Actor, this.Values.Count);

    private static int? Value(string text) =>
        int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out int value)
            ? value
            : null;
}
