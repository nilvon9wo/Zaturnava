using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Text;
using Net.NowhereAtAll.Zaturnava.Engine.Boards;
using Net.NowhereAtAll.Zaturnava.Engine.Pieces;

namespace Net.NowhereAtAll.Zaturnava.Bots.FairyStockfish;

/// <summary>
/// How a piece moves, in the Betza notation Fairy-Stockfish defines pieces in, for a piece facing a given way on a
/// square grid. Fairy-Stockfish turns White's pieces right round for Black, so a Black piece's directions are
/// written turned round too (as <c>LiveBearingRun</c> found: Black's step right goes from d4 to c4). Each direction
/// a rule goes is written as its own atom (<c>frF</c>), unless the rule goes every way the atom does (<c>F</c>).
/// None, for a rule the notation, as written here, cannot say: a slide that must go some distance first; a hop that
/// may land beyond the next cell but not anywhere; a piece it may not hop over; an uneven leap only some ways round.
/// </summary>
public static class BetzaNotation
{
    /// <summary>The atom of each leap, by its longer and shorter distances.</summary>
    private static readonly ImmutableDictionary<(int Long, int Short), char> Atoms =
        new Dictionary<(int, int), char>
        {
            [(1, 0)] = 'W',
            [(1, 1)] = 'F',
            [(2, 0)] = 'D',
            [(2, 1)] = 'N',
            [(2, 2)] = 'A',
            [(3, 0)] = 'H',
            [(3, 1)] = 'C',
            [(3, 2)] = 'Z',
            [(3, 3)] = 'G',
        }.ToImmutableDictionary();

    /// <summary>
    /// The piece's moves, every rule of <paramref name="kind"/> written one after another; none, if any rule cannot
    /// be written.
    /// </summary>
    public static string? Of(
        [Required] PieceKind kind,
        [Required] ICellNavigating topology,
        Direction forward,
        bool isBlack
    )
    {
        ImmutableList<string?> rules = [.. kind.Movement.Select(rule => Of(rule, topology, forward, isBlack))];
        return rules.Any(rule => rule is null) ? null : string.Concat(rules);
    }

    /// <summary>One rule's moves; none, if it cannot be written.</summary>
    public static string? Of(
        [Required] MovementRule rule,
        [Required] ICellNavigating topology,
        Direction forward,
        bool isBlack
    )
    {
        ImmutableList<Direction> directions =
        [
            .. topology.Resolve(rule.Directions, forward)
                .Select(direction => isBlack ? new Direction(-direction.First, -direction.Second) : direction)
                .Distinct(),
        ];
        bool isSlide = rule is { IsLeaping: false, MaximumDistance: > 1 };
        bool isWritable = (isSlide ? rule.MinimumDistance == 1 : rule.MinimumDistance == rule.MaximumDistance)
            && rule.Shunned.IsEmpty
            && (!rule.IsHopping || rule.MaximumBeyond is 1 or int.MaxValue);
        ImmutableList<(char Atom, string Bearing)>? atoms = isWritable
            ? AtomsOf(directions, rule.IsLeaping ? rule.MinimumDistance : 1)
            : null;
        return atoms is null ? null : Written(rule, atoms, isSlide);
    }

    /// <summary>
    /// Each way's atom, <paramref name="distance"/> steps along it, with its bearing, unless the ways are all of the
    /// atom's; none, if any way has no atom, or no bearing that can be written.
    /// </summary>
    private static ImmutableList<(char Atom, string Bearing)>? AtomsOf(
        ImmutableList<Direction> directions,
        int distance
    )
    {
        ImmutableList<(Direction Way, char? Atom)> ways =
        [
            .. directions.Select(direction =>
            {
                Direction way = new(direction.First * distance, direction.Second * distance);
                return (way, AtomOf(way));
            }),
        ];
        if (ways.Any(way => way.Atom is null))
        {
            return null;
        }

        ImmutableList<(char Atom, string Bearing)> written = [];
        foreach (IGrouping<char, (Direction Way, char? Atom)> atom in ways.GroupBy(way => way.Atom!.Value))
        {
            ImmutableList<Direction> mine = [.. atom.Select(way => way.Way)];
            if (mine.Count == WaysOf(mine[0]))
            {
                written = written.Add((atom.Key, string.Empty));
                continue;
            }

            ImmutableList<string?> bearings = [.. mine.Select(BearingOf)];
            if (bearings.Any(bearing => bearing is null))
            {
                return null;
            }

            written = written.AddRange(bearings.Select(bearing => (atom.Key, bearing!)));
        }

        return written;
    }

    private static string Written(MovementRule rule, ImmutableList<(char Atom, string Bearing)> atoms, bool isSlide)
    {
        string prefix = (rule.CanMove, rule.CanCapture) switch
        {
            (true, false) => "m",
            (false, true) => "c",
            _ => string.Empty,
        };
        string kind = rule.IsHopping ? rule.MaximumBeyond == 1 ? "g" : "p" : rule.IsLame ? "n" : string.Empty;
        string range = isSlide && rule.MaximumDistance != int.MaxValue
            ? rule.MaximumDistance.ToString(CultureInfo.InvariantCulture)
            : string.Empty;
        bool isRider = isSlide || rule.IsHopping;
        StringBuilder text = new();
        foreach ((char atom, string bearing) in atoms)
        {
            _ = text.Append(prefix).Append(kind).Append(bearing).Append(isRider ? RiderOf(atom) : $"{atom}");
            _ = text.Append(isRider ? range : string.Empty);
        }

        return text.ToString();
    }

    /// <summary>The rider of an atom: the Rook of the Wazir, the Bishop of the Ferz, else the atom twice.</summary>
    private static string RiderOf(char atom) =>
        atom switch
        {
            'W' => "R",
            'F' => "B",
            _ => $"{atom}{atom}",
        };

    /// <summary>The atom of a leap, or none.</summary>
    private static char? AtomOf(Direction way)
    {
        int first = Math.Abs(way.First);
        int second = Math.Abs(way.Second);
        return Atoms.TryGetValue((Math.Max(first, second), Math.Min(first, second)), out char atom) ? atom : null;
    }

    /// <summary>How many ways round the atom of <paramref name="way"/> goes: 4 straight or diagonal, else 8.</summary>
    private static int WaysOf(Direction way) =>
        way.First == 0 || way.Second == 0 || Math.Abs(way.First) == Math.Abs(way.Second) ? 4 : 8;

    /// <summary>The bearing of one straight or diagonal way (f, b, r, l, and pairs of them); none for others.</summary>
    private static string? BearingOf(Direction way) =>
        (Math.Sign(way.First), Math.Sign(way.Second), WaysOf(way)) switch
        {
            (_, _, 8) => null,
            (0, 1, _) => "f",
            (0, -1, _) => "b",
            (1, 0, _) => "r",
            (-1, 0, _) => "l",
            (1, 1, _) => "fr",
            (-1, 1, _) => "fl",
            (1, -1, _) => "br",
            _ => "bl",
        };
}
