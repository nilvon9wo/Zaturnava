using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Net.NowhereAtAll.Zaturnava.Bots.OpenAiCompatible;

/// <summary>
/// Advice from a service speaking the OpenAI chat-completions protocol, asking <paramref name="model"/>
/// through <paramref name="http"/>, which comes with the service's address and key. The question goes in
/// words, the choices numbered from 1; the advice is the first number in the reply, once any reasoning the
/// model shows (between think tags) is set aside. It answers nothing, rather than fail, when the service is
/// down or refuses, or replies with no number of a choice.
/// </summary>
public sealed partial class ChatAdvisor(HttpClient http, string model) : IMoveAdvising
{
    /// <summary>Where a chat completion is asked for, from the service's address.</summary>
    public const string Endpoint = "chat/completions";

    private const string Instructions =
        "You advise a player of a board game. You are told the game as it stands and the moves the player is "
        + "choosing among, numbered, the one the player's own engine likes best first. Answer with the number "
        + "of the move you would make, and nothing else.";

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _http = http;
    private readonly string _model = model;

    public async Task<int?> Advise([Required] MoveQuestion question, CancellationToken cancellationToken)
    {
        try
        {
            using HttpResponseMessage response =
                await this._http.PostAsJsonAsync(Endpoint, this.RequestFor(question), Json, cancellationToken);
            _ = response.EnsureSuccessStatusCode();
            ChatReply? reply = await response.Content.ReadFromJsonAsync<ChatReply>(Json, cancellationToken);
            return ChoiceIn(reply?.Choices?.FirstOrDefault()?.Message?.Content, question.Choices.Count);
        }
        catch (HttpRequestException)
        {
            return null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private ChatRequest RequestFor(MoveQuestion question) =>
        new(
            this._model,
            [
                new ChatMessage("system", Instructions),
                new ChatMessage(
                    "user",
                    string.Join(
                        '\n',
                        [
                            question.Situation,
                            "The moves:",
                            .. question.Choices.Select((choice, index) =>
                                $"{(index + 1).ToString(CultureInfo.InvariantCulture)}. {choice}"
                            ),
                        ]
                    )
                ),
            ],
            0
        );

    /// <summary>The index of the choice the reply's first number names; null if it names none.</summary>
    private static int? ChoiceIn(string? content, int count) =>
        ChoiceNamed(NumberPattern().Match(Answered(content)), count);

    private static int? ChoiceNamed(Match number, int count) =>
        number.Success
            ? IndexOf(int.Parse(number.Value, CultureInfo.InvariantCulture), count)
            : null;

    /// <summary>The reply without the reasoning it shows.</summary>
    private static string Answered(string? content) => ThinkingPattern().Replace(content ?? string.Empty, string.Empty);

    private static int? IndexOf(int number, int count) =>
        number >= 1 && number <= count
            ? number - 1
            : null;

    [GeneratedRegex(@"\d{1,4}")]
    private static partial Regex NumberPattern();

    [GeneratedRegex("<think>.*?</think>", RegexOptions.Singleline)]
    private static partial Regex ThinkingPattern();
}

/// <summary>What is asked of the service: the model, the conversation so far, and how freely to answer.</summary>
internal sealed record ChatRequest(string Model, ImmutableList<ChatMessage> Messages, double Temperature);

internal sealed record ChatMessage(string Role, string? Content);

/// <summary>What the service answers, as far as the advisor reads it: each may be missing.</summary>
internal sealed record ChatReply(ImmutableList<ChatChoice>? Choices);

internal sealed record ChatChoice(ChatMessage? Message);
