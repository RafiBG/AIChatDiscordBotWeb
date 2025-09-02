﻿using AIChatDiscordBotWeb.Models;
using AIChatDiscordBotWeb.SlashCommadns;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;

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

        //private string Token = "

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

            _client.Ready += async (s, e) =>
            {
                Console.WriteLine("\nBot is online.");
                await Task.CompletedTask;

                await _client.UpdateStatusAsync(
                    new DiscordActivity("/help", ActivityType.ListeningTo));
            };

            var slash = _client.UseSlashCommands(new SlashCommandsConfiguration
            {
                Services = _serviceProvider // this connects ASP.NET DI to DSharpPlus
            });
            //slash.RegisterCommands<AIChat>();
            slash.RegisterCommands<AIChat>(1317235767319199845);

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
