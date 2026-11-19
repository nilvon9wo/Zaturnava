using System.Collections.Immutable;

namespace Net.NowhereAtAll.Zaturnava.Web.Test;

public sealed class ListViewTest
{
    [Fact]
    public void Text_WhenParsedBack_IsTheSameViewWithAValueTypedWithCommasAndAmpersandsKept()
    {
        // Arrange - a choice left as it starts is not kept
        ListView view = new(
            ImmutableDictionary<string, string>.Empty.Add("players", "Bob, Jo* & open").Add("people", string.Empty),
            "moves",
            true
        );

        // Act
        ListView? parsed = ListView.Parse(view.Text);

        // Assert
        Assert.Equal(
            ("desc=1&players=Bob%2C%20Jo%2A%20%26%20open&sort=moves", "Bob, Jo* & open", null, "moves", true),
            (view.Text, parsed?["players"], parsed?["people"], parsed?.Sort, parsed?.IsDescending)
        );
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("=nameless&alone")]
    public void Parse_WhenNothingIsKeptThatItCanRead_IsNone(string? text)
    {
        // Act
        ListView? view = ListView.Parse(text);

        // Assert
        Assert.Null(view);
    }

    [Fact]
    public void Parse_WhenNoColumnIsKept_SortsByNone()
    {
        // Act
        ListView? view = ListView.Parse("state=waiting&state=mine");

        // Assert
        Assert.Equal(("waiting", string.Empty, false), (view?["state"], view?.Sort, view?.IsDescending));
    }
}
