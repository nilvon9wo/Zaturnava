using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Text.Json;

namespace Net.NowhereAtAll.Zaturnava.Games.Chaturaji2027;

/// <summary>
/// What the Chaturaji 2027 bonuses score (rules.md §5, §8), and how far ahead a team must be, on points and on the
/// board both, to win by giving up its turns (§10). Read from a table of the game's own, not written into the
/// program: <c>Scoring/{game}.json</c>, kept in the assembly that plays the game, which can be changed alone.
/// </summary>
/// <param name="ThroneEntryBonus">The points for a leader entering an enemy's throne.</param>
/// <param name="RajaOnThroneBonus">The points, on top of the capture, for taking a leader on its own throne.</param>
/// <param name="EmpireBonus">The points for creating the empire.</param>
/// <param name="EmpireByCaptureBonus">The points, on top of <paramref name="EmpireBonus"/>, for its captor.</param>
/// <param name="EmpireByThroneCaptureBonus">
/// The points instead of <paramref name="EmpireByCaptureBonus"/> when the leader taken was on its throne.
/// </param>
/// <param name="EveryPieceBonus">The points for commanding every piece on the board, on one's own turn.</param>
/// <param name="RansomAcceptedBonus">The points for accepting a ransom, for each prisoner it sends home.</param>
/// <param name="RansomRajaBonus">The points instead of <paramref name="RansomAcceptedBonus"/> for a leader.</param>
/// <param name="ClearLead">How far ahead a team must be to win by giving up its turns.</param>
public sealed record Scoring(
    int ThroneEntryBonus,
    int RajaOnThroneBonus,
    int EmpireBonus,
    int EmpireByCaptureBonus,
    int EmpireByThroneCaptureBonus,
    int EveryPieceBonus,
    int RansomAcceptedBonus,
    int RansomRajaBonus,
    int ClearLead
)
{
    private const string Folder = "Scoring/";

    private static readonly JsonSerializerOptions Strict =
        new(JsonSerializerOptions.Web) { RespectRequiredConstructorParameters = true };

    /// <summary>Chaturaji 2027's own.</summary>
    public static Scoring Standard { get; } = Of(typeof(Scoring).Assembly, Chaturaji2027Rules.RulesetId);

    /// <summary>The scoring of <paramref name="game"/>, as its table in <paramref name="assembly"/> has it.</summary>
    /// <exception cref="ArgumentException">The assembly keeps no table for the game.</exception>
    public static Scoring Of([Required] Assembly assembly, [Required] string game)
    {
        using Stream stream = assembly.GetManifestResourceStream($"{Folder}{game}.json")
            ?? throw new ArgumentException($"No scoring table for {game}.", nameof(game));
        return JsonSerializer.Deserialize<Scoring>(stream, Strict)!; // ours, so whole
    }
}
