using System.Collections.Immutable;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Net.NowhereAtAll.Zaturnava.Persistence.EfCore.SqlServer.Test;

/// <summary>The SQL Server migrations, read without a server: none is opened.</summary>
public sealed class SqlServerGamesTest
{
    private const string Provider = "Microsoft.EntityFrameworkCore.SqlServer";

    [Fact]
    public void HasPendingModelChanges_WhenTheModelIsAsLastMigrated_IsFalse()
    {
        // Arrange
        using GameDbContext context = new SqlServerDesignTimeContext().CreateDbContext([]);

        // Act
        bool changed = context.Database.HasPendingModelChanges();

        // Assert
        Assert.False(changed);
    }

    [Fact]
    public void CreateMigration_WhenEachMigrationIsRead_DescribesItsWayUpAndDownAndTheModelItLeaves()
    {
        // Arrange
        using GameDbContext context = new SqlServerDesignTimeContext().CreateDbContext([]);
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
    public void CreateDbContext_WhenAskedByTheTool_KeepsGamesInSqlServerWithTheseMigrations()
    {
        // Arrange
        SqlServerDesignTimeContext factory = new();

        // Act
        using GameDbContext context = factory.CreateDbContext([]);

        // Assert
        Assert.Equal(
            (Provider, typeof(SqlServerGames).Assembly),
            (context.Database.ProviderName, context.GetService<IMigrationsAssembly>().Assembly)
        );
    }
}
