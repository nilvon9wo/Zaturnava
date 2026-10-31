using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Net.NowhereAtAll.Zaturnava.Persistence.EfCore.SqlServer;

/// <summary>Games kept in SQL Server, the schema built and changed by this assembly's migrations.</summary>
public static class SqlServerGames
{
    public static DbContextOptionsBuilder UseSqlServerGames(
        [Required] this DbContextOptionsBuilder options,
        [Required] string connection
    ) =>
        options.UseSqlServer(connection, sqlServer => sqlServer.MigrationsAssembly(typeof(SqlServerGames).Assembly));
}

/// <summary>The context as <c>dotnet ef</c> builds it to add a migration: no database is opened.</summary>
public sealed class SqlServerDesignTimeContext : IDesignTimeDbContextFactory<GameDbContext>
{
    private const string Nowhere = "Server=.;Database=Zaturnava";

    public GameDbContext CreateDbContext(string[] args)
    {
        DbContextOptionsBuilder<GameDbContext> options = new();
        _ = options.UseSqlServerGames(Nowhere);
        return new GameDbContext(options.Options);
    }
}
