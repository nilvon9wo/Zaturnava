using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;

namespace Net.NowhereAtAll.Zaturnava.Engine.Actions;

/// <summary>Turns descriptors back into actions, by kind; a ruleset adds the kinds it introduces.</summary>
public sealed class ActionParser(ImmutableDictionary<string, Func<ActionDescriptor, GameAction>> parsers)
{
    private readonly ImmutableDictionary<string, Func<ActionDescriptor, GameAction>> _parsers = parsers;

    /// <summary>The kinds of action the kernel itself defines.</summary>
    public static ActionParser Standard { get; } = new(
        ImmutableDictionary<string, Func<ActionDescriptor, GameAction>>.Empty
            .Add(MoveAction.Kind, MoveAction.Parse)
            .Add(PassAction.Kind, PassAction.Parse)
            .Add(RollAction.Kind, RollAction.Parse)
            .Add(DiceAction.Kind, DiceAction.Parse)
            .Add(PromoteAction.Kind, PromoteAction.Parse)
            .Add(PlaceAction.Kind, PlaceAction.Parse)
            .Add(EscapeAction.Kind, EscapeAction.Parse)
            .Add(HandOverAction.Kind, HandOverAction.Parse)
            .Add(ProposeEndAction.Kind, ProposeEndAction.Parse)
            .Add(RejoinAction.Kind, RejoinAction.Parse)
            .Add(AnswerEndAction.Kind, AnswerEndAction.Parse)
            .Add(OfferAction.Kind, OfferAction.Parse)
            .Add(AcceptAction.Kind, AcceptAction.Parse)
            .Add(DeclineAction.Kind, DeclineAction.Parse)
    );

    /// <exception cref="MalformedActionException">The kind is unknown, or an argument is malformed.</exception>
    public GameAction Parse([Required] ActionDescriptor descriptor) =>
        this._parsers.TryGetValue(descriptor.Kind, out Func<ActionDescriptor, GameAction>? parse)
            ? parse(descriptor)
            : throw new MalformedActionException($"There is no '{descriptor.Kind}' action.");
}
