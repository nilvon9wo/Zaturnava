using System.ComponentModel.DataAnnotations;

namespace Net.NowhereAtAll.Zaturnava.Games.Chaturaji2027;

/// <summary>
/// The limits of the safety nets that end a game on points (rules.md §10), the server's own, from its settings
/// file (Stated 2026-11-06): how many rounds no one may move; how many may pass with no piece taken; and how many
/// may every turn end where the game has been before.
/// </summary>
public sealed record SafetyNets(
    [Range(1, int.MaxValue)] int IdleRounds,
    [Range(1, int.MaxValue)] int QuietRounds,
    [Range(1, int.MaxValue)] int RepeatRounds
)
{
    public static SafetyNets Default { get; } = new(
        Chaturaji2027Rules.DefaultIdleRounds,
        Chaturaji2027Rules.DefaultQuietRounds,
        Chaturaji2027Rules.DefaultRepeatRounds
    );
}
