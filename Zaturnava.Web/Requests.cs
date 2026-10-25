using Net.NowhereAtAll.Zaturnava.Contracts;

namespace Net.NowhereAtAll.Zaturnava.Web;

/// <summary>
/// A request to the game server, from a page: a refusal, or a server that cannot be reached or does
/// not answer in time (a free host asleep or gone), is something to tell the player, not a crash.
/// </summary>
internal static class Requests
{
    /// <summary>Makes the request; returns what to tell the player if it failed, or null.</summary>
    public static async Task<string?> Attempt(Func<Task> request)
    {
        try
        {
            await request();
            return null;
        }
        catch (Exception exception)
            when (exception is GameRequestRefusedException or HttpRequestException or TaskCanceledException)
        {
            return exception.Message;
        }
    }
}
