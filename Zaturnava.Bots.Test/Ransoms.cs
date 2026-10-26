using System.Collections.Immutable;
using Net.NowhereAtAll.Zaturnava.Engine.Actions;
using Net.NowhereAtAll.Zaturnava.Engine.Boards;
using Net.NowhereAtAll.Zaturnava.Engine.Decisions;
using Net.NowhereAtAll.Zaturnava.Engine.Pieces;
using Net.NowhereAtAll.Zaturnava.Engine.Positions;
using Net.NowhereAtAll.Zaturnava.Engine.Rules;
using Net.NowhereAtAll.Zaturnava.Engine.Seats;

namespace Net.NowhereAtAll.Zaturnava.Bots.Test;

/// <summary>
/// Prisoners to trade, with seats 0 and 2 allied against 1 and 3: seat 0 holds Yellow's (seat 1's)
/// Pawn 10 and Horse 11, and Green's (its ally's) Raja 30; Yellow holds seat 0's Horse 20 and Green's
/// Pawn 21.
/// </summary>
internal static class Ransoms
{
    public static readonly SeatId Black = ScoringRules.Seat;
    public static readonly SeatId Yellow = new(1);
    public static readonly SeatId Green = new(2);

    public static readonly PassAction Done = new(Black);

    /// <summary>Once seat 0's turn is over, when it may offer, without the prisoners <paramref name="gone"/>.</summary>
    public static DecisionRequest WindowWithout(params int[] gone)
    {
        DecisionRequest request = Window();
        GameState state = request.State;
        return request with
        {
            State = state with { Prisoners = state.Prisoners.RemoveAll(held => gone.Contains(held.Piece.Id.Value)) },
        };
    }

    /// <summary>Once seat 0's turn is over, when it may offer, having made <paramref name="made"/>.</summary>
    public static DecisionRequest Window(params RansomOffer[] made)
    {
        DecisionRequest request = Request();
        return request with { State = request.State with { IsRansomWindow = true, OffersMade = [.. made] } };
    }

    /// <summary>
    /// Seat 0 answering Yellow's offer of its <paramref name="given"/> for its <paramref name="asked"/>.
    /// </summary>
    public static DecisionRequest Offered(ImmutableList<int> given, ImmutableList<int> asked)
    {
        DecisionRequest request = Request();
        RansomOffer offer = new(Yellow, Black, [.. given.Select(Id)], [.. asked.Select(Id)]);
        return request with { State = request.State with { Offer = offer } };
    }

    /// <summary>
    /// Seat 0's turn, with its Pawn 40 on cell 7, which it may promote into Yellow's Horse 11 or the
    /// piece <paramref name="teams"/> (Green's Raja 30, or its own Horse 20), or not at all (a pass).
    /// </summary>
    public static DecisionRequest Promoting(int teams)
    {
        PassAction pass = new(Black);
        PromoteAction enemys = new(Black, new CellId(7), Id(11));
        PromoteAction promotion = new(Black, new CellId(7), Id(teams));
        ScoringRules rules =
            new(ImmutableDictionary<GameAction, int>.Empty.Add(pass, 0).Add(enemys, 0).Add(promotion, 0));
        GameState state = Request().State with
        {
            Position = Position.Empty.Place(new CellId(7), new Piece(Id(40), new PieceKindId("pawn"), Black)),
        };
        return new DecisionRequest(rules, state, Black, rules.LegalActions(state));
    }

    /// <summary>The request, with each piece on its cell (cells from 0), worth what its kind is.</summary>
    public static DecisionRequest On(DecisionRequest request, params (string Kind, SeatId Owner)[] pieces)
    {
        Position position = pieces
            .Select((piece, index) => (piece, index))
            .Aggregate(
                Position.Empty,
                (placed, next) => placed.Place(
                    new CellId(next.index),
                    new Piece(Id(100 + next.index), new PieceKindId(next.piece.Kind), next.piece.Owner)
                )
            );
        return request with { State = request.State with { Position = position } };
    }

    /// <summary>The request, with seat 0 also holding Yellow's Raja 12.</summary>
    public static DecisionRequest HoldingYellowsRaja(DecisionRequest request) =>
        request with
        {
            State = request.State with { Prisoners = request.State.Prisoners.Add(Held(12, "raja", Yellow, Black)) },
        };

    public static RansomOffer Offer(SeatId to, ImmutableList<int> given, ImmutableList<int> asked) =>
        new(Black, to, [.. given.Select(Id)], [.. asked.Select(Id)]);

    private static DecisionRequest Request()
    {
        ScoringRules rules = new(ImmutableDictionary<GameAction, int>.Empty.Add(Done, 0));
        GameState state = ScoringRules.State() with
        {
            Teams = TeamGraph.FromTeams([[Black, Green], [Yellow, new SeatId(3)]]),
            Prisoners =
            [
                Held(10, "pawn", Yellow, Black),
                Held(11, "horse", Yellow, Black),
                Held(30, "raja", Green, Black),
                Held(20, "horse", Black, Yellow),
                Held(21, "pawn", Green, Yellow),
            ],
        };
        return new DecisionRequest(rules, state, Black, rules.LegalActions(state));
    }

    private static Prisoner Held(int id, string kind, SeatId owner, SeatId holder) =>
        new(new Piece(Id(id), new PieceKindId(kind), owner), holder);

    private static PieceId Id(int id) => new(id);
}
