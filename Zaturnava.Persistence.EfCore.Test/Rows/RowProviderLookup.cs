using Net.NowhereAtAll.Xfty.Core.RecordProviders;
using Net.NowhereAtAll.Xfty.Lookup;
using Net.NowhereAtAll.Zaturnava.Persistence.EfCore.Entities;

namespace Net.NowhereAtAll.Zaturnava.Persistence.EfCore.Test.Rows;

/// <summary>Which Xfty provider generates each kind of row.</summary>
internal sealed class RowProviderLookup : IProviderLocating
{
    private static readonly Dictionary<IRecordIdentifying, Type> ProviderTypeByKey = new()
    {
        [LookupKey.Get<GameEntity>()] = typeof(GameRowProvider),
        [LookupKey.Get<SeatEntity>()] = typeof(SeatRowProvider),
        [LookupKey.Get<ActionEntity>()] = typeof(ActionRowProvider),
    };

    private readonly Dictionary<IRecordIdentifying, IRecordProviding> _instanceCache = [];

    public IRecordProviding Get(Type recordType) => this.Get(LookupKey.Get(recordType));

    public IRecordProviding Get(IRecordIdentifying lookupKey) =>
        ProviderLookups.Get(
            ProviderTypeByKey,
            this._instanceCache,
            lookupKey
        );

    public ISet<IRecordIdentifying> KeysFor(object? record) =>
        ProviderLookups.KeysFor(ProviderTypeByKey.Keys.ToHashSet(), record);
}
