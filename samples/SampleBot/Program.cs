using System.Collections.Immutable;
using System.Globalization;
using SampleBot;

// A bot that plays Zaturnava's games through the server's public HTTP API, as any program may (README.md):
//
//     dotnet run -- --server https://the.server/                   a game of its own against the server's bots
//     dotnet run -- --server https://the.server/ --join ID --seat N   a seat left open for it, in someone's game
//
// With ZATURNAVA_AI_TOKEN set in the environment (an AI token, from a signed-in player's profile), it plays as that
// player's AI; else as a guest. A token is never taken on the command line, where others may see it.

Options options = Options.Parse(args);
using CancellationTokenSource stop = new();
Console.CancelKeyPress += (_, pressed) =>
{
    pressed.Cancel = true;
    stop.Cancel();
};
using HttpClient http = new() { BaseAddress = options.Server };
GameApi api = new(http);

(string gameId, int seat) = options.Join is { } joining
    ? (joining, options.Seat)
    : (await Create(api, options, stop.Token), 0);
string? playerToken = Environment.GetEnvironmentVariable("ZATURNAVA_AI_TOKEN");
string seatToken = await api.Claim(
    gameId,
    seat,
    new ClaimSeat(options.Nickname, playerToken is not null),
    playerToken,
    stop.Token
);
Console.WriteLine($"Playing seat {seat} of game {gameId}: watch it in the lobby of the server's page.");

Player player = new(api, new Greedy(new Random()), options.Pause, Console.Out);
Outcome outcome = await player.Play(gameId, seat, seatToken, stop.Token);
Console.WriteLine(outcome.Text);
return outcome.Winners.Contains(seat) ? 0 : 1;

// A game of the ruleset with the bot at seat 0 and the server's bots, in turn, at the others.
static async Task<string> Create(GameApi api, Options options, CancellationToken cancellationToken)
{
    Catalog catalog = await api.Catalog(cancellationToken);
    Ruleset ruleset = catalog.Rulesets.FirstOrDefault(offered => offered.Id == options.Ruleset)
        ?? throw new ArgumentException(
            $"The server has no ruleset {options.Ruleset}: it has "
                + string.Join(", ", catalog.Rulesets.Select(offered => offered.Id))
        );
    ImmutableList<string?> bots =
    [
        null,
        .. Enumerable.Range(0, ruleset.SeatCount - 1)
            .Select(other => options.Opponents[other % options.Opponents.Count]),
    ];
    return await api.Create(ruleset.Id, bots, cancellationToken);
}

/// <summary>What the command line asks for.</summary>
internal sealed record Options(
    Uri Server,
    string Ruleset,
    ImmutableList<string> Opponents,
    string? Join,
    int Seat,
    string Nickname,
    TimeSpan Pause
)
{
    public static Options Parse(string[] args)
    {
        ImmutableDictionary<string, string> named = Enumerable.Range(0, args.Length / 2)
            .ToImmutableDictionary(pair => args[2 * pair], pair => args[(2 * pair) + 1], StringComparer.Ordinal);
        string server = named.GetValueOrDefault("--server")
            ?? throw new ArgumentException("Say which server: --server https://...");
        return new Options(
            new Uri(server.EndsWith('/') ? server : server + "/"),
            named.GetValueOrDefault("--ruleset", "chaturaji-2027"),
            [.. named.GetValueOrDefault("--opponents", "sage").Split(',')],
            named.GetValueOrDefault("--join"),
            int.Parse(named.GetValueOrDefault("--seat", "0"), CultureInfo.InvariantCulture),
            named.GetValueOrDefault("--nickname", "Sample bot"),
            TimeSpan.FromMilliseconds(
                int.Parse(named.GetValueOrDefault("--pause-ms", "250"), CultureInfo.InvariantCulture)
            )
        );
    }
}
