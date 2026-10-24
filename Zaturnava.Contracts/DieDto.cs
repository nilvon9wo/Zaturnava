namespace Net.NowhereAtAll.Zaturnava.Contracts;

/// <summary>A die as it came up: its <see cref="Value"/>, and what that lets its roller do ("Horse").</summary>
public sealed record DieDto(int Value, string Meaning);
