namespace Net.NowhereAtAll.Zaturnava.Application.Games;

/// <summary>
/// A seat held back from people while the game's creator drafts one army for every seat (Stated, 2026-11-12): no one
/// may claim it, and the lobby lists the game as waiting for no one, until the draft is over, when it opens
/// (<see cref="Opened"/>); only a player signed in may then claim it, if <see cref="SeatController.IsForPlayers"/>.
/// </summary>
public sealed record HeldSeat : SeatController
{
    public const string KindName = "held";

    public override string Kind => KindName;

    public override string? Detail => this.IsForPlayers ? OpenSeat.ForPlayers : null;

    /// <summary>Shown as the open seat it will be.</summary>
    public override string Player => OpenSeat.KindName;

    /// <summary>The seat as <see cref="Detail"/> stores it.</summary>
    public static HeldSeat From(string? detail) => new() { IsForPlayers = detail == OpenSeat.ForPlayers };

    /// <summary>The seat opened to people, once the draft is over.</summary>
    public OpenSeat Opened() => new() { IsForPlayers = this.IsForPlayers };
}
