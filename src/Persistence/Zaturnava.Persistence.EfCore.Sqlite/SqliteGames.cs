using System.ComponentModel.DataAnnotations;
using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Net.NowhereAtAll.Zaturnava.Persistence.EfCore.Sqlite;

/// <summary>Games kept in SQLite, the schema built and changed by this assembly's migrations.</summary>
public static class SqliteGames
{
    public static DbContextOptionsBuilder UseSqliteGames(
        [Required] this DbContextOptionsBuilder options,
        [Required] string connection
    ) =>
        options.UseSqlite(connection, sqlite => sqlite.MigrationsAssembly(typeof(SqliteGames).Assembly));

    /// <summary>On a connection the caller keeps open and disposes, as an in-memory database needs.</summary>
    public static DbContextOptionsBuilder UseSqliteGames(
        [Required] this DbContextOptionsBuilder options,
        [Required] DbConnection connection
    ) =>
        options.UseSqlite(connection, sqlite => sqlite.MigrationsAssembly(typeof(SqliteGames).Assembly));
}

/// <summary>The context as <c>dotnet ef</c> builds it to add a migration: no database is opened.</summary>
public sealed class SqliteDesignTimeContext : IDesignTimeDbContextFactory<GameDbContext>
{
    private const string Nowhere = "Data Source=zaturnava.db";

    public GameDbContext CreateDbContext(string[] args)
    {
        DbContextOptionsBuilder<GameDbContext> options = new();
        _ = options.UseSqliteGames(Nowhere);
        return new GameDbContext(options.Options);
    }
}
