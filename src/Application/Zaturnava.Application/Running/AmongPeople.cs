using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using Net.NowhereAtAll.Zaturnava.Engine.Actions;
using Net.NowhereAtAll.Zaturnava.Engine.Decisions;
using Net.NowhereAtAll.Zaturnava.Engine.Seats;

namespace Net.NowhereAtAll.Zaturnava.Application.Running;

/// <summary>A bot told which seats people play, as it decides, so that it may defer to them.</summary>
public sealed class AmongPeople([Required] IDeciding bot, [Required] ImmutableHashSet<SeatId> people) : IDeciding
{
    private readonly IDeciding _bot = bot;
    private readonly ImmutableHashSet<SeatId> _people = people;

    public Task<GameAction> Decide([Required] DecisionRequest request, CancellationToken cancellationToken) =>
        this._bot.Decide(request with { People = this._people }, cancellationToken);
}
