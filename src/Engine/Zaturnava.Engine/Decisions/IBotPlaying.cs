namespace Net.NowhereAtAll.Zaturnava.Engine.Decisions;

/// <summary>A player that needs no person, and says what it is.</summary>
public interface IBotPlaying : IDeciding
{
    BotProfile Profile { get; }
}
