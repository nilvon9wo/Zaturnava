using Net.NowhereAtAll.Zaturnava.Engine.Rules;
using Net.NowhereAtAll.Zaturnava.Engine.Seats;

namespace Net.NowhereAtAll.Zaturnava.Engine.Test.Rules;

public sealed class AbsenceTest
{
    private static readonly SeatId Black = new(0);
    private static readonly SeatId Yellow = new(1);
    private static readonly SeatId Green = new(2);

    /// <summary>Black missed two turns and is back; Yellow would stop waiting for Black and for Green.</summary>
    private static readonly Absence Away = new(
        [new KeyValuePair<SeatId, int>(Black, 2)],
        [new AwayVote(Yellow, Black), new AwayVote(Yellow, Green)],
        [Black]
    );

    [Fact]
    public void MissedBy_WhenASeatMissedTurnsOrNone_SaysHowMany()
    {
        // Arrange - nothing beyond who is away

        // Act
        (int Black, int Yellow) missed = (Away.MissedBy(Black), Away.MissedBy(Yellow));

        // Assert
        Assert.Equal((2, 0), missed);
    }

    [Fact]
    public void Away_WhenSeatsMissedTurns_NamesThoseNotBackInSeatOrder()
    {
        // Arrange - Black is back; Green and Yellow missed turns, and a seat missed none
        Absence absence = Away with
        {
            Missed = Away.Missed.Add(Green, 1).Add(Yellow, 3).Add(new SeatId(3), 0),
        };

        // Act
        IEnumerable<SeatId> away = absence.Away;

        // Assert
        Assert.Equal([Yellow, Green], away);
    }

    [Fact]
    public void StoppingFor_WhenSeatsWouldStopWaiting_NamesThoseForThatSeatOnly()
    {
        // Arrange - nothing beyond who is away

        // Act
        IEnumerable<SeatId> stopping = Away.StoppingFor(Black);

        // Assert
        Assert.Equal([Yellow], stopping);
    }

    [Fact]
    public void Forgetting_WhenASeatPlaysAgain_ForgetsItsAbsenceButNoOnesElse()
    {
        // Arrange - nothing beyond who is away

        // Act
        Absence forgotten = Away.Forgetting(Black);

        // Assert
        Assert.Equal(
            (0, false, 1),
            (forgotten.MissedBy(Black), forgotten.Returning.Contains(Black), forgotten.StopVotes.Count)
        );
        Assert.Equal([Yellow], forgotten.StoppingFor(Green));
    }
}
