using System.ComponentModel.DataAnnotations;

namespace Net.NowhereAtAll.Zaturnava.Bots;

/// <summary>
/// A service a bot may ask which of a few moves to make: an AI, say. It answers with the index of the
/// choice it advises, or null when it cannot answer (the service down, refusing, or making no sense), so
/// that the bot decides alone; it should not throw for any of those.
/// </summary>
public interface IMoveAdvising
{
    Task<int?> Advise([Required] MoveQuestion question, CancellationToken cancellationToken);
}
