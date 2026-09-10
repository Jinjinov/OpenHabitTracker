using Microsoft.Extensions.DependencyInjection;
using OpenHabitTracker.App;
using OpenHabitTracker.Blazor.Web.ApiClient;
using OpenHabitTracker.Data;
using OpenHabitTracker.EntityFrameworkCore;

namespace OpenHabitTracker.UnitTests.ApiClient;

// The remote client is built by a factory delegate, which the container cannot inspect for cycles,
// so resolving the graph is the only thing that proves it terminates.
[TestFixture]
public class DataAccessWiringTests
{
    private ServiceProvider _serviceProvider = null!;

    [SetUp]
    public void SetUp()
    {
        ServiceCollection services = new();

        services.AddServices();
        services.AddDataAccess(Path.Combine(Path.GetTempPath(), "OpenHabitTrackerWiringTests.db"));
        services.AddHttpClients();

        // Not ValidateOnBuild: that walks every registration, including the ones each host adds for
        // itself, which is a different check than the one these tests are for.
        _serviceProvider = services.BuildServiceProvider(validateScopes: true);
    }

    [TearDown]
    public void TearDown()
    {
        _serviceProvider.Dispose();
    }

    [Test]
    public void Resolve_TheWholeDataAccessGraph_Terminates()
    {
        using IServiceScope scope = _serviceProvider.CreateScope();

        Assert.Multiple(() =>
        {
            Assert.That(scope.ServiceProvider.GetRequiredService<DataAccessClient>(), Is.Not.Null);
            Assert.That(scope.ServiceProvider.GetRequiredService<ITokenRefresher>(), Is.Not.Null);
            Assert.That(scope.ServiceProvider.GetRequiredService<ClientState>(), Is.Not.Null);
        });
    }

    [Test]
    public void Resolve_TheDataAccessCollection_CarriesLocalAndRemote()
    {
        using IServiceScope scope = _serviceProvider.CreateScope();

        IEnumerable<IDataAccess> dataAccess = scope.ServiceProvider.GetServices<IDataAccess>();

        Assert.That(dataAccess.Select(x => x.DataLocation), Is.EquivalentTo(new[] { DataLocation.Local, DataLocation.Remote }));
    }

    [Test]
    public void Resolve_TheKeyedLocalDataAccess_IsTheSameInstanceAsTheUnkeyedOne()
    {
        using IServiceScope scope = _serviceProvider.CreateScope();

        IDataAccess keyed = scope.ServiceProvider.GetRequiredKeyedService<IDataAccess>(DataAccessKeys.Local);
        IDataAccess unkeyed = scope.ServiceProvider.GetServices<IDataAccess>().Single(x => x.DataLocation == DataLocation.Local);

        Assert.That(keyed, Is.SameAs(unkeyed));
    }
}
