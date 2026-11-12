using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Net.NowhereAtAll.Zaturnava.Application.Players;
using Net.NowhereAtAll.Zaturnava.Application.Ports;
using Net.NowhereAtAll.Zaturnava.Persistence.EfCore.Entities;

namespace Net.NowhereAtAll.Zaturnava.Persistence.EfCore;

/// <summary>The players who have signed in, their logins and their sessions, in a relational database.</summary>
public sealed class EfPlayerStore(GameDbContext context) : IPlayerPersisting
{
    /// <summary>What escapes a wildcard in a <c>LIKE</c> pattern.</summary>
    private const string LikeEscape = "\\";

    private readonly GameDbContext _context = context;

    public async Task<Player?> ByLogin(Login login, CancellationToken cancellationToken)
    {
        PlayerEntity? player = await this._context.Players
            .Include(entity => entity.Logins)
            .FirstOrDefaultAsync(
                entity => entity.Logins.Any(held => held.Provider == login.Provider && held.Key == login.Key),
                cancellationToken
            );
        return ToPlayer(player);
    }

    public async Task<Player?> ById(PlayerId player, CancellationToken cancellationToken) =>
        ToPlayer(
            await this._context.Players
                .Include(entity => entity.Logins)
                .FirstOrDefaultAsync(entity => entity.Id == player.Value, cancellationToken)
        );

    public async Task<Player?> BySession([Required] string tokenHash, CancellationToken cancellationToken)
    {
        PlayerEntity? player = await this._context.Players
            .Include(entity => entity.Logins)
            .FirstOrDefaultAsync(
                entity => this._context.Sessions.Any(
                    session => session.TokenHash == tokenHash && session.PlayerId == entity.Id
                ),
                cancellationToken
            );
        return ToPlayer(player);
    }

    public async Task<ImmutableDictionary<PlayerId, string>> Names(
        [Required] ImmutableList<PlayerId> players,
        CancellationToken cancellationToken
    )
    {
        List<string> ids = [.. players.Select(player => player.Value).Distinct(StringComparer.Ordinal)];
        List<PlayerEntity> known = await this._context.Players
            .Where(entity => ids.Contains(entity.Id))
            .ToListAsync(cancellationToken);
        return known.ToImmutableDictionary(entity => new PlayerId(entity.Id), entity => entity.Name);
    }

    /// <remarks>
    /// By <c>LIKE</c>, its wildcards escaped: without regard to capitals in SQLite (for ASCII letters) and in SQL
    /// Server under its default collation.
    /// </remarks>
    public Task<bool> IsNameTaken([Required] string name, PlayerId? except, CancellationToken cancellationToken)
    {
        string pattern = name
            .Replace(LikeEscape, LikeEscape + LikeEscape, StringComparison.Ordinal)
            .Replace("%", LikeEscape + "%", StringComparison.Ordinal)
            .Replace("_", LikeEscape + "_", StringComparison.Ordinal)
            .Replace("[", LikeEscape + "[", StringComparison.Ordinal);
        string? exception = except?.Value;
        return this._context.Players.AnyAsync(
            entity => EF.Functions.Like(entity.Name, pattern, LikeEscape) && entity.Id != exception,
            cancellationToken
        );
    }

    public async Task<ImmutableDictionary<PlayerId, DateTimeOffset>> LastSignedIn(
        [Required] ImmutableList<PlayerId> players,
        CancellationToken cancellationToken
    )
    {
        List<string> ids = [.. players.Select(player => player.Value).Distinct(StringComparer.Ordinal)];
        List<PlayerEntity> known = await this._context.Players
            .Where(entity => ids.Contains(entity.Id) && entity.LastSignedIn != null)
            .ToListAsync(cancellationToken);
        return known.ToImmutableDictionary(
            entity => new PlayerId(entity.Id),
            entity => new DateTimeOffset(DateTime.SpecifyKind(entity.LastSignedIn!.Value, DateTimeKind.Utc))
        );
    }

