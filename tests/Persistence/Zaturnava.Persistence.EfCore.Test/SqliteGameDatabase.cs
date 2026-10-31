using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Net.NowhereAtAll.Zaturnava.Persistence.EfCore.Sqlite;

namespace Net.NowhereAtAll.Zaturnava.Persistence.EfCore.Test;

/// <summary>
/// A throwaway in-memory SQLite database with <see cref="GameDbContext"/>'s schema, built by its migrations, for
/// one test class instance: a real relational database, with no external service.
/// </summary>
internal sealed class SqliteGameDatabase : IDisposable
{
    private const string InMemory = "DataSource=:memory:";

    private readonly SqliteConnection _connection;

    /// <summary>The database, its schema built by the migrations, if <paramref name="isMigrated"/>.</summary>
    public SqliteGameDatabase(bool isMigrated = true)
    {
        // an in-memory SQLite database lives as long as one open connection to it
        this._connection = new SqliteConnection(InMemory);
        this._connection.Open();
        this.Context = this.NewContext();
        if (isMigrated)
        {
            this.Context.Database.Migrate();
        }
    }

    public GameDbContext Context { get; }

    /// <summary>A second context on the same database, as a second request would have.</summary>
    public GameDbContext NewContext()
    {
        DbContextOptionsBuilder<GameDbContext> options = new();
        _ = options.UseSqliteGames(this._connection);
        return new GameDbContext(options.Options);
    }

    public void Dispose()
    {
        this.Context.Dispose();
        this._connection.Dispose();
    }
}
