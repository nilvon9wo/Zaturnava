using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Net.NowhereAtAll.Zaturnava.Persistence.EfCore.Test;

/// <summary>
/// A throwaway in-memory SQLite database with <see cref="GameDbContext"/>'s schema, for one test
/// class instance: a real relational database, with no external service.
/// </summary>
internal sealed class SqliteGameDatabase : IDisposable
{
    private const string InMemory = "DataSource=:memory:";

    private readonly SqliteConnection _connection;

    public SqliteGameDatabase()
    {
        // an in-memory SQLite database lives as long as one open connection to it
        this._connection = new SqliteConnection(InMemory);
        this._connection.Open();
        this.Context = this.NewContext();
        _ = this.Context.Database.EnsureCreated();
    }

    public GameDbContext Context { get; }

    /// <summary>A second context on the same database, as a second request would have.</summary>
    public GameDbContext NewContext()
    {
        DbContextOptions<GameDbContext> options = new DbContextOptionsBuilder<GameDbContext>()
            .UseSqlite(this._connection)
            .Options;
        return new GameDbContext(options);
    }

    public void Dispose()
    {
        this.Context.Dispose();
        this._connection.Dispose();
    }
}
