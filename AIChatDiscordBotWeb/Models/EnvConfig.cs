namespace AIChatDiscordBotWeb.Models
{
    public class EnvConfig
    {
        public string BOT_TOKEN { get; set; }
        public string LOCAL_HOST { get; set; }
        public string MODEL { get; set; }
        public string API_KEY { get; set; }
        public List<ulong> ALLOWED_CHANNEL_IDS { get; set; } = new List<ulong>();
        public List<ulong> ALLOWED_GROUP_CHANNEL_IDS { get; set; } = new List<ulong>();
        public string SYSTEM_MESSAGE { get; set; }
        public int SHORT_MEMORY { get; set; }
        public string SERPER_API_KEY { get; set; }
        public string COMFYUI_API {  get; set; }
        public string COMFYUI_IMAGE_PATH { get; set; }
        public int COMFYUI_IMAGE_WIDTH { get; set; }
        public int COMFYUI_IMAGE_HEIGHT { get; set; }
        public byte COMFYUI_STEPS { get; set; }
        public string TALK_SYSTEM_MESSAGE { get; set; }
        public string MULTI_MODEL_1 { get; set; }
        public string MULTI_MODEL_2 { get; set; }
        public string MULTI_MODEL_3 { get; set; }
        public string EMBEDDIN_MODEL { get; set; }
        public string MUSIC_GENERATION_PATH { get; set; }
        public string PYTHON_EXECUTION_API { get; set; }
    }
}
