using System.Collections.Immutable;
using System.Globalization;
using UtcpSampleBot;

// A bot that plays Zaturnava's games through the server's UTCP manual, calling each tool as the manual says
// (README.md):
//
//     dotnet run -- --server https://the.server/                       a game of its own against the server's bots
//     dotnet run -- --server https://the.server/ --join ID --seat Red    a seat left open for it, in someone's game
//
// The manual is served at {server}/utcp. Seats go by the names the game gives them (in Chaturaji 2027, Black,
// Yellow, Green and Red). With ZATURNAVA_AI_TOKEN set in the environment (an AI token, from a signed-in player's
// profile), it plays as that player's AI; else as an AI guest. A token is never taken on the command line, where
// others may see it. Stopped mid-game and run again with the same --join and --seat, it plays on by the seat's token
// it kept (SeatKeeper).

Options options = Options.Parse(args);
using CancellationTokenSource stop = new();
Console.CancelKeyPress += (_, pressed) =>
{
    pressed.Cancel = true;
    stop.Cancel();
};
using HttpClient http = new();
GameTools tools = new(await UtcpClient.Discover(http, new Uri(options.Server, "utcp"), stop.Token));

(string gameId, int seat) = options.Join is { } joining
    ? (joining, SeatNamed([.. (await tools.Get(joining, stop.Token)).Seats.Select(held => held.Name)], options.Seat))
    : await Create(tools, options, stop.Token);
string? kept = SeatKeeper.Kept(gameId, seat);
string seatToken = kept
    ?? await tools.Claim(
        gameId,
        seat,
        options.Nickname,
        Environment.GetEnvironmentVariable("ZATURNAVA_AI_TOKEN"),
        stop.Token
    );
SeatKeeper.Keep(gameId, seat, seatToken);
Console.WriteLine(
    $"Playing game {gameId}{(kept is null ? "" : ", on from before")}: watch it in the lobby of the server's page."
);

Player player = new(tools, new Greedy(new Random()), options.Pause, Console.Out);
Outcome outcome = await player.Play(gameId, seat, seatToken, stop.Token);
SeatKeeper.Forget(gameId, seat);
Console.WriteLine(outcome.Text);
return outcome.Winners.Contains(seat) ? 0 : 1;

// A game of the ruleset with the bot at its seat, the server's bots at the others: --allies on its own team's
// seats, --opponents on the rest, each list taken in turn, in seat order. Says who sits where.
static async Task<(string GameId, int Seat)> Create(
    GameTools tools,
    Options options,
    CancellationToken cancellationToken
)
{
    Catalog catalog = await tools.Catalog(cancellationToken);
    Ruleset ruleset = catalog.Rulesets.FirstOrDefault(offered => offered.Id == options.Ruleset)
        ?? throw new ArgumentException(
            $"The server has no ruleset {options.Ruleset}: it has "
                + string.Join(", ", catalog.Rulesets.Select(offered => offered.Id))
        );
    int seat = SeatNamed(ruleset.SeatNames, options.Seat);
    ImmutableList<int> others = [.. Enumerable.Range(0, ruleset.SeatCount).Where(other => other != seat)];
    ImmutableList<int> allies = [.. others.Where(other => ruleset.SeatTeams[other] == ruleset.SeatTeams[seat])];
    ImmutableList<int> opponents = [.. others.Except(allies)];
    ImmutableDictionary<int, string> bots = allies
        .Select((other, turn) => (other, Bot: options.Allies[turn % options.Allies.Count]))
        .Concat(opponents.Select((other, turn) => (other, Bot: options.Opponents[turn % options.Opponents.Count])))
        .ToImmutableDictionary(placed => placed.other, placed => placed.Bot);
    foreach (int each in Enumerable.Range(0, ruleset.SeatCount))
    {
        string role = each == seat ? "this bot" : allies.Contains(each) ? "ally" : "opponent";
        Console.WriteLine($"{ruleset.SeatNames[each]}: {(each == seat ? role : $"{bots[each]} ({role})")}");
    }

    string gameId = await tools.Create(
        ruleset.Id,
        Enumerable.Range(0, ruleset.SeatCount).Select(each => bots.GetValueOrDefault(each)),
        cancellationToken
    );
    return (gameId, seat);
}

// The seat of that name, whatever its case; the first, if none is named.
static int SeatNamed(ImmutableList<string> names, string? name) =>
    name is null
        ? 0
        : names.FindIndex(each => string.Equals(each, name, StringComparison.OrdinalIgnoreCase)) is var found
            and >= 0
            ? found
            : throw new ArgumentException($"There is no seat {name}: the seats are {string.Join(", ", names)}");

/// <summary>What the command line asks for.</summary>
internal sealed record Options(
    Uri Server,
    string Ruleset,
    string? Seat,
    ImmutableList<string> Allies,
    ImmutableList<string> Opponents,
    string? Join,
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
            named.GetValueOrDefault("--seat"),
            [.. named.GetValueOrDefault("--allies", "sage").Split(',')],
            [.. named.GetValueOrDefault("--opponents", "sage").Split(',')],
            named.GetValueOrDefault("--join"),
            named.GetValueOrDefault("--nickname", "UTCP sample bot"),
            TimeSpan.FromMilliseconds(
                int.Parse(named.GetValueOrDefault("--pause-ms", "250"), CultureInfo.InvariantCulture)
            )
        );
    }
}
