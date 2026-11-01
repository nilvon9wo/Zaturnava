using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Net.NowhereAtAll.Zaturnava.Application.Players;
using Net.NowhereAtAll.Zaturnava.Persistence.EfCore.Entities;

namespace Net.NowhereAtAll.Zaturnava.Persistence.EfCore;

public sealed class GameDbContext(DbContextOptions<GameDbContext> options) : DbContext(options)
{
    // a player's id is a GUID's 32 hex digits; a token's hash, SHA-256's 64
    private const int IdLength = 32;
    private const int TokenHashLength = 64;
    private const int ProviderLength = 40;
    private const int LoginKeyLength = 200;

    // seats and actions are reached only through their game
    public DbSet<GameEntity> Games => this.Set<GameEntity>();

    // what is said at each table: kept apart from the games, as talk changes nothing in play
    public DbSet<ChatEntity> Chats => this.Set<ChatEntity>();

    // the players who have signed in; their logins are reached only through them
    public DbSet<PlayerEntity> Players => this.Set<PlayerEntity>();

    public DbSet<SessionEntity> Sessions => this.Set<SessionEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        _ = modelBuilder.Entity<GameEntity>(MapGame);
        _ = modelBuilder.Entity<SeatEntity>(MapSeat);
        _ = modelBuilder.Entity<ActionEntity>(MapAction);
        _ = modelBuilder.Entity<ChatEntity>(MapChat);
        _ = modelBuilder.Entity<PlayerEntity>(MapPlayer);
        _ = modelBuilder.Entity<LoginEntity>(MapLogin);
        _ = modelBuilder.Entity<SessionEntity>(MapSession);
    }

    private static void MapPlayer(EntityTypeBuilder<PlayerEntity> player)
    {
        _ = player.HasKey(entity => entity.Id);
        _ = player.Property(entity => entity.Id).HasMaxLength(IdLength);
        _ = player.Property(entity => entity.Name).HasMaxLength(PlayerService.MaximumNameLength);
        _ = player.Property(entity => entity.Links)
            .HasMaxLength(PlayerService.MaximumLinks * (PlayerService.MaximumLinkLength + 1));
        _ = player.HasMany(entity => entity.Logins).WithOne().HasForeignKey(login => login.PlayerId);
    }

    private static void MapLogin(EntityTypeBuilder<LoginEntity> login)
    {
        _ = login.HasKey(entity => new { entity.Provider, entity.Key });
        _ = login.Property(entity => entity.Provider).HasMaxLength(ProviderLength);
        _ = login.Property(entity => entity.Key).HasMaxLength(LoginKeyLength);
    }

    private static void MapSession(EntityTypeBuilder<SessionEntity> session)
    {
        _ = session.HasKey(entity => entity.TokenHash);
        _ = session.Property(entity => entity.TokenHash).HasMaxLength(TokenHashLength);
        _ = session.HasOne<PlayerEntity>().WithMany().HasForeignKey(entity => entity.PlayerId);
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
        _ = chat.HasIndex(entity => new { entity.TableId, entity.Number }).IsUnique();
    }
}
