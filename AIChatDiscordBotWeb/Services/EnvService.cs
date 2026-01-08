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
                    case "LOCAL_HOST": config.LOCAL_HOST = value; break;
                    case "MODEL": config.MODEL = value; break;
                    case "API_KEY": config.API_KEY = value; break;
                    case "ALLOWED_CHANNEL_IDS":
                        config.ALLOWED_CHANNEL_IDS = value
                            .Split(',', StringSplitOptions.RemoveEmptyEntries)
                            .Select(v => ulong.TryParse(v.Trim(), out var id) ? id : 0)
                            .Where(id => id != 0)
                            .ToList();
                        break;
                    case "ALLOWED_GROUP_CHANNEL_IDS":
                        config.ALLOWED_GROUP_CHANNEL_IDS = value
                            .Split(',', StringSplitOptions.RemoveEmptyEntries)
                            .Select(v => ulong.TryParse(v.Trim(), out var id) ? id : 0)
                            .Where(id => id != 0)
                            .ToList();
                        break;
                    // Decode \n into real newline
                    case "SYSTEM_MESSAGE": config.SYSTEM_MESSAGE = 
                            value.Replace("\\n", Environment.NewLine); break;
                    case "SERPER_API_KEY": config.SERPER_API_KEY = value; break;
                    case "COMFYUI_API": config.COMFYUI_API = value; break;
                    case "COMFYUI_IMAGE_PATH": config.COMFYUI_IMAGE_PATH = value; break;
                    case "MULTI_MODEL_1": config.MULTI_MODEL_1 = value; break;
                    case "MULTI_MODEL_2": config.MULTI_MODEL_2 = value; break;
                    case "MULTI_MODEL_3": config.MULTI_MODEL_3 = value; break;
                    case "EMBEDDIN_MODEL": config.EMBEDDIN_MODEL = value; break;
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
                $"API_KEY={config.API_KEY}",
                $"ALLOWED_CHANNEL_IDS={string.Join(",", config.ALLOWED_CHANNEL_IDS)}",
                $"ALLOWED_GROUP_CHANNEL_IDS={string.Join(",", config.ALLOWED_GROUP_CHANNEL_IDS)}",
                $"SYSTEM_MESSAGE={config.SYSTEM_MESSAGE?.Replace(Environment.NewLine, "\\n")}",
                $"SERPER_API_KEY={config.SERPER_API_KEY}",
                $"COMFYUI_API={config.COMFYUI_API}",
                $"COMFYUI_IMAGE_PATH={config.COMFYUI_IMAGE_PATH}",
                $"MULTI_MODEL_1={config.MULTI_MODEL_1}",
                $"MULTI_MODEL_2={config.MULTI_MODEL_2}",
                $"MULTI_MODEL_3={config.MULTI_MODEL_3}",
                $"EMBEDDIN_MODEL={config.EMBEDDIN_MODEL}"
            };

            await File.WriteAllLinesAsync(_filePath, lines);
        }
    }
}
