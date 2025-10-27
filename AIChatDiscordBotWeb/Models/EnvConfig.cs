namespace AIChatDiscordBotWeb.Models
{
    public class EnvConfig
    {
        public string BOT_TOKEN { get; set; }
        public int LOCAL_HOST { get; set; }
        public string MODEL { get; set; }
        public List<ulong> ALLOWED_CHANNEL_IDS { get; set; } = new List<ulong>();
        public string SYSTEM_MESSAGE { get; set; }
        public string SERPER_API_KEY { get; set; }
        public string COMFYUI_API {  get; set; }
        public string COMFYUI_IMAGE_PATH { get; set; }
    }
}
