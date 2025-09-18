using AIChatDiscordBotWeb.Models;

namespace AIChatDiscordBotWeb.Services
{
    public class EnvService
    {
        private readonly string _filePath = Path.Combine(Directory.GetCurrentDirectory(), ".env");

        // Load from .env
        public async Task<EnvConfig> LoadAsync()
        {
            var config = new EnvConfig();
            if (!File.Exists(_filePath))
            {
                return config; // return if not file yet
            }

            var lines = await File.ReadAllLinesAsync(_filePath);
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                var parts = line.Split('=', 2); // split into key/value
                if (parts.Length != 2)
                {
                    continue;
                }

                var key = parts[0].Trim();
                var value = parts[1].Trim();

                switch (key)
                {
                    case "BOT_TOKEN": config.BOT_TOKEN = value; break;
                    case "LOCAL_HOST": config.LOCAL_HOST = Convert.ToInt32(value); break;
                    case "MODEL": config.MODEL = value; break;
                    case "ALLOWED_CHANNEL_IDS":
                        config.ALLOWED_CHANNEL_IDS = value
                            .Split(',', StringSplitOptions.RemoveEmptyEntries)
                            .Select(v => ulong.TryParse(v.Trim(), out var id) ? id : 0)
                            .Where(id => id != 0)
                            .ToList();
                        break;
                        // Decode \n into real newline
                    case "SYSTEM_MESSAGE": config.SYSTEM_MESSAGE = 
                            value.Replace("\\n", Environment.NewLine); break;
                }
            }
            return config;
        }

        public async Task SaveAsync(EnvConfig config)
        {
            var lines = new List<string>
            {
                $"BOT_TOKEN={config.BOT_TOKEN}",
                $"LOCAL_HOST={config.LOCAL_HOST}",
                $"MODEL={config.MODEL}",
                $"ALLOWED_CHANNEL_IDS={string.Join(",", config.ALLOWED_CHANNEL_IDS)}",
                $"SYSTEM_MESSAGE={config.SYSTEM_MESSAGE?.Replace(Environment.NewLine, "\\n")}"
            };

            await File.WriteAllLinesAsync(_filePath, lines);
        }
    }
}
