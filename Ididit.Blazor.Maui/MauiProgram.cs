using Ididit.Data;
using Ididit.EntityFrameworkCore;
using Ididit.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Ididit.Blazor.Maui;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        MauiAppBuilder builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
            });

        builder.Services.AddMauiBlazorWebView();

#if DEBUG
		builder.Services.AddBlazorWebViewDeveloperTools();
		builder.Logging.AddDebug();
#endif

        builder.Services.AddScoped<HabitService>();
        builder.Services.AddScoped<NoteService>();
        builder.Services.AddScoped<TaskService>();
        builder.Services.AddScoped<TrashService>();

        builder.Services.AddScoped<IDataAccess, DataAccess>();

        builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseSqlite("Data Source=Ididit.db"));

        return builder.Build();
    }
}
