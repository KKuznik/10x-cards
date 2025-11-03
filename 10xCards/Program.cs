using _10xCards.Components;
using _10xCards.Database.Context;
using _10xCards.Extensions;
using Microsoft.EntityFrameworkCore;

namespace _10xCards;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddRazorComponents()
            .AddInteractiveServerComponents();

		builder.Services.AddDbContext<ApplicationDbContext>(
			options => options
				.UseNpgsql(builder.Configuration.GetConnectionString("Database"))
				.UseSnakeCaseNamingConvention());

		var app = builder.Build();

		app.MigrateDatabase();

        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Error");
            app.UseHsts();
        }
		

		app.UseHttpsRedirection();

        app.UseAntiforgery();

        app.MapStaticAssets();
        app.MapRazorComponents<App>()
            .AddInteractiveServerRenderMode();

        app.Run();
    }
}
