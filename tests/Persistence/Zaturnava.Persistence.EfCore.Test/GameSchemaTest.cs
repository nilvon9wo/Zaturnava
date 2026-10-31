using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Net.NowhereAtAll.Zaturnava.Persistence.EfCore.Entities;
using Net.NowhereAtAll.Zaturnava.Persistence.EfCore.Sqlite;

namespace Net.NowhereAtAll.Zaturnava.Persistence.EfCore.Test;

public sealed class GameSchemaTest : IDisposable
{
    private readonly CancellationToken _cancellation = TestContext.Current.CancellationToken;
    private readonly SqliteGameDatabase _database = new(isMigrated: false);

    [Fact]
    public async Task MigrateGamesAsync_WhenTheDatabaseIsEmpty_AppliesEveryMigration()
    {
        // Arrange
        DbContext context = this._database.Context;

        // Act
        await context.Database.MigrateGamesAsync(this._cancellation);

        // Assert
        Assert.Equal(context.Database.GetMigrations(), await AppliedAsync(context));
    }

    [Fact]
    public async Task MigrateGamesAsync_WhenItsTablesWereMadeBeforeAnyMigration_KeepsThemAndRecordsEveryMigration()
    {
        // Arrange - the tables the server once made all at once (those of the first migration, with no record of
        // it), with a game in them
        DbContext context = this._database.Context;
        await context.GetService<IMigrator>()
            .MigrateAsync(context.Database.GetMigrations().First(), cancellationToken: this._cancellation);
        _ = await context.Database.ExecuteSqlRawAsync("DROP TABLE __EFMigrationsHistory", this._cancellation);
        _ = await context.Database.ExecuteSqlRawAsync(
            "INSERT INTO Games (Id, RulesetId, Version) VALUES ('kept', 'chaturaji-2027', 1)",
            this._cancellation
        );

        // Act
        await context.Database.MigrateGamesAsync(this._cancellation);

        // Assert
        int games = await context.Set<GameEntity>().CountAsync(this._cancellation);
        Assert.Equal(
            (string.Join(",", context.Database.GetMigrations()), 1),
            (string.Join(",", await AppliedAsync(context)), games)
        );
    }

    [Fact]
    public async Task MigrateGamesAsync_WhenAlreadyMigrated_ChangesNothing()
    {
        // Arrange
        DbContext context = this._database.Context;
        await context.Database.MigrateGamesAsync(this._cancellation);

        // Act
        await context.Database.MigrateGamesAsync(this._cancellation);

        // Assert
        Assert.Equal(context.Database.GetMigrations(), await AppliedAsync(context));
    }

    [Fact]
    public async Task MigrateGamesAsync_WhenTheDatabaseDoesNotExistYet_MakesIt()
    {
        // Arrange
        string file = Path.Combine(Path.GetTempPath(), $"zaturnava-{Guid.NewGuid():N}.db");
        DbContextOptionsBuilder<GameDbContext> options = new();
        _ = options.UseSqliteGames($"Data Source={file};Pooling=False");
        await using GameDbContext context = new(options.Options);

        // Act
        await context.Database.MigrateGamesAsync(this._cancellation);

        // Assert
        Assert.Equal(context.Database.GetMigrations(), await AppliedAsync(context));
        File.Delete(file);
    }

    public void Dispose() => this._database.Dispose();

    private static async Task<IEnumerable<string>> AppliedAsync(DbContext context) =>
        await context.Database.GetAppliedMigrationsAsync(TestContext.Current.CancellationToken);
}
