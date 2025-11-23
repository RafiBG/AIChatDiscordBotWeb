using Microsoft.SemanticKernel;
using System.ComponentModel;

namespace AIChatDiscordBotWeb.Tools
{
    public class TimeTool
    {
        [KernelFunction]
        [Description("Gets the current time in the local timezone.")]
        public string GetCurrentTime()
        {
            Console.WriteLine($"\n[Tool] Time used. {DateTime.Now:t}");
            return $"The current time is {DateTime.Now:t}";
        }

        [KernelFunction]
        [Description("Gets the current date,month and year in the local timezone.")]
        public string GetCurrentDate()
        {
            Console.WriteLine($"\n[Tool] Date used. {DateTime.Now:d}");
            return $"The current date is {DateTime.Now:d}";
        }
    }
}
