using Microsoft.SemanticKernel;
using System.ComponentModel;
using System.Text;
using System.Text.Json;

namespace AIChatDiscordBotWeb.Tools
{
    public class SerperSearchTool
    {
        private readonly string _apiKey;
        private readonly HttpClient _http;
        public static List<string> LatestLinks { get; private set; } = new();

        public SerperSearchTool(string apiKey)
        {
            _apiKey = apiKey;
            _http = new HttpClient();
        }

        [KernelFunction("serper_search")]
        [Description("Search the web using Serper API and return summarized results.")]
        public async Task<string> SearchAsync([Description("Search query text")] string query)
        {
            if (string.IsNullOrWhiteSpace(_apiKey))
            {
                Console.WriteLine("Serper API key is missing.");
                return "Serper API key is missing.";
            }

            string endpoint = query.Contains("news", StringComparison.OrdinalIgnoreCase)
                ? "https://google.serper.dev/news"
                : "https://google.serper.dev/search";

            Console.WriteLine($"Tool: Serper web starting for query: {query}");
            //Console.WriteLine($"Tool: Endpoint: {endpoint}");

            var payload = new { q = query };
            var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

            var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
            request.Headers.Add("X-API-KEY", _apiKey);
            request.Content = content;

            try
            {
                var response = await _http.SendAsync(request);
                Console.WriteLine($"Tool: Serper API status: {response.StatusCode}");

                var responseBody = await response.Content.ReadAsStringAsync();
                //Console.WriteLine($"Tool: Serper API content: {responseBody}");

                if (!response.IsSuccessStatusCode)
                    return $"Serper API error: {response.StatusCode}";

                using var doc = JsonDocument.Parse(responseBody);

                var sb = new StringBuilder();
                sb.AppendLine($"**Top results for:** {query}\n");

                if (doc.RootElement.TryGetProperty("organic", out var organic))
                {
                    int index = 1;
                    foreach (var item in organic.EnumerateArray())
                    {
                        var title = item.GetProperty("title").GetString();
                        var link = item.GetProperty("link").GetString();
                        var snippet = item.GetProperty("snippet").GetString();

                        sb.AppendLine($"**{index}. {title}**\n{snippet}\n<{link}>\n");
                        LatestLinks.Add(link);
                        index++;
                    }
                }
                else
                {
                    Console.WriteLine("Tool: Serper web no results found.");
                    sb.AppendLine("No results found.");
                }

                return sb.ToString();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Tool: Exception while querying Serper: {ex.Message}");
                return $"Exception while querying Serper: {ex.Message}";
            }
        }
    }
}
