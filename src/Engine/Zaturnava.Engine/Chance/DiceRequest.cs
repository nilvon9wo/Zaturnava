namespace Net.NowhereAtAll.Zaturnava.Engine.Chance;

/// <summary>
/// Dice a game is waiting on: <see cref="Count"/> dice of <see cref="Sides"/> sides each. Whoever
/// hosts the game rolls them, through an <see cref="IDiceRolling"/>; no seat ever chooses them.
/// </summary>
public sealed record DiceRequest(int Count, int Sides);
