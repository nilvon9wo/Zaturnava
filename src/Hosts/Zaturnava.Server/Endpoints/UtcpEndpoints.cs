using System.Collections.Immutable;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Net.NowhereAtAll.Zaturnava.Contracts;

namespace Net.NowhereAtAll.Zaturnava.Server.Endpoints;

/// <summary>
/// The server's routes as a UTCP manual (<see href="https://www.utcp.io"/>), at <c>utcp</c>: the same tools the MCP
/// server offers, by the same names, but each telling a client how to call the HTTP API itself, with no server in
/// between. A tool's inputs fill the route's path (<c>{gameId}</c>, <c>{seat}</c>), its JSON body (<c>body</c>), its
/// <c>Authorization</c> header ("Bearer {token}"), and, the rest, its query. The addresses are this server's own
/// (<see cref="McpEndpoints.SelfAddressOf"/>).
/// </summary>
internal static class UtcpEndpoints
{
    public const string Route = "utcp";

    public const string UtcpVersion = "1.0.1";

    /// <summary>The manual's own version: raised when its tools change.</summary>
    public const string ManualVersion = "1.0.0";

    private const string Body = "body";
    private const string Authorization = "Authorization";

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public static IEndpointRouteBuilder MapUtcp(this IEndpointRouteBuilder routes)
    {
        _ = routes.MapGet(
            Route,
            (HttpRequest request, IConfiguration configuration) =>
                TypedResults.Json(ManualAt(McpEndpoints.SelfAddressOf(configuration, request)), Json)
        );
        return routes;
    }

    /// <summary>The manual, its tools calling the server at <paramref name="address"/>.</summary>
    public static UtcpManual ManualAt(string address)
    {
        string game = $"{address}{GameRoutes.Games}/{{gameId}}";
        return new UtcpManual(
            UtcpVersion,
            ManualVersion,
            [
                Tool(
                    "catalog",
                    "The rulesets games can be created with (their seats' names and teams), and the bots that can take "
                        + "a seat.",
                    Inputs(),
                    new HttpCallTemplate("http", "GET", address + GameRoutes.Catalog)
                ),
                Tool(
                    "list_games",
                    "A page of the games on the server, newest first, with the seats still open for a player to claim.",
                    Inputs(
                        Input("game", Text("Only games of this game, at any size, by its id; else any.")),
                        Input("ruleset", Text("Only games of this ruleset id, one game at one size; else any.")),
                        Input("open", Typed("boolean", "true for only games waiting for players; false, under way.")),
                        Input("page", Typed("integer", "The page, from 1. A page holds 20 games."))
                    ),
                    new HttpCallTemplate("http", "GET", address + GameRoutes.Games)
                ),
                Tool(
                    "create_game",
                    "Creates a game. For each seat in order, a bot's id has it play that seat; null leaves the seat "
                        + "open for a player (you, with claim_seat).",
                    Inputs(
                        Input(
                            Body,
                            Inputs(
                                Input("rulesetId", Text("A ruleset id, from catalog.")),
                                Input(
                                    "bots",
                                    new JsonObject
                                    {
                                        ["type"] = "array",
                                        ["items"] = new JsonObject { ["type"] = new JsonArray("string", "null") },
                                        ["description"] = "One entry per seat: a bot id, or null for an open seat.",
                                    }
                                )
                            ),
                            isRequired: true
                        )
                    ),
                    Posting(address + GameRoutes.Games)
                ),
                Tool(
                    "claim_seat",
                    "Claims an open seat, for an AI: returns the token that holds it, which every action for the seat "
                        + "needs, and which the server cannot show again. With a player's token (an AI token from "
                        + "their profile), the seat is theirs, shown as \"Name (AI)\"; else an AI guest's, by its "
                        + "nickname.",
                    Inputs(
                        Input("gameId", Text("The game's id."), isRequired: true),
                        Input("seat", Typed("integer", "The seat's number, from 0."), isRequired: true),
                        Input(
                            Body,
                            Inputs(
                                Input("nickname", Text("The name shown, as \"Nickname (AI guest)\"; at most 20.")),
                                Input("isAi", Typed("boolean", "true: the seat is shown as one an AI plays."))
                            ),
                            isRequired: true
                        ),
                        Input(Authorization, Text("\"Bearer {player token}\", to play for that player; else none."))
                    ),
                    Posting(game + "/seats/{seat}", Authorization)
                ),
                Tool(
                    "get_game",
                    "A game as it stands: the cells, the pieces, the seats and scores, whose decision it is "
                        + "(decidingSeat, null once over), that seat's legal actions, the dice, and what happened "
                        + "lately.",
                    Inputs(Input("gameId", Text("The game's id."), isRequired: true)),
                    new HttpCallTemplate("http", "GET", game)
                ),
                Tool(
                    "game_history",
                    "A game as data to learn from: each seat (who played it, and its score), every action taken in "
                        + "order, and how it ended (null while it goes on).",
                    Inputs(Input("gameId", Text("The game's id."), isRequired: true)),
                    new HttpCallTemplate("http", "GET", game + "/history")
                ),
                Tool(
                    "advance_game",
                    "Lets the bot whose turn it is make one decision, if the game is still at the version given (from "
                        + "get_game); bots only play when someone advances the game. Returns the game after it.",
                    Inputs(
                        Input("gameId", Text("The game's id."), isRequired: true),
                        Input(
                            Body,
                            Inputs(Input("version", Typed("integer", "The game's version, as get_game gave it."))),
                            isRequired: true
                        )
                    ),
                    Posting(game + "/advance")
                ),
                Tool(
                    "submit_action",
                    "Takes an action for the seat a token holds, when it is that seat's decision: one of get_game's "
                        + "legalActions, its kind and arguments copied. Returns the game after it.",
                    Inputs(
                        Input("gameId", Text("The game's id."), isRequired: true),
                        Input(
                            Body,
                            Inputs(
                                Input("kind", Text("The action's kind, as in legalActions.")),
                                Input(
                                    "arguments",
                                    new JsonObject
                                    {
                                        ["type"] = "object",
                                        ["additionalProperties"] = new JsonObject { ["type"] = "string" },
                                        ["description"] = "The action's arguments, as in legalActions.",
                                    }
                                )
                            ),
                            isRequired: true
                        ),
                        Input(Authorization, Text("\"Bearer {seat token}\", as claim_seat gave it."), isRequired: true)
                    ),
                    Posting(game + "/actions", Authorization)
                ),
                Tool(
                    "read_chat",
                    "What was said at a game's table after message number 'after' (0 for everything): to the whole "
                        + "table, and, with a seat's token, what its team said to itself.",
                    Inputs(
                        Input("gameId", Text("The game's id."), isRequired: true),
                        Input("after", Typed("integer", "The number of the last message already read; 0 for all.")),
                        Input(Authorization, Text("\"Bearer {seat token}\", to read the team's talk too; else none."))
                    ),
                    new HttpCallTemplate("http", "GET", game + "/chat") { HeaderFields = [Authorization] }
                ),
                Tool(
                    "say",
                    "Says something at a game's table, as the seat a token holds: to the whole table, or to its team "
                        + "alone. At most 500 characters. A guest's seat may not speak.",
                    Inputs(
                        Input("gameId", Text("The game's id."), isRequired: true),
                        Input(
                            Body,
                            Inputs(
                                Input("channel", Text("Whom to say it to: \"table\" or \"team\".")),
                                Input("text", Text("What to say."))
                            ),
                            isRequired: true
                        ),
                        Input(Authorization, Text("\"Bearer {seat token}\", as claim_seat gave it."), isRequired: true)
                    ),
                    Posting(game + "/chat", Authorization)
                ),
            ]
        );
    }