    public async Task MarkSignedIn(PlayerId player, DateTimeOffset time, CancellationToken cancellationToken) =>
        _ = await this._context.Players
            .Where(entity => entity.Id == player.Value)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(entity => entity.LastSignedIn, time.UtcDateTime),
                cancellationToken
            );

    public async Task Add([Required] Player player, CancellationToken cancellationToken)
    {
        _ = this._context.Players.Add(
            new PlayerEntity
            {
                Id = player.Id.Value,
                Name = player.Name,
                Logins =
                [
                    .. player.Logins.Select(login =>
                        new LoginEntity { Provider = login.Provider, Key = login.Key, PlayerId = player.Id.Value }
                    ),
                ],
            }
        );
        _ = await this._context.SaveChangesAsync(cancellationToken);
    }

    public async Task Rename(PlayerId player, [Required] string name, CancellationToken cancellationToken) =>
        _ = await this._context.Players
            .Where(entity => entity.Id == player.Value)
            .ExecuteUpdateAsync(setters => setters.SetProperty(entity => entity.Name, name), cancellationToken);

    public async Task KeepPreferences(
        PlayerId player,
        [Required] ImmutableSortedDictionary<string, string> preferences,
        CancellationToken cancellationToken
    )
    {
        string kept = JsonSerializer.Serialize(preferences);
        _ = await this._context.Players
            .Where(entity => entity.Id == player.Value)
            .ExecuteUpdateAsync(setters => setters.SetProperty(entity => entity.Preferences, kept), cancellationToken);
    }

    public async Task Describe(
        PlayerId player,
        [Required(AllowEmptyStrings = true)] string description,
        [Required] ImmutableSortedSet<string> shown,
        CancellationToken cancellationToken
    )
    {
        string kept = JsonSerializer.Serialize(shown);
        _ = await this._context.Players
            .Where(entity => entity.Id == player.Value)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(entity => entity.Description, description)
                    .SetProperty(entity => entity.Shown, kept),
                cancellationToken
            );
    }

    public async Task AddLogin(PlayerId player, Login login, CancellationToken cancellationToken)
    {
        _ = this._context.Set<LoginEntity>()
            .Add(new LoginEntity { Provider = login.Provider, Key = login.Key, PlayerId = player.Value });
        _ = await this._context.SaveChangesAsync(cancellationToken);
    }

    public async Task RemoveLogin(PlayerId player, Login login, CancellationToken cancellationToken) =>
        _ = await this._context.Set<LoginEntity>()
            .Where(held => held.PlayerId == player.Value && held.Provider == login.Provider && held.Key == login.Key)
            .ExecuteDeleteAsync(cancellationToken);

    public async Task AddSession(PlayerId player, [Required] string tokenHash, CancellationToken cancellationToken)
    {
        _ = this._context.Sessions.Add(new SessionEntity { TokenHash = tokenHash, PlayerId = player.Value });
        _ = await this._context.SaveChangesAsync(cancellationToken);
    }

    public async Task RemoveSession([Required] string tokenHash, CancellationToken cancellationToken) =>
        _ = await this._context.Sessions
            .Where(session => session.TokenHash == tokenHash)
            .ExecuteDeleteAsync(cancellationToken);

    public async Task Merge(PlayerId from, PlayerId into, CancellationToken cancellationToken)
    {
        _ = await this._context.Sessions
            .Where(session => session.PlayerId == from.Value)
            .ExecuteUpdateAsync(set => set.SetProperty(session => session.PlayerId, into.Value), cancellationToken);
        _ = await this._context.Set<LoginEntity>()
            .Where(login => login.PlayerId == from.Value)
            .ExecuteUpdateAsync(set => set.SetProperty(login => login.PlayerId, into.Value), cancellationToken);
        List<StandingEntity> standings = await this._context.Standings
            .AsNoTracking()
            .Where(standing => standing.PlayerId == from.Value || standing.PlayerId == into.Value)
            .ToListAsync(cancellationToken);
        ImmutableDictionary<string, Standing> mine = standings
            .Where(standing => standing.PlayerId == into.Value)
            .ToImmutableDictionary(standing => standing.Game, ToStanding);
        ImmutableList<Standing> merged =
        [
            .. standings
                .Where(standing => standing.PlayerId == from.Value)
                .Select(ToStanding)
                .Select(theirs => mine.GetValueOrDefault(theirs.Game) is { } own
                    ? own.Merged(theirs)
                    : theirs with { Player = into }
                ),
        ];
        await this.Remove(from, cancellationToken);
        await this.Keep(merged, cancellationToken);
    }

    public async Task Remove(PlayerId player, CancellationToken cancellationToken)
    {
        _ = await this._context.Sessions
            .Where(session => session.PlayerId == player.Value)
            .ExecuteDeleteAsync(cancellationToken);
        _ = await this._context.Set<LoginEntity>()
            .Where(login => login.PlayerId == player.Value)
            .ExecuteDeleteAsync(cancellationToken);
        _ = await this._context.Standings
            .Where(standing => standing.PlayerId == player.Value)
            .ExecuteDeleteAsync(cancellationToken);
        _ = await this._context.Players
            .Where(entity => entity.Id == player.Value)
            .ExecuteDeleteAsync(cancellationToken);
    }

    public async Task<ImmutableDictionary<PlayerId, Standing>> Standings(
        [Required] string game,
        [Required] ImmutableList<PlayerId> players,
        CancellationToken cancellationToken
    )
    {
        List<string> ids = [.. players.Select(player => player.Value).Distinct(StringComparer.Ordinal)];
        List<StandingEntity> known = await this._context.Standings
            .AsNoTracking()
            .Where(standing => standing.Game == game && ids.Contains(standing.PlayerId))
            .ToListAsync(cancellationToken);
        return known.ToImmutableDictionary(standing => new PlayerId(standing.PlayerId), ToStanding);
    }

    public async Task Keep([Required] ImmutableList<Standing> standings, CancellationToken cancellationToken)
    {
        foreach (Standing standing in standings)
        {
            StandingEntity? stored = await this._context.Standings.FindAsync(
                [standing.Player.Value, standing.Game],
                cancellationToken
            );
            StandingEntity kept = stored ?? this._context.Standings.Add(
                new StandingEntity { PlayerId = standing.Player.Value, Game = standing.Game }
            ).Entity;
            kept.Elo = standing.Elo;
            kept.Experience = standing.Experience;
            kept.Points = standing.Points;
            kept.Won = standing.Won;
            kept.Lost = standing.Lost;
            kept.Drawn = standing.Drawn;
            (kept.Streak, kept.StreakOpponentElo) = (standing.WinningStreak.Wins, standing.WinningStreak.OpponentElo);
            (kept.RatedStreak, kept.RatedStreakOpponentElo) =
                (standing.RatedStreak.Wins, standing.RatedStreak.OpponentElo);
            (kept.PeopleStreak, kept.PeopleStreakOpponentElo) =
                (standing.PeopleStreak.Wins, standing.PeopleStreak.OpponentElo);
        }

        _ = await this._context.SaveChangesAsync(cancellationToken);
    }

    public async Task<ImmutableDictionary<string, BotStanding>> BotStandings(
        [Required] string game,
        [Required] ImmutableList<string> bots,
        CancellationToken cancellationToken
    )
    {
        List<string> ids = [.. bots.Distinct(StringComparer.Ordinal)];
        List<BotStandingEntity> known = await this._context.BotStandings
            .AsNoTracking()
            .Where(standing => standing.Game == game && ids.Contains(standing.BotId))
            .ToListAsync(cancellationToken);
        return known.ToImmutableDictionary(standing => standing.BotId, ToBotStanding, StringComparer.Ordinal);
    }

    public async Task KeepBots([Required] ImmutableList<BotStanding> standings, CancellationToken cancellationToken)
    {
        foreach (BotStanding standing in standings)
        {
            BotStandingEntity? stored = await this._context.BotStandings.FindAsync(
                [standing.BotId, standing.Game],
                cancellationToken
            );
            BotStandingEntity kept = stored ?? this._context.BotStandings.Add(
                new BotStandingEntity { BotId = standing.BotId, Game = standing.Game }
            ).Entity;
            kept.Points = standing.Points;
            kept.Won = standing.Won;
            kept.Lost = standing.Lost;
            kept.Drawn = standing.Drawn;
            kept.WonAgainstPeople = standing.WonAgainstPeople;
            kept.LostAgainstPeople = standing.LostAgainstPeople;
            kept.DrawnAgainstPeople = standing.DrawnAgainstPeople;
        }

        _ = await this._context.SaveChangesAsync(cancellationToken);
    }

    public async Task<ImmutableList<BotStanding>> BotLeaders(
        [Required] string game,
        int count,
        CancellationToken cancellationToken
    )
    {
        List<BotStandingEntity> leaders = await this._context.BotStandings
            .AsNoTracking()
            .Where(standing => standing.Game == game)
            .OrderByDescending(standing => standing.Won)
            .ThenBy(standing => standing.Lost)
            .ThenBy(standing => standing.BotId)
            .Take(count)
            .ToListAsync(cancellationToken);
        return [.. leaders.Select(ToBotStanding)];
    }

    private static BotStanding ToBotStanding(BotStandingEntity entity) =>
        new(entity.BotId, entity.Game)
        {
            Points = entity.Points,
            Won = entity.Won,
            Lost = entity.Lost,
            Drawn = entity.Drawn,
            WonAgainstPeople = entity.WonAgainstPeople,
            LostAgainstPeople = entity.LostAgainstPeople,
            DrawnAgainstPeople = entity.DrawnAgainstPeople,
        };

    public async Task<ImmutableList<Standing>> Leaders(
        [Required] string game,
        int count,
        CancellationToken cancellationToken
    )
    {
        List<StandingEntity> leaders = await this._context.Standings
            .AsNoTracking()
            .Where(standing => standing.Game == game)
            .OrderByDescending(standing => standing.Elo)
            .ThenByDescending(standing => standing.Experience)
            .ThenBy(standing => standing.PlayerId)
            .Take(count)
            .ToListAsync(cancellationToken);
        return [.. leaders.Select(ToStanding)];
    }

    private static Standing ToStanding(StandingEntity entity) =>
        new(new PlayerId(entity.PlayerId), entity.Game)
        {
            Elo = entity.Elo,
            Experience = entity.Experience,
            Points = entity.Points,
            Won = entity.Won,
            Lost = entity.Lost,
            Drawn = entity.Drawn,
            WinningStreak = new Streak(entity.Streak, entity.StreakOpponentElo),
            RatedStreak = new Streak(entity.RatedStreak, entity.RatedStreakOpponentElo),
            PeopleStreak = new Streak(entity.PeopleStreak, entity.PeopleStreakOpponentElo),
        };

    private static Player? ToPlayer(PlayerEntity? entity) =>
        entity is null
            ? null
            : new Player(
                new PlayerId(entity.Id),
                entity.Name,
                [.. entity.Logins.Select(login => new Login(login.Provider, login.Key))]
            )
            {
                Preferences = PreferencesOf(entity.Preferences),
                Description = entity.Description ?? string.Empty,
                Shown = ShownOf(entity.Shown),
            };

    /// <summary>The services shown, kept as JSON; none, if none were.</summary>
    private static ImmutableSortedSet<string> ShownOf(string? kept) =>
        kept is null
            ? Player.NoneShown
            : Player.NoneShown.Union(JsonSerializer.Deserialize<List<string>>(kept)!);

    /// <summary>The settings kept as JSON; none, if none were.</summary>
    private static ImmutableSortedDictionary<string, string> PreferencesOf(string? kept) =>
        kept is null
            ? Player.NoPreferences
            : Player.NoPreferences.AddRange(JsonSerializer.Deserialize<Dictionary<string, string>>(kept)!);
}
