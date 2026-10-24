using System.Reflection;
using Net.NowhereAtAll.Xfty;
using Net.NowhereAtAll.Xfty.Core;
using Net.NowhereAtAll.Xfty.Core.Bundles;
using Net.NowhereAtAll.Xfty.Core.MasterTemplates;
using Net.NowhereAtAll.Xfty.Core.RecordProviders;
using Net.NowhereAtAll.Xfty.Engine;
using Net.NowhereAtAll.Zaturnava.Persistence.EfCore.Entities;

namespace Net.NowhereAtAll.Zaturnava.Persistence.EfCore.Test.Rows;

/// <summary>A valid stored game: the classic ruleset, never saved since it was created.</summary>
internal sealed class GameRowProvider : IRecordProviding
{
    public const string DefaultRulesetId = "classic";

    public MasterTemplate MasterTemplate { get; } = new MasterTemplate<GameEntity>(x => x.Id)
    {
        [x => x.RulesetId] = DefaultRulesetId,
        [x => x.Version] = 0,
    };

    public PropertyInfo PrimaryTargetField => Field.Of<GameEntity>(x => x.Id);

    public Task<Bundle> CreateBundle(GenerationContext context, List<object> templateRecords) =>
        RecordFactory.CreateBundle(context, this.MasterTemplate, templateRecords);
}
