using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using Net.NowhereAtAll.Zaturnava.Engine.Boards;
using Net.NowhereAtAll.Zaturnava.Engine.Seats;

namespace Net.NowhereAtAll.Zaturnava.Engine.Actions;

/// <summary>
/// An action as plain data - a kind and named arguments - so that it can be stored, sent to any
/// client in any language, and echoed back, without anyone outside the ruleset knowing its type.
/// </summary>
public sealed record ActionDescriptor(string Kind, ImmutableSortedDictionary<string, string> Arguments)
{
    public const string ActorArgument = "actor";

    /// <summary>What separates the items of an argument that lists integers: "3,5".</summary>
    public const char ListSeparator = ',';

    public SeatId Actor() => new(this.IntegerArgument(ActorArgument));

    public CellId CellArgument([Required] string name) => new(this.IntegerArgument(name));

    /// <exception cref="MalformedActionException">The argument is missing or not an integer.</exception>
    public int IntegerArgument([Required] string name)
    {
        string text = this.Arguments.GetValueOrDefault(name, string.Empty);
        bool isInteger = int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out int value);
        return isInteger
            ? value
            : throw new MalformedActionException($"The '{this.Kind}' action needs an integer '{name}'.");
    }

    /// <summary>An argument listing integers, such as "3,5"; empty if the argument is empty or missing.</summary>
    /// <exception cref="MalformedActionException">An item is not an integer.</exception>
    public ImmutableList<int> IntegersArgument([Required] string name)
    {
        string text = this.Arguments.GetValueOrDefault(name, string.Empty);
        string[] items = text.Length == 0
            ? []
            : text.Split(ListSeparator);
        ImmutableList<int> values = [.. items.Select(Integer).OfType<int>()];
        return values.Count == items.Length
            ? values
            : throw new MalformedActionException($"The '{this.Kind}' action needs integers in '{name}', such as 3,5.");
    }

    private static int? Integer(string text) =>
        int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out int value)
            ? value
            : null;
}
