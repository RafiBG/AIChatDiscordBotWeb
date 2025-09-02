using AIChatDiscordBotWeb.Models;
using AIChatDiscordBotWeb.Services;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace AIChatDiscordBotWeb.Controllers
{
    public class HomeController : Controller
    {
        private readonly BotService _botService;

        // Inject bot service (registered in Program.cs)
        public HomeController(BotService botService)
        {
            _botService = botService;
        }

        public IActionResult Index()
        {
            ViewBag.IsRunning = _botService.IsRunning();
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Start()
        {
            await _botService.StartAsync();
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> Stop()
        {
            await _botService.StopAsync();
            return RedirectToAction("Index");
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
