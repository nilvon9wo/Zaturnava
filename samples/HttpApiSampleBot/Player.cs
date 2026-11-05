using System.Net;

namespace HttpApiSampleBot;

/// <summary>
/// Plays one seat of one game to its end, as docs/bots/http-api.md#playing describes: read the game; if it is
/// the bot's decision, choose one of the legal actions; if a server bot's, let it move; if a person's, wait.
/// </summary>
internal sealed class Player(GameApi api, IChoosing mind, TimeSpan pause, TextWriter log)
{
    /// <summary>How long to wait when the server is too busy (429): its limits are counted by the minute.</summary>
    private static readonly TimeSpan TooBusyPause = TimeSpan.FromMinutes(1);

    private readonly GameApi _api = api;
    private readonly IChoosing _mind = mind;
    private readonly TimeSpan _pause = pause;
    private readonly TextWriter _log = log;

    /// <summary>Plays the seat whose token is <paramref name="seatToken"/> until the game is over.</summary>
    public async Task<Outcome> Play(string gameId, int seat, string seatToken, CancellationToken cancellationToken)
    {
        while (true)
        {
            if (await this.Step(gameId, seat, seatToken, cancellationToken) is { } outcome)
            {
                return outcome;
            }

            // Be a good guest: a game needs no more than a few requests a second.
            await Task.Delay(this._pause, cancellationToken);
        }
    }

    /// <summary>
    /// One step of the game: read it; then the bot's own decision, a server bot's, or none, while a person (or no one
    /// yet) decides. The outcome, once the game is over. A request the game outran (409, someone else moved it
    /// meanwhile) is simply tried again next step; so is any, after a minute, when the server is too busy (429).
    /// </summary>
    private async Task<Outcome?> Step(string gameId, int seat, string seatToken, CancellationToken cancellationToken)
    {
        try
        {
            GameView view = await this._api.Get(gameId, cancellationToken);
            await this.Decide(view, seat, seatToken, cancellationToken);
            return view.Outcome;
        }
        catch (RefusedException refused) when (refused.Status == HttpStatusCode.Conflict)
        {
            await this._log.WriteLineAsync($"The game moved on meanwhile ({refused.Message}); reading it again.");
        }
        catch (RefusedException refused) when (refused.Status == HttpStatusCode.TooManyRequests)
        {
            await this._log.WriteLineAsync("The server is busy: waiting a minute.");
            await Task.Delay(TooBusyPause, cancellationToken);
        }

        return null;
    }

    private async Task Decide(GameView view, int seat, string seatToken, CancellationToken cancellationToken)
    {
        if (view.DecidingSeat == seat && !view.LegalActions.IsEmpty)
        {
            GameAction action = this._mind.Choose(view, seat);
            await this._log.WriteLineAsync($"{view.Seats[seat].Name}: {action.Label ?? Described(action)}");
            _ = await this._api.Act(view.Id, action, seatToken, cancellationToken);
        }
        else if (view.DecidingSeat is { } other && view.Seats[other].Controller == "bot")
        {
            _ = await this._api.Advance(view.Id, view.Version, cancellationToken);
        }
    }

    /// <summary>"move from=8 to=16": the action's kind and arguments, but for its actor.</summary>
    private static string Described(GameAction action) =>
        string.Join(
            ' ',
            action.Arguments
                .Where(argument => argument.Key != "actor")
                .Select(argument => $"{argument.Key}={argument.Value}")
                .Prepend(action.Kind)
        );
}
