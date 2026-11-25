using System.Collections.Immutable;
using System.Security.Cryptography;
using System.Text;
using Net.NowhereAtAll.Zaturnava.Games.Chaturaji2027;

namespace Net.NowhereAtAll.Zaturnava.Games.GrandLegions.Test;

/// <summary>
/// The armies stored games were dealt, which they replay from: each quick game's, by a few seeds, with rivers and
/// without, kept as a digest of every piece placed, as they were dealt before the armies could hold the pieces a
/// palace holds. Should a change to the dealing alter any, a game in progress would replay otherwise. Changed in
/// place, for every game, 2026-11-20: the throne, as the program deals it, is never on the kingdom's front rank or
/// inner side; and the same day, the Shogi Knight added to the catalogue, which a fill on the medium and large boards
/// may now buy; and 2026-11-25, the leader promotion bonus taken off the Lance, the Shogi Pawn, the Shogi Knight and
/// the Man, so much cheaper. Games on the server keep what they were dealt, so replay as dealt whatever this gives.
/// </summary>
public sealed class QuickArmyReplayTest
{
    private static readonly ImmutableList<int> DealSeeds = [0, 1, 21, 2027, 123_456_789];
    private static readonly ImmutableList<int> FillSeeds = [0, 1, 21, 2027];
    private static readonly ImmutableList<Rivers> RiversDealt = [Rivers.None, Rivers.Both];
    private static readonly ImmutableList<bool> Alike = [false, true];
    private static readonly ImmutableList<int> Terrains = [0, 3];

    public static TheoryData<string, string> Deals =>
        new()
        {
            { "grand-legions-small", "00DCCEB06FBD4E9D" },
            { "grand-legions-medium", "8BE83C43E0004B3B" },
            { "grand-legions-large", "E476EE6C077C032C" },
        };

    [Theory]
    [MemberData(nameof(Deals))]
    public void Dealt_WithoutPalacePieces_DealsAsGamesStoredWereDealt(string id, string digest)
    {
        // Arrange
        QuickGame game = QuickGame.All.Single(quick => quick.Id == id);

        // Act
        string dealt = string.Join(
            "|",
            from seed in DealSeeds
            from rivers in RiversDealt
            from areAlike in Alike
            select string.Join(";", QuickArmy.Dealt(game with { Rivers = rivers }, seed, areAlike))
        );

        // Assert
        Assert.Equal(digest, Digest(dealt));
    }

    [Theory]
    [MemberData(nameof(Fills))]
    public void Filled_WithoutPalacePieces_FillsAsGamesStoredWereFilled(string id, string digest)
    {
        // Arrange - from nothing; from a throne on b1; and from a King on a1, its throne, a third of the budget left
        QuickGame game = QuickGame.All.Single(quick => quick.Id == id);
        ImmutableList<ArmyPiece> king = [new(GrandLegionsPieces.WesternKing, 0, 0)];

        // Act
        string filled = string.Join(
            "|",
            from seed in FillSeeds
            from terrain in Terrains
            from fill in FillsOf(game, terrain, seed, king)
            let army = QuickDraft.Filled(game, fill)
            select $"{army.Throne}:{string.Join(";", army.Added)}"
        );

        // Assert
        Assert.Equal(digest, Digest(filled));
    }

    public static TheoryData<string, string> Fills =>
        new()
        {
            { "grand-legions-small", "5EDA3580F2EE23D7" },
            { "grand-legions-medium", "1C19DD6F0928C956" },
            { "grand-legions-large", "94C575914396E584" },
        };

    private static ImmutableList<ArmyFill> FillsOf(
        QuickGame game,
        int terrain,
        int seed,
        ImmutableList<ArmyPiece> king
    ) =>
    [
        new(terrain, game.Budget, [], null, new(seed)),
        new(terrain, game.Budget, [], new ArmySquare(1, 0), new(seed)),
        new(terrain, game.Budget / 3, king, new ArmySquare(0, 0), new(seed)),
    ];

    private static string Digest(string text) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text)))[..16];
}
