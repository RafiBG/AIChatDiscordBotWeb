using AIChatDiscordBotWeb.Models;
using AIChatDiscordBotWeb.Services;
using Microsoft.AspNetCore.Mvc;

namespace AIChatDiscordBotWeb.Controllers
{
    public class ConfigController : Controller
    {
        private readonly EnvService _envService;
        public ConfigController(EnvService envService)
        {
            _envService = envService;
        }

        public async Task<IActionResult> Index()
        {
            var config = await _envService.LoadAsync();
            return View(config);
        }

        [HttpPost]
        public async Task<IActionResult> Save(EnvConfig config, string AllowedChannelIdsInput, string AllowedGroupChannelIdsInput)
        {
            // Convert comma-separated input string into List<ulong>
            if (!string.IsNullOrWhiteSpace(AllowedChannelIdsInput))
            {
                config.ALLOWED_CHANNEL_IDS = AllowedChannelIdsInput
                    .Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(v => ulong.TryParse(v.Trim(), out var id) ? id : 0)
                    .Where(id => id != 0)
                    .ToList();
            }

            if (!string.IsNullOrWhiteSpace(AllowedGroupChannelIdsInput))
            {
                config.ALLOWED_GROUP_CHANNEL_IDS = AllowedGroupChannelIdsInput
                    .Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(v => ulong.TryParse(v.Trim(), out var id) ? id : 0)
                    .Where(id => id != 0)
                    .ToList();
            }
            await _envService.SaveAsync(config);
            return RedirectToAction("Index");
        }
    }
}