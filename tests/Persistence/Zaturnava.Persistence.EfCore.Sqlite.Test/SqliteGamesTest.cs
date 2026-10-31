using System.Collections.Immutable;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Net.NowhereAtAll.Zaturnava.Persistence.EfCore.Sqlite.Test;

public sealed class SqliteGamesTest : IDisposable
{
    private const string Provider = "Microsoft.EntityFrameworkCore.Sqlite";

    private readonly CancellationToken _cancellation = TestContext.Current.CancellationToken;

    // an in-memory SQLite database lives as long as one open connection to it
    private readonly SqliteConnection _connection = new("DataSource=:memory:");

    [Fact]
    public async Task MigrateAsync_WhenTheDatabaseIsNew_LeavesNoMigrationPending()
    {
        // Arrange
        this._connection.Open();
        await using GameDbContext context = this.Context();

        // Act
        await context.Database.MigrateAsync(this._cancellation);

        // Assert
        Assert.Empty(await context.Database.GetPendingMigrationsAsync(this._cancellation));
    }

    [Fact]
    public void HasPendingModelChanges_WhenTheModelIsAsLastMigrated_IsFalse()
    {
        // Arrange
        using GameDbContext context = new SqliteDesignTimeContext().CreateDbContext([]);

        // Act
        bool changed = context.Database.HasPendingModelChanges();

        // Assert
        Assert.False(changed);
    }

    [Fact]
    public void CreateMigration_WhenEachMigrationIsRead_DescribesItsWayUpAndDownAndTheModelItLeaves()
    {
        // Arrange
        using GameDbContext context = new SqliteDesignTimeContext().CreateDbContext([]);
        IMigrationsAssembly migrations = context.GetService<IMigrationsAssembly>();

        // Act
        ImmutableList<Migration> read =
            [.. migrations.Migrations.Values.Select(type => migrations.CreateMigration(type, Provider))];

        // Assert
        Assert.All(
            read,
            migration => Assert.True(
                migration.UpOperations.Count > 0
                    && migration.DownOperations.Count > 0
                    && migration.TargetModel.GetEntityTypes().Any()
            )
        );
    }

    [Fact]
    public void CreateDbContext_WhenAskedByTheTool_KeepsGamesInSqliteWithTheseMigrations()
    {
        // Arrange
        SqliteDesignTimeContext factory = new();

        // Act
        using GameDbContext context = factory.CreateDbContext([]);

        // Assert
        Assert.Equal(
            (Provider, typeof(SqliteGames).Assembly),
            (context.Database.ProviderName, context.GetService<IMigrationsAssembly>().Assembly)
        );
    }

    public void Dispose() => this._connection.Dispose();

    private GameDbContext Context()
    {
        DbContextOptionsBuilder<GameDbContext> options = new();
        _ = options.UseSqliteGames(this._connection);
        return new GameDbContext(options.Options);
    }
}
