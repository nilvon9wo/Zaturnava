namespace Net.NowhereAtAll.Zaturnava.Engine.Rules;

/// <summary>
/// What a game's creator chose of its terrain: the bits of <see cref="Mask"/> set as in <see cref="Value"/>, the
/// rest dealt at random.
/// </summary>
public sealed record TerrainChoice(int Mask, int Value)
{
    /// <summary>The terrain <paramref name="dealt"/>, its chosen bits set as chosen.</summary>
    public int Applied(int dealt) => (dealt & ~this.Mask) | (this.Value & this.Mask);
}
