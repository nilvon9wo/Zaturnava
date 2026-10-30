using Net.NowhereAtAll.Zaturnava.Bots.Learning;
using Net.NowhereAtAll.Zaturnava.Engine.Rules;
using Net.NowhereAtAll.Zaturnava.Engine.Seats;

namespace Net.NowhereAtAll.Zaturnava.Bots.Test.Learning;

/// <summary>Networks taught, over and over, how each of a few positions will end for a seat: by no margin.</summary>
internal static class Taught
{
    private const int Hidden = 8;
    private const int Rounds = 1000;
    private const double Rate = 0.02;

    public static ValueNetworkData Network(
        IRulesApplying rules,
        params (GameState State, SeatId Seat, double Outcome)[] lessons
    )
    {
        PositionEncoder encoder = new(rules);
        ValueNetwork network = ValueNetwork.Untrained(encoder.Size, Hidden, new Random(7));
        for (int round = 0; round < Rounds; round++)
        {
            foreach ((GameState state, SeatId seat, double outcome) in lessons)
            {
                _ = network.Learn(encoder.Encode(state, seat), outcome, 0, Rate);
            }
        }

        return network.ToData(rules.Id);
    }
}
