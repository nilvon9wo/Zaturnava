using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using Net.NowhereAtAll.Zaturnava.Contracts;

namespace Net.NowhereAtAll.Zaturnava.Presentation;

/// <summary>What a button for an action off the board says: a phrase for the kinds known, else the kind.</summary>
public static class ActionLabels
{
    private static readonly ImmutableDictionary<string, string> Phrases =
        ImmutableDictionary<string, string>.Empty
            .Add("roll", "Roll the dice")
            .Add("pass", "Pass");

    public static string LabelOf([Required] ActionDto action) => Phrases.GetValueOrDefault(action.Kind, action.Kind);
}
