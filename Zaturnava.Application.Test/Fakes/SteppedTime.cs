namespace Net.NowhereAtAll.Zaturnava.Application.Test.Fakes;

/// <summary>A clock that stands still until a test moves it on.</summary>
internal sealed class SteppedTime : TimeProvider
{
    public DateTimeOffset Now { get; private set; } = new(2026, 10, 25, 12, 0, 0, TimeSpan.Zero);

    public override DateTimeOffset GetUtcNow() => this.Now;

    public void Pass(TimeSpan time) => this.Now += time;
}
