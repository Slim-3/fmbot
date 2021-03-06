using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Discord.Commands;
using Discord.WebSocket;
using FMBot.Bot.Configurations;
using FMBot.Bot.Resources;
using FMBot.Bot.Services;

namespace FMBot.Bot.Handlers
{
    public class CommandHandler
    {
        private static readonly List<DateTimeOffset> StackCooldownTimer = new List<DateTimeOffset>();
        private static readonly List<SocketUser> StackCooldownTarget = new List<SocketUser>();
        private readonly CommandService _commands;
        private readonly DiscordShardedClient _discord;
        private readonly IPrefixService _prefixService;
        private readonly IServiceProvider _provider;

        // DiscordSocketClient, CommandService, IConfigurationRoot, and IServiceProvider are injected automatically from the IServiceProvider
        public CommandHandler(
            DiscordShardedClient discord,
            CommandService commands,
            IServiceProvider provider, IPrefixService prefixService)
        {
            this._discord = discord;
            this._commands = commands;
            this._provider = provider;
            this._prefixService = prefixService;

            this._discord.MessageReceived += OnMessageReceivedAsync;
        }

        private async Task OnMessageReceivedAsync(SocketMessage s)
        {
            var msg = s as SocketUserMessage; // Ensure the message is from a user/bot
            if (msg == null)
            {
                return;
            }

            if (msg.Author.Id == this._discord.CurrentUser.Id)
            {
                return; // Ignore self when checking commands
            }

            if (msg.Author.IsBot)
            {
                return; // Ignore bots
            }

            var context = new ShardedCommandContext(this._discord, msg); // Create the command context

            var argPos = 0; // Check if the message has a valid command prefix
            var customPrefix = this._prefixService.GetPrefix(context.Guild?.Id);
            if (msg.HasStringPrefix(ConfigData.Data.CommandPrefix, ref argPos) && customPrefix == null || msg.HasMentionPrefix(this._discord.CurrentUser, ref argPos))
            {
                await ExecuteCommand(msg, context, argPos);
            }
            else if (msg.HasStringPrefix(customPrefix, ref argPos, StringComparison.CurrentCultureIgnoreCase))
            {
                await ExecuteCommand(msg, context, argPos, customPrefix);
            }
        }

        private async Task ExecuteCommand(SocketUserMessage msg, ShardedCommandContext context, int argPos,
            string customPrefix = null)
        {
            if (StackCooldownTarget.Contains(msg.Author))
            {
                //If they have used this command before, take the time the user last did something, add 1100ms, and see if it's greater than this very moment.
                if (StackCooldownTimer[StackCooldownTarget.IndexOf(msg.Author)].AddMilliseconds(1100) >=
                    DateTimeOffset.Now)
                {
                    return;
                }

                StackCooldownTimer[StackCooldownTarget.IndexOf(msg.Author)] = DateTimeOffset.Now;
            }
            else
            {
                //If they've never used this command before, add their username and when they just used this command.
                StackCooldownTarget.Add(msg.Author);
                StackCooldownTimer.Add(DateTimeOffset.Now);
            }

            var searchResult = this._commands.Search(context, argPos);

            // If no command were found, return.
            if ((searchResult.Commands == null || searchResult.Commands.Count == 0) && customPrefix != null && !msg.Content.StartsWith(customPrefix))
            {
                return;
            }

            if ((searchResult.Commands == null || searchResult.Commands.Count == 0) && msg.Content.StartsWith(ConfigData.Data.CommandPrefix))
            {
                var commandPrefixResult = await this._commands.ExecuteAsync(context, msg.Content.Remove(0, 1), this._provider);

                if (commandPrefixResult.IsSuccess)
                {
                    Statistics.CommandsExecuted.Inc();
                }
                else
                {
                    var logger = new Logger.Logger();
                    logger.LogError(commandPrefixResult.ToString(), context.Message.Content);
                }

                return;
            }

            var result = await this._commands.ExecuteAsync(context, argPos, this._provider);

            if (result.IsSuccess)
            {
                Statistics.CommandsExecuted.Inc();
            }
            else
            {
                var logger = new Logger.Logger();
                logger.LogError(result.ToString(), context.Message.Content);
            }
        }
    }
}
