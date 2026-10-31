using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage;

namespace Net.NowhereAtAll.Zaturnava.Persistence.EfCore;

/// <summary>
/// The games' schema, brought up to date by the migrations of the provider in use. A database whose tables were
/// made before there were any migrations (all at once, from the model) is first marked as having had the first
/// migration, which makes those same tables; so that it is changed from there, not made again.
/// </summary>
public static class GameSchema
{
    public static async Task MigrateGamesAsync(
        [Required] this DatabaseFacade database,
        CancellationToken cancellationToken
    )
    {
        if (await IsUnmigratedAsync(database, cancellationToken))
        {
            await AdoptAsync(database, cancellationToken);
        }

        await database.MigrateAsync(cancellationToken);
    }

    /// <summary>Whether the database has tables, but no record of any migration.</summary>
    private static async Task<bool> IsUnmigratedAsync(DatabaseFacade database, CancellationToken cancellationToken)
    {
        IRelationalDatabaseCreator creator = database.GetService<IRelationalDatabaseCreator>();
        return await creator.ExistsAsync(cancellationToken)
            && await creator.HasTablesAsync(cancellationToken)
            && !await database.GetService<IHistoryRepository>().ExistsAsync(cancellationToken);
    }

    private static async Task AdoptAsync(DatabaseFacade database, CancellationToken cancellationToken)
    {
        IHistoryRepository history = database.GetService<IHistoryRepository>();
        HistoryRow first = new(database.GetMigrations().First(), ProductInfo.GetVersion());
        _ = await database.ExecuteSqlRawAsync(history.GetCreateIfNotExistsScript(), cancellationToken);
        _ = await database.ExecuteSqlRawAsync(history.GetInsertScript(first), cancellationToken);
    }
}
