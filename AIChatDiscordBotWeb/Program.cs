using AIChatDiscordBotWeb.Models;
using AIChatDiscordBotWeb.Services;

var builder = WebApplication.CreateBuilder(args);

// Register EnvService
builder.Services.AddSingleton<EnvService>();

// Register EnvConfig (loaded once from .env)
builder.Services.AddSingleton<EnvConfig>(sp =>
{
    var envService = sp.GetRequiredService<EnvService>();
    return envService.LoadAsync().Result;
});

// Register OllamaService (depends on EnvConfig)
builder.Services.AddSingleton<OllamaService>();

builder.Services.AddSingleton<ChatMemoryService>();

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

// Start browser after server is ready
//app.Lifetime.ApplicationStarted.Register(() =>
//{
//    try
//    {
//        Process.Start(new ProcessStartInfo
//        {
//            FileName = "http://localhost:5000",
//            UseShellExecute = true, // Opens default browser
//        });
//    }
//    catch { } // Ignore if browser fails
//});

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
