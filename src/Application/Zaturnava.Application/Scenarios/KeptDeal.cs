using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using Net.NowhereAtAll.Zaturnava.Engine.Boards;
using Net.NowhereAtAll.Zaturnava.Engine.Pieces;
using Net.NowhereAtAll.Zaturnava.Engine.Positions;
using Net.NowhereAtAll.Zaturnava.Engine.Rules;
using Net.NowhereAtAll.Zaturnava.Engine.Seats;

namespace Net.NowhereAtAll.Zaturnava.Application.Scenarios;

/// <summary>
/// What a game's deal dealt, kept in its first action (<see cref="ScenarioStart.Kept"/>): every piece where it stood,
/// each army's throne and palace, and the terrain. A game replays from these, not from its seed, so that a change to
/// how games are dealt changes only games dealt after it (the user's word of 2026-11-20: whether the program or a
/// person set a throne somewhere should never matter to loading a game).
/// </summary>
public sealed record KeptDeal(
    ImmutableList<KeptPiece> Pieces,
    ImmutableSortedDictionary<int, int>? Thrones,
    ImmutableSortedDictionary<int, ImmutableList<int>>? Palaces,
    int Terrain
)
{
    /// <summary>What <paramref name="dealt"/> was dealt.</summary>
    public static KeptDeal Of([Required] GameState dealt) =>
        new(
            [
                .. dealt.Position.Occupants
                    .OrderBy(occupant => occupant.Key.Value)
                    .Select(occupant => new KeptPiece(
                        occupant.Key.Value,
                        occupant.Value.Id.Value,
                        occupant.Value.Kind.Value,
                        occupant.Value.Owner.Value
                    )),
            ],
            dealt.Thrones?.ToImmutableSortedDictionary(throne => throne.Key.Value, throne => throne.Value.Value),
            dealt.Palaces?.ToImmutableSortedDictionary(
                palace => palace.Key.Value,
                palace => palace.Value.Select(cell => cell.Value).ToImmutableList()
            ),
            dealt.Terrain
        );

    /// <summary><paramref name="dealt"/> as this deal dealt it, whatever the seed would deal now.</summary>
    public GameState Applied([Required] GameState dealt)
    {
        Position position = this.Pieces.Aggregate(
            Position.Empty,
            (board, piece) => board.Place(
                new CellId(piece.Cell),
                new Piece(new PieceId(piece.Id), new PieceKindId(piece.Kind), new SeatId(piece.Owner))
            )
        );
        return dealt with
        {
            Position = position,
            Start = position,
            Thrones = this.Thrones?.ToImmutableDictionary(
                throne => new SeatId(throne.Key),
                throne => new CellId(throne.Value)
            ),
            Palaces = this.Palaces?.ToImmutableDictionary(
                palace => new SeatId(palace.Key),
                palace => palace.Value.Select(cell => new CellId(cell)).ToImmutableList()
            ),
            Terrain = this.Terrain,
        };
    }

    public string Serialized() => JsonSerializer.Serialize(this, JsonSerializerOptions.Web);

    public static KeptDeal Parsed([Required] string text) =>
        JsonSerializer.Deserialize<KeptDeal>(text, JsonSerializerOptions.Web)!;
}

/// <summary>A piece as a deal dealt it: its cell, its id, its kind, and its seat.</summary>
public sealed record KeptPiece(int Cell, int Id, string Kind, int Owner);
