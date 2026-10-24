using System.ComponentModel.DataAnnotations;
using Net.NowhereAtAll.Zaturnava.Application.Bots;
using Net.NowhereAtAll.Zaturnava.Engine.Decisions;

namespace Net.NowhereAtAll.Zaturnava.Application.Games;

/// <summary>
/// Who decides for a seat: nobody yet, a person holding the seat's token, or a bot the server runs.
/// <see cref="Kind"/> and <see cref="Detail"/> are how a controller is stored and shown.
/// </summary>
public abstract record SeatController
{
    public abstract string Kind { get; }

    public abstract string? Detail { get; }

    public virtual bool IsOpen => false;

    public virtual bool IsHeldBy([Required] string token) => false;

    /// <summary>What the server should ask to decide for this seat, if anything.</summary>
    public virtual IDeciding? DeciderFrom([Required] IBotLocating bots) => null;

    /// <exception cref="ArgumentException">There is no such kind of controller.</exception>
    public static SeatController From([Required] string kind, string? detail) =>
        kind switch
        {
            OpenSeat.KindName => new OpenSeat(),
            HumanSeat.KindName => new HumanSeat(detail!),
            BotSeat.KindName => new BotSeat(detail!),
            _ => throw new ArgumentException($"There is no '{kind}' seat controller.", nameof(kind)),
        };
}
