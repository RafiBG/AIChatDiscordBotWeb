using Microsoft.SemanticKernel;
using System.ComponentModel;

namespace AIChatDiscordBotWeb.Tools
{
    public class PythonExecutionTool
    {
        private readonly string _pythonApi;

        public PythonExecutionTool(string pythonApi)
        {
            _pythonApi = pythonApi;
        }

        [KernelFunction("run_python")]
        [Description("Executes Python code for math, calculations, statistics, or simulations. Use this when exact computation is required.")]
        public async Task<string> RunPythonAsync(
            [Description("Valid Python code. Always print the final result.")] string pythonCode)
        {
            using var http = new HttpClient();
            var payload = new { code = pythonCode };

            Console.WriteLine("[Tool] Python code writen");
            try
            {
                var response = await http.PostAsJsonAsync(
                    $"{_pythonApi}/run",
                    payload
                );

                if (!response.IsSuccessStatusCode)
                    return "Python execution failed. The service may be offline.";

                var result = await response.Content.ReadFromJsonAsync<PythonExecResult>();
                Console.WriteLine($"[Tool] Python code result: {result.Stdout}");

                if (!string.IsNullOrWhiteSpace(result.Stderr))
                    return $"Python error:\n{result.Stderr}";
                return $"Python output:\n{result.Stdout}";
            }
            catch
            {
                return "Error contacting Python execution service.";
            }
        }

        private class PythonExecResult
        {
            public string Stdout { get; set; }
            public string Stderr { get; set; }
        }
    }
}
