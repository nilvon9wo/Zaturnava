using System.Collections.Immutable;
using System.Text.Json;
using Net.NowhereAtAll.Zaturnava.Engine.Actions;
using Net.NowhereAtAll.Zaturnava.Engine.Rules;
using Net.NowhereAtAll.Zaturnava.Engine.Seats;

namespace Net.NowhereAtAll.Zaturnava.Games.Chaturaji2027.Test;

/// <summary>
/// Games the bots played, kept as their histories, replayed through today's rules: every game a server keeps is
/// its actions, replayed, so a change to the shared engine (for Grand Legions, say) that plays Chaturaji 2027
/// differently would lose games and standings already played. These must end as they ended.
/// </summary>
public sealed class RecordedGamesTest
{
    private static readonly ImmutableList<JsonElement> Games =
    [
        .. File.ReadLines(Path.Combine(AppContext.BaseDirectory, "RecordedGames.jsonl"))
            .Select(line => JsonDocument.Parse(line).RootElement),
    ];

    public static TheoryData<int> Numbers => [.. Enumerable.Range(0, Games.Count)];

    [Theory]
    [MemberData(nameof(Numbers))]
    public void Apply_WhenARecordedGameIsReplayed_EndsAsItEnded(int number)
    {
        // Arrange
        JsonElement game = Games[number];
        JsonElement outcome = game.GetProperty("outcome");
        Chaturaji2027Rules rules = new();

        // Act
        GameState state = game.GetProperty("actions")
            .EnumerateArray()
            .Aggregate(
                rules.CreateInitialState(),
                (reached, action) => rules.Apply(reached, rules.Parse(DescriptorOf(action))).State
            );

        // Assert
        Assert.Equal(
            [.. game.GetProperty("seats").EnumerateArray().Select(seat => seat.GetProperty("score").GetInt32())],
            state.Seats.Select(seat => state.Scores.GetValueOrDefault(seat.Id)).ToList()
        );
        Assert.Equal(outcome.GetProperty("ending").GetString(), state.Outcome?.Ending);
        Assert.Equal(
            [.. outcome.GetProperty("winners").EnumerateArray().Select(winner => new SeatId(winner.GetInt32()))],
            state.Outcome?.Winners
        );
    }

    private static ActionDescriptor DescriptorOf(JsonElement action) =>
        new(
            action.GetProperty("kind").GetString()!,
            action.GetProperty("arguments")
                .EnumerateObject()
                .ToImmutableSortedDictionary(argument => argument.Name, argument => argument.Value.GetString()!)
        );
}
