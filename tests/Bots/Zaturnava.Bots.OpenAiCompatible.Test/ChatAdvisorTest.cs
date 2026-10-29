using System.Net;
using System.Text;
using System.Text.Json.Nodes;

namespace Net.NowhereAtAll.Zaturnava.Bots.OpenAiCompatible.Test;

public sealed class ChatAdvisorTest
{
    private const string Model = "some-model";
    private static readonly MoveQuestion Question = new("The board.", ["move a", "move b", "pass"]);

    private readonly CancellationToken _cancellation = TestContext.Current.CancellationToken;

    [Fact]
    public async Task Advise_WhenAsked_PostsTheQuestionToTheModelWithTheChoicesNumbered()
    {
        // Arrange
        using Service service = new(HttpStatusCode.OK, Reply("1"));
        using HttpClient http = service.Client();

        // Act
        _ = await new ChatAdvisor(http, Model).Advise(Question, this._cancellation);

        // Assert
        JsonNode sent = JsonNode.Parse(service.Sent!)!;
        Assert.Equal(
            ("https://advice.test/v1/" + ChatAdvisor.Endpoint, Model, 0, "system", "user"),
            (
                service.Uri,
                (string?)sent["model"],
                (int?)sent["temperature"],
                (string?)sent["messages"]![0]!["role"],
                (string?)sent["messages"]![1]!["role"]
            )
        );
        Assert.Equal(
            "The board.\nThe moves:\n1. move a\n2. move b\n3. pass",
            (string?)sent["messages"]![1]!["content"]
        );
    }

    [Theory]
    [InlineData("2", 1)]
    [InlineData("I would make move 3.", 2)]
    [InlineData("<think>Move 1 or 2? Both lose a piece.</think>3", 2)]
    [InlineData("1", 0)]
    [InlineData("0", null)]
    [InlineData("4", null)]
    [InlineData("pass", null)]
    public async Task Advise_WhenTheModelAnswers_NamesTheChoiceItsFirstNumberDoes(string answer, int? advised)
    {
        // Arrange
        using Service service = new(HttpStatusCode.OK, Reply(answer));
        using HttpClient http = service.Client();

        // Act
        int? choice = await new ChatAdvisor(http, Model).Advise(Question, this._cancellation);

        // Assert
        Assert.Equal(advised, choice);
    }

    [Theory]
    [InlineData("null")]
    [InlineData("{}")]
    [InlineData("""{ "choices": [] }""")]
    [InlineData("""{ "choices": [{}] }""")]
    [InlineData("""{ "choices": [{ "message": { "role": "assistant" } }] }""")]
    [InlineData("""{ "choices": "none" }""")]
    [InlineData("not json")]
    public async Task Advise_WhenTheReplyHasNoAnswer_AdvisesNothing(string body)
    {
        // Arrange
        using Service service = new(HttpStatusCode.OK, body);
        using HttpClient http = service.Client();

        // Act
        int? choice = await new ChatAdvisor(http, Model).Advise(Question, this._cancellation);

        // Assert
        Assert.Null(choice);
    }

    [Theory]
    [InlineData(HttpStatusCode.TooManyRequests)]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.InternalServerError)]
    public async Task Advise_WhenTheServiceRefuses_AdvisesNothing(HttpStatusCode status)
    {
        // Arrange
        using Service service = new(status, Reply("1"));
        using HttpClient http = service.Client();

        // Act
        int? choice = await new ChatAdvisor(http, Model).Advise(Question, this._cancellation);

        // Assert
        Assert.Null(choice);
    }

    private static string Reply(string content) =>
        new JsonObject
        {
            ["choices"] = new JsonArray(
                new JsonObject { ["message"] = new JsonObject { ["role"] = "assistant", ["content"] = content } }
            ),
        }.ToJsonString();
}

/// <summary>
/// A chat service answering every request with <paramref name="status"/> and <paramref name="body"/>.
/// </summary>
file sealed class Service(HttpStatusCode status, string body) : HttpMessageHandler
{
    private readonly HttpStatusCode _status = status;
    private readonly string _body = body;

    public string? Uri { get; private set; }

    public string? Sent { get; private set; }

    public HttpClient Client() => new(this, false) { BaseAddress = new Uri("https://advice.test/v1/") };

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken
    )
    {
        this.Uri = request.RequestUri?.ToString();
        this.Sent = await request.Content!.ReadAsStringAsync(cancellationToken);
        return new HttpResponseMessage(this._status)
        {
            Content = new StringContent(this._body, Encoding.UTF8, "application/json"),
        };
    }
}
