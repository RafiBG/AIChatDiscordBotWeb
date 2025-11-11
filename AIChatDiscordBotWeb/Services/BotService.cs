using AIChatDiscordBotWeb.Models;
using AIChatDiscordBotWeb.SlashCommadns;
using AIChatDiscordBotWeb.SlashCommands;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Enums;
using DSharpPlus.Interactivity.Extensions;
using DSharpPlus.SlashCommands;
using DSharpPlus.VoiceNext;

namespace AIChatDiscordBotWeb.Services
{
    public class BotService
    {
        private DiscordClient _client;
        private EnvService _envService;
        private readonly IServiceProvider _serviceProvider;
        private bool _isRunning;

        public BotService(EnvService envService, IServiceProvider serviceProvider)
        {
            _envService = envService;
            _serviceProvider = serviceProvider;
        }


        public async Task StartAsync()
        {
            if (_isRunning) return;

            EnvConfig config = await _envService.LoadAsync();

            _client = new DiscordClient(new DiscordConfiguration
            {
                Token = config.BOT_TOKEN,
                TokenType = TokenType.Bot,
                Intents = DiscordIntents.All,
                AutoReconnect = true
            });
            //_client.UseVoiceNext();

            _client.UseInteractivity(new InteractivityConfiguration()
            {
                PollBehaviour = PollBehaviour.KeepEmojis,
                Timeout = TimeSpan.FromSeconds(30)
            });

            _client.UseVoiceNext(new VoiceNextConfiguration
            {
                EnableIncoming = true
            });

            _client.Ready += async (s, e) =>
            {
                Console.WriteLine("\nBot is online.");
                await Task.CompletedTask;

                await _client.UpdateStatusAsync(
                    new DiscordActivity("/help", ActivityType.ListeningTo));
            };

            var slash = _client.UseSlashCommands(new SlashCommandsConfiguration
            {
                Services = _serviceProvider
            });

            // Registers slash commands from this classes
            slash.RegisterCommands<AIChat>();
            slash.RegisterCommands<MultiModel>();
            //slash.RegisterCommands<VoiceRecorder>();

            await _client.ConnectAsync();
            _isRunning = true;
        }

        public async Task StopAsync()
        {
            if (!_isRunning) return;

            Console.WriteLine("\nBot is offline.");

            await _client.DisconnectAsync();
            _client.Dispose();

            _isRunning = false;
        }

        public bool IsRunning() => _isRunning;
    }
}
