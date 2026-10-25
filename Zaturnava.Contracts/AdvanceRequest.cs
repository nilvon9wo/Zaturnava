namespace Net.NowhereAtAll.Zaturnava.Contracts;

/// <summary>Let the bot whose turn it is make one decision, if the game is still at <see cref="Version"/>.</summary>
public sealed record AdvanceRequest(int Version);
