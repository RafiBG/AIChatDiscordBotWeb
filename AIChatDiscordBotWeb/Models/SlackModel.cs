using System.Text.Json.Serialization;

namespace AIChatDiscordBotWeb.Models
{
    public class SlackModel
    {
        [JsonPropertyName("userId")]
        public string userId { get; set; } = string.Empty;

        [JsonPropertyName("guildId")]
        public string guildId { get; set; } = string.Empty;

        [JsonPropertyName("channelId")]
        public string channelId { get; set; } = string.Empty;

        [JsonPropertyName("transcription")]
        public string transcription { get; set; } = string.Empty;
    }
}
