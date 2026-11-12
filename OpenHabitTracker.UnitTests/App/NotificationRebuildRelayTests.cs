using OpenHabitTracker.App;

namespace OpenHabitTracker.UnitTests.App;

[TestFixture]
public class NotificationRebuildRelayTests
{
    [Test]
    public async Task TryRebuild_WithNoUi_ReturnsFalse()
    {
        NotificationRebuildRelay relay = new();

        Assert.That(await relay.TryRebuild(), Is.False);
    }

    [Test]
    public async Task TryRebuild_RunsTheRebuildTheUiSet()
    {
        NotificationRebuildRelay relay = new();
        int rebuilds = 0;

        relay.SetRebuildAction(() =>
        {
            rebuilds++;
            return Task.CompletedTask;
        });

        bool rebuilt = await relay.TryRebuild();

        Assert.Multiple(() =>
        {
            Assert.That(rebuilt, Is.True);
            Assert.That(rebuilds, Is.EqualTo(1));
        });
    }
}
