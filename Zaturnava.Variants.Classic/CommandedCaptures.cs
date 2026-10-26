namespace Net.NowhereAtAll.Zaturnava.Variants.Classic;

/// <summary>
/// What a seat may take with an enemy army it commands (rules.md §6, <c>control.enemyArmyCaptures</c>):
/// through a throne its Raja holds, or moving for a seat whose throne its Raja holds.
/// </summary>
public enum CommandedCaptures
{
    /// <summary>Any piece: that army's own, its ally's, or the commanding team's.</summary>
    Anything,

    /// <summary>Any piece but the commanding team's. The default.</summary>
    NotOwnTeam,
}
