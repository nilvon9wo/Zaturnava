namespace Net.NowhereAtAll.Zaturnava.Contracts;

/// <summary>
/// A die as it came up: its <see cref="Value"/>, what that lets its roller do ("Horse"), and whether
/// anything it lets move may move now (<see cref="IsUsable"/>).
/// </summary>
public sealed record DieDto(int Value, string Meaning, bool IsUsable = true);
