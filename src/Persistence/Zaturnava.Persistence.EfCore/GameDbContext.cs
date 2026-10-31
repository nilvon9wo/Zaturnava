using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Net.NowhereAtAll.Zaturnava.Persistence.EfCore.Entities;

namespace Net.NowhereAtAll.Zaturnava.Persistence.EfCore;

public sealed class GameDbContext(DbContextOptions<GameDbContext> options) : DbContext(options)
{
    // seats and actions are reached only through their game
    public DbSet<GameEntity> Games => this.Set<GameEntity>();

    // what is said at a game's table: kept apart from the game, as talk changes nothing in play
    public DbSet<ChatEntity> Chats => this.Set<ChatEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        _ = modelBuilder.Entity<GameEntity>(MapGame);
        _ = modelBuilder.Entity<SeatEntity>(MapSeat);
        _ = modelBuilder.Entity<ActionEntity>(MapAction);
        _ = modelBuilder.Entity<ChatEntity>(MapChat);
    }

    private static void MapGame(EntityTypeBuilder<GameEntity> game)
    {
        _ = game.HasKey(entity => entity.Id);
        _ = game.Property(entity => entity.Version).IsConcurrencyToken();
        _ = game.HasMany(entity => entity.Seats).WithOne().HasForeignKey(seat => seat.GameId);
        _ = game.HasMany(entity => entity.Actions).WithOne().HasForeignKey(action => action.GameId);
    }

    private static void MapSeat(EntityTypeBuilder<SeatEntity> seat)
    {
        _ = seat.HasKey(entity => entity.Id);
        _ = seat.HasIndex(entity => new { entity.GameId, entity.Seat }).IsUnique();
    }

    private static void MapAction(EntityTypeBuilder<ActionEntity> action)
    {
        _ = action.HasKey(entity => entity.Id);
        _ = action.HasIndex(entity => new { entity.GameId, entity.Sequence }).IsUnique();
    }

    private static void MapChat(EntityTypeBuilder<ChatEntity> chat)
    {
        _ = chat.HasKey(entity => entity.Id);
        _ = chat.HasIndex(entity => new { entity.GameId, entity.Number }).IsUnique();
    }
}
