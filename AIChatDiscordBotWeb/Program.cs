using AIChatDiscordBotWeb.Models;
using AIChatDiscordBotWeb.Services;
using AIChatDiscordBotWeb.SlashCommands;
using Microsoft.AspNetCore.Hosting.Server.Features;
using System.Diagnostics;

var builder = WebApplication.CreateBuilder(args);

// Register EnvService
builder.Services.AddSingleton<EnvService>();

// Register EnvConfig (loaded once from .env)
builder.Services.AddSingleton<EnvConfig>(sp =>
{
    var envService = sp.GetRequiredService<EnvService>();
    return envService.LoadAsync().Result;
});

// Register SemanticService Ollama (depends on EnvConfig)
builder.Services.AddSingleton<AIConnectionService>();

builder.Services.AddSingleton<ChatMemoryService>();
builder.Services.AddSingleton<SlackMemoryService>();
builder.Services.AddSingleton<KernelMemoryService>();

// Register the ai group chat command
builder.Services.AddSingleton<AIGroupChat>();

// Register BotService (needs EnvService + IServiceProvider)
builder.Services.AddSingleton<BotService>(sp =>
{
    var envService = sp.GetRequiredService<EnvService>();
    return new BotService(envService, sp);
});

// Add MVC controllers
builder.Services.AddControllersWithViews();

var app = builder.Build();

// Open the browser page
//app.MapGet("/", () => "");

//// Start browser after server is ready
if (!app.Environment.IsDevelopment())
{
    app.Lifetime.ApplicationStarted.Register(() =>
    {
        var server = app.Services.GetRequiredService<
                         Microsoft.AspNetCore.Hosting.Server.IServer>();
        var addresses = server.Features.Get<IServerAddressesFeature>();

        if (addresses != null)
        {
            var url = addresses.Addresses.FirstOrDefault();
            if (!string.IsNullOrEmpty(url))
            {
                try
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = url,
                        UseShellExecute = true
                    });
                }
                catch { } // Ignore if browser launch fails
            }
        }
        app.UseExceptionHandler("/Home/Error");
        // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
        app.UseHsts();
        // Only use HTTPS in dev mode
        app.UseHttpsRedirection();
    });
}
app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();