using System.Collections.Immutable;

namespace Net.NowhereAtAll.Zaturnava.Contracts;

/// <summary>
/// What a player's profile shows anyone from now on (<see cref="GameRoutes.MyProfile"/>): a description of their own,
/// at most <see cref="MaximumDescriptionLength"/> characters, and cut if longer; and which of the services they sign
/// in with it names (<paramref name="Shown"/>), others' being left out. None shown, none named.
/// </summary>
public sealed record DescribeRequest(string? Description, ImmutableList<string>? Shown)
{
    public const int MaximumDescriptionLength = 500;
}
