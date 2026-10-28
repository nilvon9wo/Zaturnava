using System.Reflection;
using Net.NowhereAtAll.Xfty;
using Net.NowhereAtAll.Xfty.Core;
using Net.NowhereAtAll.Xfty.Core.Bundles;
using Net.NowhereAtAll.Xfty.Core.MasterTemplates;
using Net.NowhereAtAll.Xfty.Core.RecordProviders;
using Net.NowhereAtAll.Xfty.Engine;
using Net.NowhereAtAll.Xfty.Relationships;
using Net.NowhereAtAll.Zaturnava.Persistence.EfCore.Entities;

namespace Net.NowhereAtAll.Zaturnava.Persistence.EfCore.Test.Rows;

/// <summary>A valid seat row: seat 0 of a generated game, open.</summary>
internal sealed class SeatRowProvider : IRecordProviding
{
    public const string DefaultKind = "open";

    public MasterTemplate MasterTemplate { get; } = new MasterTemplate<SeatEntity>(x => x.Id)
    {
        [x => x.Seat] = 0,
        [x => x.Kind] = DefaultKind,
    }.PutRequired(x => x.GameId, new DefaultRelationship(new GameEntity()));

    public PropertyInfo PrimaryTargetField => Field.Of<SeatEntity>(x => x.Id);

    public Task<Bundle> CreateBundle(GenerationContext context, List<object> templateRecords) =>
        RecordFactory.CreateBundle(context, this.MasterTemplate, templateRecords);
}
