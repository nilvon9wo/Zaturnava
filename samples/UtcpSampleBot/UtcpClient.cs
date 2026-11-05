using System.Collections.Immutable;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace UtcpSampleBot;

/// <summary>
/// A small UTCP client, for tools called over HTTP (https://www.utcp.io): it reads a manual, and calls each tool as
/// its call template says. An input named in the address (<c>{gameId}</c>) fills it; the template's
/// <c>body_field</c> input is the JSON body; its <c>header_fields</c> inputs are headers of their names; the rest
/// are the query. The answer is read as JSON. A full client (Python's or TypeScript's) does more: other protocols,
/// authentication from its configuration, variables. This one does what Zaturnava's manual needs.
/// </summary>
internal sealed class UtcpClient
{
    private static readonly JsonSerializerOptions ManualJson = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
    };

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _http;
    private readonly ImmutableDictionary<string, CallTemplate> _tools;

    private UtcpClient(HttpClient http, ImmutableDictionary<string, CallTemplate> tools)
    {
        this._http = http;
        this._tools = tools;
    }

    /// <summary>A client of the tools in the manual at <paramref name="manual"/>.</summary>
    public static async Task<UtcpClient> Discover(HttpClient http, Uri manual, CancellationToken cancellationToken)
    {
        Manual read = await http.GetFromJsonAsync<Manual>(manual, ManualJson, cancellationToken)
            ?? throw new RefusedException("Empty", $"There is no manual at {manual}.");
        return new UtcpClient(http, read.Tools.ToImmutableDictionary(tool => tool.Name, tool => tool.ToolCallTemplate));
    }

    /// <summary>What the tool answers to the inputs, as <typeparamref name="T"/>.</summary>
    public async Task<T> Call<T>(
        string tool,
        IReadOnlyDictionary<string, object?> inputs,
        CancellationToken cancellationToken
    )
    {
        CallTemplate template = this._tools.GetValueOrDefault(tool)
            ?? throw new RefusedException("NoSuchTool", $"The manual has no tool {tool}.");
        ImmutableList<string> headers = template.HeaderFields ?? [];
        ImmutableList<KeyValuePair<string, object?>> given = [.. inputs.Where(input => input.Value is not null)];
        string url = given.Aggregate(
            template.Url,
            (filled, input) => filled.Replace($"{{{input.Key}}}", Escaped(input.Value), StringComparison.Ordinal)
        );
        ImmutableList<string> query =
        [
            .. given
                .Where(input => !template.Url.Contains($"{{{input.Key}}}", StringComparison.Ordinal))
                .Where(input => input.Key != template.BodyField && !headers.Contains(input.Key))
                .Select(input => $"{Uri.EscapeDataString(input.Key)}={Escaped(input.Value)}"),
        ];
        using HttpRequestMessage request = new(
            new HttpMethod(template.HttpMethod),
            query.IsEmpty ? url : $"{url}?{string.Join('&', query)}"
        );
        request.Content = template.BodyField is { } field && inputs.GetValueOrDefault(field) is { } body
            ? JsonContent.Create(body, body.GetType(), options: Json)
            : null;
        foreach (KeyValuePair<string, object?> header in given.Where(input => headers.Contains(input.Key)))
        {
            _ = request.Headers.TryAddWithoutValidation(header.Key, $"{header.Value}");
        }

        using HttpResponseMessage response = await this._http.SendAsync(request, cancellationToken);
        return response.IsSuccessStatusCode
            ? await response.Content.ReadFromJsonAsync<T>(Json, cancellationToken)
                ?? throw new RefusedException("Empty", $"The tool {tool} sent nothing back.")
            : throw await Refusal(response, cancellationToken);
    }

    /// <summary>An input as it goes in an address: true and false as JSON writes them, the rest as text.</summary>
    private static string Escaped(object? value) =>
        Uri.EscapeDataString(value is bool flag ? (flag ? "true" : "false") : $"{value}");

    /// <summary>The refusal, as the server words it: its title (Conflict, TooBusy...), and why.</summary>
    private static async Task<RefusedException> Refusal(
        HttpResponseMessage response,
        CancellationToken cancellationToken
    )
    {
        string text = await response.Content.ReadAsStringAsync(cancellationToken);
        Problem? problem = text.StartsWith('{') ? JsonSerializer.Deserialize<Problem>(text, Json) : null;
        return new RefusedException(
            problem?.Title ?? (response.StatusCode == HttpStatusCode.TooManyRequests ? "TooBusy" : "Unknown"),
            $"{problem?.Title}: {problem?.Detail ?? text}"
        );
    }

    /// <summary>The parts of a manual this client reads.</summary>
    private sealed record Manual(string UtcpVersion, ImmutableList<Tool> Tools);

    private sealed record Tool(string Name, CallTemplate ToolCallTemplate);

    private sealed record CallTemplate(
        string CallTemplateType,
        string HttpMethod,
        string Url,
        string? BodyField,
        ImmutableList<string>? HeaderFields
    );

    private sealed record Problem(string? Title, string? Detail);
}

/// <summary>A call the server refused: which way (<see cref="Refusal"/>: Conflict, TooBusy...), and why.</summary>
internal sealed class RefusedException(string refusal, string message) : Exception(message)
{
    public string Refusal { get; } = refusal;
}
