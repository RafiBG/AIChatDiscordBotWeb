using Microsoft.SemanticKernel;
using System.ComponentModel;
using System.Text;
using System.Text.Json;

namespace AIChatDiscordBotWeb.Tools
{
    public class ComfyUITool
    {
        private readonly string _comfyUiApi;

        public static bool IsImageGenerating { get; set; } = false;

        public ComfyUITool(string comfyUiApi)
        {
            _comfyUiApi = comfyUiApi;
        }

        [KernelFunction("generate_image")]
        [Description("Generate an image using the ComfyUI Lumina 2 workflow from a given text prompt. It may take some time to finish.")]
        public async Task<string> GenerateImageAsync(string userPrompt)
        {
            IsImageGenerating = true;
            try
            {
                var random = new Random();
                int randomSeed = random.Next(10000, 999999999);

                using var http = new HttpClient();

                string genText = "You are an assistant designed to generate superior images with a superior degree of text-image alignment based on the following prompt: " +
                                 $"<Prompt Start> {userPrompt}";

                // Build the JSON workflow payload
                var jsonObject = new
                {
                    prompt = new Dictionary<string, object>
                    {
                        ["4"] = new
                        {
                            inputs = new { ckpt_name = "lumina_2.safetensors" },
                            class_type = "CheckpointLoaderSimple"
                        },
                        ["6"] = new
                        {
                            inputs = new { text = genText, clip = new object[] { "4", 1 } },
                            class_type = "CLIPTextEncode"
                        },
                        ["7"] = new
                        {
                            inputs = new { text = "low quality, blurry, distorted, bad hands, bad anatomy", clip = new object[] { "4", 1 } },
                            class_type = "CLIPTextEncode"
                        },
                        ["13"] = new
                        {
                            inputs = new { width = 1024, height = 1024, batch_size = 1 },
                            class_type = "EmptySD3LatentImage"
                        },
                        ["11"] = new
                        {
                            inputs = new { shift = 4, model = new object[] { "4", 0 } },
                            class_type = "ModelSamplingAuraFlow"
                        },
                        ["3"] = new
                        {
                            inputs = new
                            {
                                seed = randomSeed,
                                steps = 20,
                                cfg = 4,
                                sampler_name = "res_multistep",
                                scheduler = "simple",
                                denoise = 1,
                                model = new object[] { "11", 0 },
                                positive = new object[] { "6", 0 },
                                negative = new object[] { "7", 0 },
                                latent_image = new object[] { "13", 0 }
                            },
                            class_type = "KSampler"
                        },
                        ["8"] = new
                        {
                            inputs = new { samples = new object[] { "3", 0 }, vae = new object[] { "4", 2 } },
                            class_type = "VAEDecode"
                        },
                        ["9"] = new
                        {
                            inputs = new { filename_prefix = "Lumina2_Num_", images = new object[] { "8", 0 } },
                            class_type = "SaveImage"
                        }
                    }
                };

                // Serialize to JSON
                var json = JsonSerializer.Serialize(jsonObject, new JsonSerializerOptions { WriteIndented = true });
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                // Send to ComfyUI API
                var response = await http.PostAsync($"{_comfyUiApi}/prompt", content);

                if (!response.IsSuccessStatusCode)
                {
                    IsImageGenerating = false;
                    var errorText = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"[ComfyUI] Failed: {response.StatusCode} - {errorText}");
                    return $"ComfyUI error: {response.StatusCode}";
                }

                Console.WriteLine("[ComfyUI] Image generation started successfully.");
                return "Image generation started successfully.";
            }
            catch (Exception ex)
            {
                IsImageGenerating = false;
                Console.WriteLine($"[ComfyUI] Error: {ex.Message}");
                return $"Error: {ex.Message}";
            }
        }
    }
}
