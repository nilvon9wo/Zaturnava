using Net.NowhereAtAll.Zaturnava.Engine.Seats;

namespace Net.NowhereAtAll.Zaturnava.Application.Games;

/// <summary>
/// A game waiting for people to take its open seats, and who may fill them with bots instead, and when: the first to
/// arrive of the people seated (<see cref="FirstArrival"/>; none, if no one is seated), <see cref="UntilFirst"/> from
/// now, once the game has waited <see cref="GameServiceSettings.FillWait"/> since anything last changed; and, in case
/// they have gone, anyone seated, <see cref="UntilAnyone"/> from now, once it has waited twice as long.
/// </summary>
public sealed record Vacancy(SeatId? FirstArrival, TimeSpan UntilFirst, TimeSpan UntilAnyone)
{
    /// <summary>Whether <paramref name="seat"/>, held by a person, may fill the open seats now.</summary>
    public bool MayFill(SeatId seat) =>
        this.UntilAnyone == TimeSpan.Zero || (seat == this.FirstArrival && this.UntilFirst == TimeSpan.Zero);
}
