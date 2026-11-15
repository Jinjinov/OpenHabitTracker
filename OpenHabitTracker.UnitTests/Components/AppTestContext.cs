using Bunit;
using GTour.Abstractions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using NSubstitute;
using OpenHabitTracker.App;
using OpenHabitTracker.Backup;
using OpenHabitTracker.Blazor;
using OpenHabitTracker.Data;
using OpenHabitTracker.Data.Models;
using OpenHabitTracker.Services;
using LocalDataAccess = OpenHabitTracker.EntityFrameworkCore.DataAccess;
using LocalDbContext = OpenHabitTracker.EntityFrameworkCore.ApplicationDbContext;

namespace OpenHabitTracker.UnitTests.Components;

// Renders components over the real services and a real SQLite store, so a test sees what the
// filtering and the saving behind a page actually do. Only the browser's side is substituted.
internal sealed class AppTestContext : IDisposable
{
    private readonly SqliteConnection _connection = new("DataSource=:memory:");

    public BunitContext Ctx { get; } = new();

    public IJsInterop JsInterop { get; } = Substitute.For<IJsInterop>();

    public IGTourService GTourService { get; } = Substitute.For<IGTourService>();

    public IAuthFragment AuthFragment { get; } = Substitute.For<IAuthFragment>();

    public OpenHabitTracker.Localization.ICultureTarget CultureTarget { get; } = Substitute.For<OpenHabitTracker.Localization.ICultureTarget>();

    // Registered after the null host, so it is the one resolved; a test sets its answers before Start.
    public INotifications Notifications { get; } = Substitute.For<INotifications>();

    private sealed class SharedConnectionFactory(DbContextOptions<LocalDbContext> options) : IDbContextFactory<LocalDbContext>
    {
        public LocalDbContext CreateDbContext() => new(options);
    }

    public AppTestContext()
    {
        _connection.Open();

        DbContextOptions<LocalDbContext> options = new DbContextOptionsBuilder<LocalDbContext>().UseSqlite(_connection).Options;

        Ctx.JSInterop.Mode = JSRuntimeMode.Loose;

        Ctx.Services.AddLogging();
        Ctx.Services.AddServices();
        Ctx.Services.AddBackup();
        Ctx.Services.AddSingleton<IDbContextFactory<LocalDbContext>>(new SharedConnectionFactory(options));
        Ctx.Services.AddScoped<LocalDataAccess>();
        Ctx.Services.AddScoped<IDataAccess>(provider => provider.GetRequiredService<LocalDataAccess>());
        Ctx.Services.AddSingleton(Notifications);
        Ctx.Services.AddSingleton(Substitute.For<IAppReview>());
        Ctx.Services.AddSingleton(JsInterop);
        Ctx.Services.AddSingleton(GTourService);
        Ctx.Services.AddSingleton(Substitute.For<GTour.Abstractions.JsInterop.IJsInteropPopper>());
        Ctx.Services.AddSingleton(Substitute.For<GTour.Abstractions.JsInterop.IJsInteropCommon>());
        Ctx.Services.AddSingleton(Substitute.For<ILinkAttributeService>());
        Ctx.Services.AddSingleton(AuthFragment);

        // A language change would otherwise set the culture of the whole test process.
        Ctx.Services.AddSingleton(CultureTarget);

        // The key is the text, so a test can find what it renders without a language file.
        IStringLocalizer loc = Substitute.For<IStringLocalizer>();
        loc[Arg.Any<string>()].Returns(callInfo => new LocalizedString(callInfo.RequiredArg<string>(), callInfo.RequiredArg<string>()));
        loc[Arg.Any<string>(), Arg.Any<object[]>()].Returns(callInfo => new LocalizedString(callInfo.ArgAt<string>(0), string.Format(callInfo.ArgAt<string>(0), callInfo.ArgAt<object[]>(1))));
        Ctx.Services.AddSingleton(loc);
    }

    public T Get<T>() where T : notnull => Ctx.Services.GetRequiredService<T>();

    public IDataAccess Store => Get<IDataAccess>();

    public ClientState ClientState => Get<ClientState>();

    // A first run: the store is migrated and the default settings are created, with the help text
    // and the guided tour off, since the tour needs a browser.
    public async Task<ClientState> Start(Action<SettingsModel>? configure = null)
    {
        await Store.Initialize();

        ClientState clientState = ClientState;

        await clientState.LoadSettings();
        await clientState.LoadUsers();

        clientState.Settings.ShowHelp = false;
        configure?.Invoke(clientState.Settings);

        return clientState;
    }

    // RemoteDataSync, which ErrorComponent injects, can only be disposed asynchronously.
    public void Dispose()
    {
        Ctx.DisposeAsync().AsTask().GetAwaiter().GetResult();
        _connection.Dispose();
    }
}
