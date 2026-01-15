using Microsoft.SemanticKernel;
using System.ComponentModel;
using System.Text;
using System.Text.Json;

namespace AIChatDiscordBotWeb.Tools
{
    public class MusicGenTool
    {
        private readonly string _musicApi;

        public static bool IsMusicGenerating { get; set; } = false;

        public MusicGenTool(string musicApi)
        {
            _musicApi = musicApi;
        }

        [KernelFunction("generate_music")]
        [Description("Triggers an external music engine to create an audio file. Use this if the user wants to hear a song, a beat, or any musical composition.")]
        public async Task<string> GenerateMusicAsync(
        [Description("The descriptive prompt for the music style (e.g., 'fast rock')")] string aiPrompt,
        [Description("Duration in seconds as an integer. If there is no number default number is 10")] int durationSeconds = 10)
        {
            Console.WriteLine($"[Tool] Music generation triggered for: {aiPrompt} ({durationSeconds}s)");
            int duration = Convert.ToInt32(durationSeconds);
            // Tell Python to start generating
            using var http = new HttpClient();
            var payload = new { prompt = aiPrompt, duration };

            try
            {
                // We just send and move on
                await http.PostAsJsonAsync("http://localhost:5000/generate", payload);

                // Set the flag so your Watcher starts looking
                IsMusicGenerating = true;

                return "Music generation started successfully. Tell user it will take time to be ready and when is done it will show here.";
            }
            catch
            {
                return "Error. I couldn't reach the music engine. Make sure the music generation server is running.";
            }
        }
    }
}
