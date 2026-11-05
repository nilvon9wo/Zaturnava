using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace Net.NowhereAtAll.Zaturnava.Server.Test;

/// <summary>The UTCP manual served at <c>utcp</c>, and its tools, called as a UTCP client calls them.</summary>
public sealed class UtcpApiTest(ZaturnavaServer server) : IClassFixture<ZaturnavaServer>
{
    private readonly HttpClient _http = server.CreateClient();
    private readonly CancellationToken _cancellation = TestContext.Current.CancellationToken;

    [Fact]
    public async Task Get_WhenAsked_OffersTheMcpServersToolsEachAsACallToThisServer()
    {
        // Arrange - nothing beyond the server

        // Act
        JsonElement manual = await this.Manual();

        // Assert
        JsonElement submit = Tool(manual, "submit_action").GetProperty("tool_call_template");
        Assert.Equal(
            (
                "1.0.1",
                "catalog,list_games,create_game,claim_seat,get_game,game_history,advance_game,submit_action,"
                    + "read_chat,say",
                "POST",
                new Uri(this._http.BaseAddress!, "api/games/{gameId}/actions").OriginalString,
                "body",
                "Authorization"
            ),
            (
                manual.GetProperty("utcp_version").GetString(),
                string.Join(',', manual.GetProperty("tools").EnumerateArray().Select(Name)),
                submit.GetProperty("http_method").GetString(),
                submit.GetProperty("url").GetString(),
                submit.GetProperty("body_field").GetString(),
                submit.GetProperty("header_fields")[0].GetString()
            )
        );
    }

    [Fact]
    public async Task Call_WhenATool_IsCalledAsTheManualSays_DoesWhatItsRouteDoes()
    {
        // Arrange - a game created and its open seat claimed, each by its tool's template, as a client would
        JsonElement manual = await this.Manual();
        JsonElement created = await this.Called(
            Tool(manual, "create_game"),
            new { rulesetId = ZaturnavaServer.Chaturaji2027, bots = ZaturnavaServer.OneOpenSeat },
            ("{gameId}", string.Empty),
            null
        );
        string gameId = created.GetProperty("gameId").GetString()!;
        JsonElement claimed = await this.Called(
            Tool(manual, "claim_seat"),
            new { nickname = "Tool", isAi = true },
            ("{gameId}", gameId),
            null
        );

        // Act
        JsonElement view = await this.Called(
            Tool(manual, "get_game"),
            null,
            ("{gameId}", gameId),
            claimed.GetProperty("token").GetString()
        );

        // Assert
        JsonElement seat = view.GetProperty("seats")[0];
        Assert.Equal(
            ("human", true),
            (seat.GetProperty("controller").GetString(), seat.GetProperty("isAi").GetBoolean())
        );
    }

    private async Task<JsonElement> Manual() =>
        await this._http.GetFromJsonAsync<JsonElement>("utcp", this._cancellation);

    /// <summary>
    /// The tool called as its template says: its address filled (the seat, always 0), the body sent as JSON, and the
    /// token, if any, as its Authorization header.
    /// </summary>
    private async Task<JsonElement> Called(
        JsonElement tool,
        object? body,
        (string Name, string Value) path,
        string? token
    )
    {
        JsonElement template = tool.GetProperty("tool_call_template");
        using HttpRequestMessage request = new(
            new HttpMethod(template.GetProperty("http_method").GetString()!),
            template.GetProperty("url").GetString()!.Replace(path.Name, path.Value, StringComparison.Ordinal)
                .Replace("{seat}", "0", StringComparison.Ordinal)
        );
        request.Content = body is null ? null : JsonContent.Create(body);
        request.Headers.Authorization = token is null ? null : new AuthenticationHeaderValue("Bearer", token);
        using HttpResponseMessage response = await this._http.SendAsync(request, this._cancellation);
        _ = response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<JsonElement>(this._cancellation);
    }

    private static JsonElement Tool(JsonElement manual, string name) =>
        manual.GetProperty("tools").EnumerateArray().First(tool => Name(tool) == name);

    private static string? Name(JsonElement tool) => tool.GetProperty("name").GetString();
}
