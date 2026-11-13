using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using Net.NowhereAtAll.Zaturnava.Engine.Boards;

namespace Net.NowhereAtAll.Zaturnava.Games.GrandLegions;

/// <summary>
/// A quick game's terrain (Stated 2026-11-06): its rivers, decided at random, each layout as likely as the others -
/// none, one north-south, one east-west, or both - and, either equally likely, through the kingdoms or around them.
/// Numbered as a game's terrain is kept: the rivers' flags, and <see cref="Around"/> more around the kingdoms; 0, no
/// rivers, as every game dealt before terrain was has.
/// </summary>
public static class QuickTerrain
{
    private const int Around = 4;
    private const int Layouts = 8;

    /// <summary>What the terrain's draw is seeded by beside the game's seed, apart from the armies' own draw.</summary>
    private const int Salt = 0x5EED;

    /// <summary>The terrain a game dealt by <paramref name="seed"/> is played on.</summary>
    public static int Dealt(int seed) => new Random(seed ^ Salt).Next(Layouts);

    public static Rivers RiversOf(int terrain) => (Rivers)(terrain & (int)Rivers.Both);

    public static RiverCourse CourseOf(int terrain) =>
        (terrain & Around) == 0 ? RiverCourse.ThroughKingdoms : RiverCourse.AroundKingdoms;

    /// <summary>The board of <paramref name="game"/> on <paramref name="terrain"/>.</summary>
    public static GrandBoard BoardOf([Required] QuickGame game, int terrain) =>
        new(
            game.Width,
            game.Height,
            game.KingdomWidth,
            game.KingdomDepth,
            Rivers: RiversOf(terrain),
            Course: CourseOf(terrain)
        );

    /// <summary>The terrain, for people: "A river runs north to south, around the kingdoms."</summary>
    public static string Described(int terrain) =>
        RiversOf(terrain) switch
        {
            Rivers.None => "No rivers.",
            Rivers.NorthSouth => $"A river runs north to south, {CourseNamed(terrain)}.",
            Rivers.EastWest => $"A river runs east to west, {CourseNamed(terrain)}.",
            _ => $"Rivers run north to south and east to west, {CourseNamed(terrain)}.",
        };

    /// <summary>The rivers of <paramref name="game"/> on <paramref name="terrain"/>, as lines to draw.</summary>
    public static ImmutableList<BoardLine> LinesOf([Required] QuickGame game, int terrain) =>
        BoardOf(game, terrain).RiverLines();

    private static string CourseNamed(int terrain) =>
        CourseOf(terrain) == RiverCourse.ThroughKingdoms ? "through the kingdoms" : "around the kingdoms";
}
