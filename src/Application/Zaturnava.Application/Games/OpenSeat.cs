namespace Net.NowhereAtAll.Zaturnava.Application.Games;

/// <summary>A seat waiting for a person to claim it.</summary>
public sealed record OpenSeat : SeatController
{
    public const string KindName = "open";

    public override string Kind => KindName;

    public override string? Detail => null;

    public override bool IsOpen => true;
}
