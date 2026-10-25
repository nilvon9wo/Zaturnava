using Net.NowhereAtAll.Zaturnava.Engine.Actions;

namespace Net.NowhereAtAll.Zaturnava.Engine.Rules;

/// <summary>
/// A decision the deciding seat has only so long to make, by name - whoever hosts the game chooses
/// how long - and what is decided for it once that time is up.
/// </summary>
public sealed record DecisionTimer(string Name, GameAction Default);
