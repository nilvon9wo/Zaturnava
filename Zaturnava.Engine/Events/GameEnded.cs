using Net.NowhereAtAll.Zaturnava.Engine.Rules;

namespace Net.NowhereAtAll.Zaturnava.Engine.Events;

public sealed record GameEnded(GameOutcome Outcome) : GameEvent;
