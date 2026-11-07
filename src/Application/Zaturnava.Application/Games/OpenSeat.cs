namespace Net.NowhereAtAll.Zaturnava.Application.Games;

/// <summary>
/// A seat waiting for a person to claim it; only a player signed in, if <see cref="IsForPlayers"/>: whoever set the
/// game up barred guests from it.
/// </summary>
public sealed record OpenSeat : SeatController
{
    public const string KindName = "open";

    /// <summary>What marks, in <see cref="Detail"/>, a seat only a player signed in may claim.</summary>
    public const string ForPlayers = "players";

    public bool IsForPlayers { get; init; }

    public override string Kind => KindName;

    public override string? Detail => this.IsForPlayers ? ForPlayers : null;

    public override bool IsOpen => true;

    /// <summary>The seat as <see cref="Detail"/> stores it.</summary>
    public static OpenSeat From(string? detail) => new() { IsForPlayers = detail == ForPlayers };
}
