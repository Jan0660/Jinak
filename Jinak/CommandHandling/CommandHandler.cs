using System.Reflection;
using System.Text.RegularExpressions;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Jinak.Utility.Help;

namespace Jinak.CommandHandling;

public class CommandHandler
{
    private readonly DiscordSocketClient _client;
    public readonly CommandService Service;

    // Retrieve client and CommandService instance via ctor
    public CommandHandler(DiscordSocketClient client, CommandService service)
    {
        Service = service;
        _client = client;
    }

    public async Task InstallCommandsAsync()
    {
        // Hook the MessageReceived event into our command handler
        _client.MessageReceived += HandleCommandAsync;
        Service.CommandExecuted += CommandExecuted;
        // Here we discover all of the command modules in the entry 
        // assembly and load them. Starting from Discord.NET 2.0, a
        // service provider is required to be passed into the
        // module registration method to inject the 
        // required dependencies.
        //
        // If you do not use Dependency Injection, pass null.
        // See Dependency Injection guide for more information.
        await Service.AddModulesAsync(assembly: Assembly.GetEntryAssembly(),
            services: null);
    }

    private async Task CommandExecuted(Optional<CommandInfo> command, ICommandContext contextNon,
        IResult result)
    {
        var context = (BetterSocketCommandContext)contextNon;
        // // handle memory leaks by gif commands
        // if (command.IsSpecified
        //     && (command.Value.Attributes.Any(a => a.GetType() == typeof(ReleaseImageSharpResources))
        //         | command.Value.Module.Attributes.Any(a => a.GetType() == typeof(ReleaseImageSharpResources))))
        // {
        //     SixLabors.ImageSharp.Configuration.Default.MemoryAllocator.ReleaseRetainedResources();
        // }

        if (!result.IsSuccess)
        {
            if (result.Error == CommandError.Exception && result is ExecuteResult execResult)
            {
                // exception occurred while handling command
                // todo: HHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHH
                // Console.Exception(execResult.Exception);
                // if (execResult.Exception is IFriendlyException friendlyException)
                // {
                //     await context.Message.Channel.SendMessageAsync(embed: friendlyException.GetEmbed());
                // }
                // else
                {
                    var msg = await context.Message.Channel.SendMessageAsync(embed: new EmbedBuilder()
                    {
                        Title = "An internal exception has occurred",
                        Description = $@"Here's some info, you might be able to figure out what's wrong:
```{execResult.Exception.Message}```",
                        Color = Color.Red
                    }.Build());
//                     var rec = new ReactableMessage(msg, (con, react) =>
//                     {
//                         if (react.Emote.Name == "🛠️")
//                         {
//                             if (Settings.BotSettings.Devs.Contains(react.UserId))
//                             {
//                                 return msg.ModifyAsync(m => m.Embed = new EmbedBuilder()
//                                 {
//                                     Title = "Developer exception information",
//                                     Description = $@"Message: ```{execResult.Exception.Message}```
// Source: `{execResult.Exception.Source}`
// TargetSite name: `{execResult.Exception.TargetSite?.Name}`
// TargetSite module: `{execResult.Exception.TargetSite?.Module}`
// HRESULT: `{execResult.Exception.HResult}`
// Help link: `{execResult.Exception.HelpLink}`",
//                                     Color = new Color(47, 49, 54)
//                                 }.Build());
//                             }
//                         }
//
//                         if (react.Emote.Name == "🪜")
//                         {
//                             if (Settings.BotSettings.Devs.Contains(react.UserId))
//                             {
//                                 return msg.ModifyAsync(m => m.Embed = new EmbedBuilder()
//                                 {
//                                     Title = "Developer exception information - StackTrace",
//                                     Description = $@"```{execResult.Exception.StackTrace}```",
//                                     Color = new Color(47, 49, 54)
//                                 }.Build());
//                             }
//                         }
//
//                         return Task.CompletedTask;
//                     });
//                     Reactables.Add(rec);
                }
            }
            else if (result.Error == CommandError.BadArgCount | result.Error == CommandError.ParseFailed)
            {
                await context.Message.Channel.SendMessageAsync(":x: Invalid command syntax",
                    embed: command.Value.GetDescriptionEmbed());
            }
            else if (result.Error == CommandError.UnmetPrecondition)
            {
                // if (result is BetterPreconditionResult result1)
                // {
                //     await context.Message.Channel.SendMessageAsync(embed: result1.GetEmbed());
                // }
                // else
                {
                    var res = result as PreconditionResult;
                    // check if is a permission unable gett'd
                    var regex = new Regex("(?<=((User|Bot) requires (guild|channel) permission ))(.*)(?=\\.)");
                    var match = regex.Match(res.ErrorReason);
                    if (match.Success)
                    {
                        var permission = match.ToString();
                        // either "guild" or "channel"
                        var permissionLocation = res.ErrorReason.Contains("guild") ? "guild" : "channel";
                        if (res.ErrorReason.Contains("Bot"))
                        {
                            // Bot doesn't have a permission
                            await context.Channel.SendMessageAsync(embed:
                                new EmbedBuilder
                                {
                                    Title = "I don't have the permissions required!",
                                    Description =
                                        $"I cannot execute this command because I lack the {permissionLocation} permission `{permission}`!",
                                    Color = Color.Red
                                }.Build());
                        }
                        else
                        {
                            // Bot doesn't have a permission
                            await context.Channel.SendMessageAsync(embed:
                                new EmbedBuilder
                                {
                                    Title = "You can't do that!",
                                    Description =
                                        $"You need the `{permission}` {permissionLocation} permission to use this command!",
                                    Color = Color.Red
                                }.Build());
                        }
                    }
                    else
                    {
                        await context.Message.Channel.SendMessageAsync(embed: new EmbedBuilder()
                        {
                            Title = "Unpassed precondition",
                            Description = res.ToString(),
                            Color = Color.Red
                        }.Build());
                    }
                }
            }
            // else if (result.Error == CommandError.UnknownCommand)
            // {
            //     var content = context.Message.Content;
            //     if (content.StartsWith(context.Prefix))
            //     {
            //         content = content.Remove(0, context.Prefix.Length);
            //     }
            //
            //     int argPos = 0;
            //     if (context.Message.HasMentionPrefix(_client.CurrentUser, ref argPos))
            //     {
            //         content = content.Remove(0, argPos);
            //     }
            //
            //     if (content.EndsWith(" help"))
            //         content = content.Remove(content.IndexOf(" help", StringComparison.Ordinal));
            //     await Program.SendHelpPage(content, context.Channel);
            // }
            else
            {
                await context.Message.Channel.SendMessageAsync($"{result.Error}: {result.ErrorReason}");
            }

            return;
        }
    }

