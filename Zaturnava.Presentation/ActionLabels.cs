using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using Net.NowhereAtAll.Zaturnava.Contracts;

namespace Net.NowhereAtAll.Zaturnava.Presentation;

/// <summary>
/// What a button for an action off the board says: the server's label, if it gave one; else a phrase
/// for the kinds known; else the kind.
/// </summary>
public static class ActionLabels
{
    private static readonly ImmutableDictionary<string, string> Phrases =
        ImmutableDictionary<string, string>.Empty
            .Add("roll", "Roll the dice")
            .Add("pass", "Pass")
            .Add("accept", "Accept the offer")
            .Add("decline", "Decline the offer");

    public static string LabelOf([Required] ActionDto action) =>
        action.Label ?? Phrases.GetValueOrDefault(action.Kind, action.Kind);
}
