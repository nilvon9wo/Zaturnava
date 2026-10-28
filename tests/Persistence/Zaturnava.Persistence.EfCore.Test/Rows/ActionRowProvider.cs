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

/// <summary>A valid action row: the first action of a generated game, seat 0 passing.</summary>
internal sealed class ActionRowProvider : IRecordProviding
{
    public const string DefaultKind = "pass";
    public const string DefaultArguments = """{"actor":"0"}""";

    public MasterTemplate MasterTemplate { get; } = new MasterTemplate<ActionEntity>(x => x.Id)
    {
        [x => x.Sequence] = 0,
        [x => x.Kind] = DefaultKind,
        [x => x.Arguments] = DefaultArguments,
    }.PutRequired(x => x.GameId, new DefaultRelationship(new GameEntity()));

    public PropertyInfo PrimaryTargetField => Field.Of<ActionEntity>(x => x.Id);

    public Task<Bundle> CreateBundle(GenerationContext context, List<object> templateRecords) =>
        RecordFactory.CreateBundle(context, this.MasterTemplate, templateRecords);
}
