using System.Collections.Immutable;
using System.Globalization;
using System.Text.Json;
using Net.NowhereAtAll.Zaturnava.Application.Games;
using Net.NowhereAtAll.Zaturnava.Engine.Actions;
using Net.NowhereAtAll.Zaturnava.Persistence.EfCore.Entities;

namespace Net.NowhereAtAll.Zaturnava.Persistence.EfCore;

/// <summary>Between the application's records and the database's rows.</summary>
internal static class GameRecordMapping
{
    public static GameRecord ToRecord(GameEntity entity)
    {
        ImmutableList<SeatController> controllers = [.. entity.Seats.OrderBy(seat => seat.Seat).Select(ToController)];
        ImmutableList<ActionDescriptor> actions =
            [.. entity.Actions.OrderBy(action => action.Sequence).Select(ToDescriptor)];
        return new GameRecord(new GameId(entity.Id), entity.RulesetId, controllers, actions, entity.Version);
    }

    public static GameEntity ToEntity(GameRecord record) =>
        new()
        {
            Id = record.Id.Value,
            RulesetId = record.RulesetId,
            Version = record.Version,
            Seats = [.. record.Controllers.Select((controller, seat) => ToSeat(record.Id, seat, controller))],
            Actions = [.. NewActions(record, 0)],
        };

    /// <summary>The record's actions from <paramref name="sequence"/> on, as rows.</summary>
    public static IEnumerable<ActionEntity> NewActions(GameRecord record, int sequence) =>
        record.Actions
            .Skip(sequence)
            .Select((descriptor, offset) => ToAction(record.Id, sequence + offset, descriptor));

    public static void Update(SeatEntity seat, SeatController controller)
    {
        seat.Kind = controller.Kind;
        seat.Detail = controller.Detail;
    }

    private static SeatEntity ToSeat(GameId id, int seat, SeatController controller) =>
        new()
        {
            Id = ChildId(id, seat),
            GameId = id.Value,
            Seat = seat,
            Kind = controller.Kind,
            Detail = controller.Detail,
        };

    private static ActionEntity ToAction(GameId id, int sequence, ActionDescriptor descriptor) =>
        new()
        {
            Id = ChildId(id, sequence),
            GameId = id.Value,
            Sequence = sequence,
            Kind = descriptor.Kind,
            Arguments = JsonSerializer.Serialize(descriptor.Arguments),
        };

    private static SeatController ToController(SeatEntity seat) => SeatController.From(seat.Kind, seat.Detail);

    private static ActionDescriptor ToDescriptor(ActionEntity action)
    {
        ImmutableSortedDictionary<string, string> arguments =
            JsonSerializer.Deserialize<ImmutableSortedDictionary<string, string>>(action.Arguments)!;
        return new ActionDescriptor(action.Kind, arguments);
    }

    private static string ChildId(GameId id, int index) => $"{id.Value}:{index.ToString(CultureInfo.InvariantCulture)}";
}
