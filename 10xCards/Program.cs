using _10xCards.Components;
using _10xCards.Database.Context;
using _10xCards.Database.Entities;
using _10xCards.Endpoints;
using _10xCards.Extensions;
using _10xCards.Middleware;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;

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

		// Configure ASP.NET Core Identity
		builder.Services.AddIdentity<User, IdentityRole<Guid>>(options =>
		{
			// Password settings
			options.Password.RequireDigit = true;
			options.Password.RequireLowercase = true;
			options.Password.RequireUppercase = true;
			options.Password.RequireNonAlphanumeric = true;
			options.Password.RequiredLength = 8;
			options.Password.RequiredUniqueChars = 1;

			// User settings
			options.User.RequireUniqueEmail = true;

			// Sign-in settings
			options.SignIn.RequireConfirmedEmail = false;
		})
		.AddEntityFrameworkStores<ApplicationDbContext>()
		.AddDefaultTokenProviders();

		// Configure JWT Authentication
		var jwtSettings = builder.Configuration.GetSection("JwtSettings");
		var secretKey = jwtSettings["SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey is not configured");
		var key = Encoding.UTF8.GetBytes(secretKey);

		builder.Services.AddAuthentication(options =>
		{
			options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
			options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
		})
		.AddJwtBearer(options =>
		{
			options.TokenValidationParameters = new TokenValidationParameters
			{
				ValidateIssuer = true,
				ValidateAudience = true,
				ValidateLifetime = true,
				ValidateIssuerSigningKey = true,
				ValidIssuer = jwtSettings["Issuer"],
				ValidAudience = jwtSettings["Audience"],
				IssuerSigningKey = new SymmetricSecurityKey(key),
				ClockSkew = TimeSpan.Zero
			};
		});

		builder.Services.AddAuthorization();

	// Register services
	builder.Services.AddScoped<_10xCards.Services.IAuthService, _10xCards.Services.AuthService>();
	builder.Services.AddScoped<_10xCards.Services.IFlashcardService, _10xCards.Services.FlashcardService>();
	builder.Services.AddScoped<_10xCards.Services.IGenerationService, _10xCards.Services.GenerationService>();

	// Register OpenRouter AI service with HttpClient
	builder.Services.AddHttpClient<_10xCards.Services.IOpenRouterService, _10xCards.Services.OpenRouterService>(client => {
		client.BaseAddress = new Uri("https://openrouter.ai/api/v1/");
		client.Timeout = TimeSpan.FromSeconds(30);
	});

	var app = builder.Build();

		app.MigrateDatabase();

		// Global exception handler middleware
		app.UseMiddleware<GlobalExceptionHandlerMiddleware>();

        if (!app.Environment.IsDevelopment())
        {
            app.UseHsts();
        }
		

		app.UseHttpsRedirection();

		app.UseAuthentication();
		app.UseAuthorization();

        app.UseAntiforgery();

	// Map API endpoints
	app.MapAuthEndpoints();
	app.MapFlashcardEndpoints();
	app.MapGenerationEndpoints();

    app.MapStaticAssets();
        app.MapRazorComponents<App>()
            .AddInteractiveServerRenderMode();

        app.Run();
    }
}
