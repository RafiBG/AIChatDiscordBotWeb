using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Json;
using System.IO;

namespace AIChatDiscordBotWeb.Controllers
{
    [ApiController]
    [Route("api/music")]
    public class MusicGenConnController : ControllerBase
    {
        private readonly string _pythonEndpoint = "http://localhost:5000/generate";
        // Ensure this path exists and Python has permission to write to it
        private readonly string _outputFolder = @"C:\Users\Rafi\Desktop\MusicGenWorker\outputs";

        [HttpPost("generate")]
        public async Task<ActionResult<MusicGenResult>> GenerateMusic([FromBody] MusicGenRequest payload)
        {
            if (string.IsNullOrWhiteSpace(payload.Prompt))
                return BadRequest(new MusicGenResult { Status = "Error", JobId = "No Prompt" });

            string jobId = Guid.NewGuid().ToString("N")[..8];
            string expectedFile = Path.Combine(_outputFolder, $"{jobId}.wav");

            // 1. Tell Python to start
            bool sent = await SendToPythonAsync(jobId, payload.Prompt, payload.Duration);
            if (!sent)
                return StatusCode(500, new MusicGenResult { Status = "Python Offline", JobId = jobId });

            // 2. Poll for the file (Waiting for Python to finish writing)
            int maxAttempts = 30;
            for (int i = 0; i < maxAttempts; i++)
            {
                if (System.IO.File.Exists(expectedFile))
                {
                    // Basic check to ensure the file isn't still being written to
                    await Task.Delay(500);
                    return Ok(new MusicGenResult { Status = "Success", JobId = jobId, FilePath = expectedFile });
                }
                await Task.Delay(4000); // Wait 4 seconds per loop
            }

            return StatusCode(500, new MusicGenResult { Status = "Timeout", JobId = jobId });
        }

        public async Task<bool> SendToPythonAsync(string jobId, string prompt, int duration)
        {
            try
            {
                using var client = new HttpClient();
                var payload = new { prompt, duration, file_name = $"{jobId}.wav" };
                var response = await client.PostAsJsonAsync(_pythonEndpoint, payload);
                return response.IsSuccessStatusCode;
            }
            catch { return false; }
        }
    }

    public class MusicGenRequest { public string Prompt { get; set; } public int Duration { get; set; } = 10; }
    public class MusicGenResult { public string JobId { get; set; } public string FilePath { get; set; } public string Status { get; set; } }
}