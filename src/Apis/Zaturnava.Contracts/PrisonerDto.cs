namespace Net.NowhereAtAll.Zaturnava.Contracts;

/// <summary>
/// A captured piece: its kind, its name for people, and the seat it belongs to; and whether it may be given back
/// now (one its own team took waits until every seat has had a turn).
/// </summary>
public sealed record PrisonerDto(int Id, string Kind, string Name, int Owner)
{
    public bool MayGoBack { get; init; } = true;
}
