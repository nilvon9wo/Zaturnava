using System.Collections.Immutable;
using System.Globalization;
using System.Text.Json;
using Net.NowhereAtAll.Zaturnava.Application.Games;
using Net.NowhereAtAll.Zaturnava.Engine.Actions;
using Net.NowhereAtAll.Zaturnava.Engine.Seats;
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
        return new GameRecord(new GameId(entity.Id), entity.RulesetId, controllers, actions, entity.Version)
        {
            IsOver = entity.IsOver,
            Created = ToOffset(entity.Created),
            LastChanged = ToOffset(entity.LastChanged),
            Away = AwayOf(entity.Seats),
            HasChangedHands = entity.HasChangedHands,
        };
    }

    /// <summary>The seats away, in seat order.</summary>
    private static ImmutableList<SeatId> AwayOf(IReadOnlyCollection<SeatEntity> seats) =>
        [.. seats.Where(seat => seat.IsAway).Select(seat => new SeatId(seat.Seat)).OrderBy(seat => seat.Value)];

    public static GameEntity ToEntity(GameRecord record) =>
        new()
        {
            Id = record.Id.Value,
            RulesetId = record.RulesetId,
            Version = record.Version,
            IsOver = record.IsOver,
            Created = ToUtc(record.Created),
            LastChanged = ToUtc(record.LastChanged),
            HasChangedHands = record.HasChangedHands,
            Seats = [.. record.Controllers.Select((controller, seat) => ToSeat(record, seat, controller))],
            Actions = [.. NewActions(record, 0)],
        };

    /// <summary>The record's actions from <paramref name="sequence"/> on, as rows.</summary>
    public static IEnumerable<ActionEntity> NewActions(GameRecord record, int sequence) =>
        record.Actions
            .Skip(sequence)
            .Select((descriptor, offset) => ToAction(record.Id, sequence + offset, descriptor));

    /// <summary>The time as UTC, which a database orders by whatever its offset was.</summary>
    public static DateTime? ToUtc(DateTimeOffset? time) => time?.UtcDateTime;

    public static void Update(SeatEntity seat, SeatController controller)
    {
        seat.Kind = controller.Kind;
        seat.Detail = controller.Detail;
    }

    /// <summary>The seat as <paramref name="record"/> has it: who controls it, and whether it is away.</summary>
    public static void Update(SeatEntity seat, GameRecord record)
    {
        Update(seat, record.Controllers[seat.Seat]);
        seat.IsAway = record.Away.Contains(new SeatId(seat.Seat));
    }

    private static SeatEntity ToSeat(GameRecord record, int seat, SeatController controller) =>
        new()
        {
            Id = ChildId(record.Id, seat),
            GameId = record.Id.Value,
            Seat = seat,
            Kind = controller.Kind,
            Detail = controller.Detail,
            IsAway = record.Away.Contains(new SeatId(seat)),
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

    /// <summary>The game as a lobby lists it, from its row, its seats and how many actions it has.</summary>
    public static GameListing ToListing(GameEntity entity, IReadOnlyCollection<SeatEntity> seats, int moves) =>
        new(
            new GameId(entity.Id),
            entity.RulesetId,
            entity.Version,
            [.. seats.OrderBy(seat => seat.Seat).Select(ToController)],
            moves,
            entity.IsOver ?? false
        )
        {
            Created = ToOffset(entity.Created),
            LastChanged = ToOffset(entity.LastChanged),
            Away = AwayOf(seats),
        };

    private static DateTimeOffset? ToOffset(DateTime? utc) =>
        utc is { } time ? new DateTimeOffset(DateTime.SpecifyKind(time, DateTimeKind.Utc)) : null;

    private static SeatController ToController(SeatEntity seat) => SeatController.From(seat.Kind, seat.Detail);

    private static ActionDescriptor ToDescriptor(ActionEntity action)
    {
        ImmutableSortedDictionary<string, string> arguments =
            JsonSerializer.Deserialize<ImmutableSortedDictionary<string, string>>(action.Arguments)!;
        return new ActionDescriptor(action.Kind, arguments);
    }

    private static string ChildId(GameId id, int index) => $"{id.Value}:{index.ToString(CultureInfo.InvariantCulture)}";
}
