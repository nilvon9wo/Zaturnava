using System.Collections.Immutable;
using System.Globalization;
using Net.NowhereAtAll.Zaturnava.Contracts;

namespace Net.NowhereAtAll.Zaturnava.Presentation.Test;

/// <summary>Actions as a server would send them: some drawn on the board, some not.</summary>
internal static class Actions
{
    public static readonly ActionDto Pass = Of("pass", (ActionArguments.Actor, "0"));

    public static ActionDto Move(int from, int to) =>
        Of(
            "move",
            (ActionArguments.Actor, "0"),
            (ActionArguments.From, from.ToString(CultureInfo.InvariantCulture)),
            (ActionArguments.To, to.ToString(CultureInfo.InvariantCulture))
        );

    public static ActionDto Of(string kind, params (string Name, string Value)[] arguments) =>
        new(kind, arguments.ToImmutableSortedDictionary(argument => argument.Name, argument => argument.Value));
}
