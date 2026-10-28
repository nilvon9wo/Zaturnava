using System.Collections.Immutable;
using Net.NowhereAtAll.Zaturnava.Engine.Seats;

namespace Net.NowhereAtAll.Zaturnava.Engine.Pieces;

/// <summary>
/// <see cref="From"/> offers <see cref="To"/> back the prisoners <see cref="Given"/>, for the
/// prisoners <see cref="Asked"/>, which <see cref="To"/> holds, in return.
/// </summary>
public sealed record RansomOffer(SeatId From, SeatId To, ImmutableList<PieceId> Given, ImmutableList<PieceId> Asked)
{
    public bool Equals(RansomOffer? other) =>
        other is not null
        && (this.From, this.To) == (other.From, other.To)
        && this.Given.SequenceEqual(other.Given)
        && this.Asked.SequenceEqual(other.Asked);

    public override int GetHashCode() => HashCode.Combine(this.From, this.To, this.Given.Count, this.Asked.Count);
}
