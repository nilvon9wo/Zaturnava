using System.Globalization;

namespace Net.NowhereAtAll.Zaturnava.Presentation;

/// <summary>A die's value as a die face (⚀ to ⚅), or as a number for values no six-sided die shows.</summary>
public static class DieFaces
{
    private const int FirstFace = 0x2680;
    private const int Sides = 6;

    public static string FaceOf(int value) =>
        value is >= 1 and <= Sides
            ? char.ConvertFromUtf32(FirstFace + value - 1)
            : value.ToString(CultureInfo.InvariantCulture);
}