    private async Task HandleCommandAsync(SocketMessage messageParam)
    {
        // Don't process the command if it was a system message
        var message = messageParam as SocketUserMessage;
        if (message == null) return;
        // Create context
        var context = new BetterSocketCommandContext(_client, message);
        // var prefix = context.ServerSettings.Prefix ?? Program.prefix;
        // todo(parity): custom prefixes
        var prefix = "if~";
        // Create a number to track where the prefix ends and the command begins
        int argPos = prefix.Length;
        // Determine if the message is a command based on the prefix and make sure no bots trigger commands
        if (!(message.Content.StartsWith(prefix)
              || message.HasMentionPrefix(_client.CurrentUser, ref argPos)
                )
            || message.Author.IsBot
           )
        {
#pragma warning disable 4014
            // AzurGame.HandleMessage(context);
#pragma warning restore 4014
            return;
        }

        context.Prefix = message.Content[..argPos];

        // todo(parity): blacklist
        // todo(parity): bot lockdown

        // Execute the command with the command context we just
        // created, along with the service provider for precondition checks.
        var result = await Service.ExecuteAsync(
            context: context,
            argPos: argPos,
            services: null);
    }

    // todo(cleanup): figure out where to put this
    /// <summary>
    /// gets a command by one of it's aliases
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    public CommandInfo[] GetCommands(string name)
        // todo(cleanup): this is a mess
        => Service.Commands
            .Where((command) => command.Aliases.Any(a => a.ToLower().Contains(name.ToLower()))).ToArray();

    // todo(parity): port all
    // todo(cleanup): figure out where to put this
    // public IEnumerable<CommandInfo> GetPublicCommands()
    // {
    //     return Program.commandHandler._commands.Commands.Where(
    //         c =>
    //         {
    //             bool SuitableModule(ModuleInfo module)
    //             {
    //                 if (module.IsHidden())
    //                     if (module.GetAttribute<SecondHelpPage>() == null)
    //                         return false;
    //                 if (module.Attributes.All(a => a.GetType() != typeof(HelpPageAttribute)))
    //                     return false;
    //                 if (module.Parent != null)
    //                     return SuitableModule(module.Parent);
    //                 return true;
    //             }
    //
    //             bool SuitableCommand(CommandInfo cmd)
    //             {
    //                 if (cmd.IsHidden())
    //                     return false;
    //                 return SuitableModule(cmd.Module);
    //             }
    //
    //             return SuitableCommand(c);
    //         });
    // }
}