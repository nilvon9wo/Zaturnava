namespace Net.NowhereAtAll.Zaturnava.Engine.Events;

/// <summary>Why one seat commands another's army.</summary>
public enum CommandBasis
{
    /// <summary>It plays the rest of the army's turn, as the army's ally let it: complete control.</summary>
    Regency,

    /// <summary>
    /// Its leader sits on an ally's throne: it moves that army on its own turns, and may be regent.
    /// </summary>
    AllyThrone,

    /// <summary>
    /// Its leader sits on an enemy's throne: it moves that army on its own turns, and shares the enemy's.
    /// </summary>
    EnemyThrone,

    /// <summary>Its ally has no leader: it moves that army on its own turns, and may be regent.</summary>
    LeaderlessAlly,
}