    private static UtcpTool Tool(string name, string description, JsonObject inputs, HttpCallTemplate call) =>
        new(name, description, inputs, new JsonObject { ["type"] = "object" }, ["zaturnava"], call);

    /// <summary>A POST of the tool's <c>body</c> input, as JSON, with the <paramref name="headers"/> inputs.</summary>
    private static HttpCallTemplate Posting(string url, params string[] headers) =>
        new("http", "POST", url)
        {
            ContentType = "application/json",
            BodyField = Body,
            HeaderFields = headers.Length == 0 ? null : [.. headers],
        };

    /// <summary>An object of the inputs given, those marked required named so.</summary>
    private static JsonObject Inputs(params (string Name, JsonObject Schema, bool IsRequired)[] inputs) =>
        new()
        {
            ["type"] = "object",
            ["properties"] = new JsonObject(
                inputs.Select(input => KeyValuePair.Create(input.Name, (JsonNode?)input.Schema))
            ),
            ["required"] = new JsonArray(
                [.. inputs.Where(input => input.IsRequired).Select(input => (JsonNode?)JsonValue.Create(input.Name))]
            ),
        };

    private static (string Name, JsonObject Schema, bool IsRequired) Input(
        string name,
        JsonObject schema,
        bool isRequired = false
    ) =>
        (name, schema, isRequired);

    private static JsonObject Text(string description) => Typed("string", description);

    private static JsonObject Typed(string type, string description) =>
        new() { ["type"] = type, ["description"] = description };
}

/// <summary>A UTCP manual: the protocol's version, the manual's, and its tools.</summary>
internal sealed record UtcpManual(string UtcpVersion, string ManualVersion, ImmutableList<UtcpTool> Tools);

/// <summary>A UTCP tool: its name, what it does, its inputs and outputs (JSON Schema), and how to call it.</summary>
internal sealed record UtcpTool(
    string Name,
    string Description,
    JsonObject Inputs,
    JsonObject Outputs,
    ImmutableList<string> Tags,
    HttpCallTemplate ToolCallTemplate
);

/// <summary>
/// How to call a tool over HTTP: the method and address (its <c>{input}</c>s filled from the inputs), and which input
/// is the body, and which are headers; the rest are the query.
/// </summary>
internal sealed record HttpCallTemplate(string CallTemplateType, string HttpMethod, string Url)
{
    public string? ContentType { get; init; }

    public string? BodyField { get; init; }

    public ImmutableList<string>? HeaderFields { get; init; }
}
