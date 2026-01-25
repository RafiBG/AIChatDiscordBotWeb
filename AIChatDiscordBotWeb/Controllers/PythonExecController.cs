using Microsoft.AspNetCore.Mvc;
using System.Text.Json.Serialization;

namespace AIChatDiscordBotWeb.Controllers
{
    [ApiController]
    [Route("api/python")]
    public class PythonExecController : ControllerBase
    {
        private readonly string _pythonEndpoint = "http://localhost:5000/api/python/execute";

        [HttpPost("execute")]
        public async Task<ActionResult<PythonExecResult>> ExecutePython(
            [FromBody] PythonExecRequest payload)
        {
            if (string.IsNullOrWhiteSpace(payload.Code))
                return BadRequest(new PythonExecResult
                {
                    Status = "Error",
                    Error = "No code provided"
                });

            try
            {
                using var http = new HttpClient();
                var response = await http.PostAsJsonAsync(_pythonEndpoint, payload);

                if (!response.IsSuccessStatusCode)
                {
                    return StatusCode(500, new PythonExecResult
                    {
                        Status = "Python Error",
                        Error = "Python service returned error"
                    });
                }

                var result = await response.Content.ReadFromJsonAsync<PythonExecResult>();
                result.Status = "Success";
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new PythonExecResult
                {
                    Status = "Offline",
                    Error = ex.Message
                });
            }
        }
    }

    public class PythonExecRequest
    {
        [JsonPropertyName("code")]
        public string Code { get; set; }
    }

    public class PythonExecResult
    {
        public string Status { get; set; }
        [JsonPropertyName("stdout")]
        public string Stdout { get; set; }

        [JsonPropertyName("stderr")]
        public string Stderr { get; set; }
        public string Error { get; set; }
    }
}
