using CryptoPortfolio.Services;
using CryptoPortfolioUI.Components;
using CryptoPortfolioUI.Services;
using CryptoPortfolioUI.Settings;

internal class Program
{
    private static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.
        builder.Services.AddRazorComponents()
            .AddInteractiveServerComponents();

        var portfolioSettingsSection = builder.Configuration.GetSection("PortfolioSettings");
        builder.Services.Configure<PortfolioSettings>(portfolioSettingsSection);

        builder.Services.AddSingleton<PortfolioService>();

        var logPath = Path.Combine(builder.Environment.ContentRootPath, portfolioSettingsSection[nameof(PortfolioSettings.LogFileName)]!);
        Logger.AddFileLogging(logPath);

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Error", createScopeForErrors: true);
            // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
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