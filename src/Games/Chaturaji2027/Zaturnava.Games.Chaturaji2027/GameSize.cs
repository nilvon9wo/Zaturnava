namespace Net.NowhereAtAll.Zaturnava.Games.Chaturaji2027;

/// <summary>
/// One size of a game played at several: the game's id and name, whatever the size, and the size ("small"); and
/// whether the game is a preview, not yet ready for the public.
/// </summary>
public sealed record GameSize(string Id, string Name, string Size, bool IsPreview = false);
