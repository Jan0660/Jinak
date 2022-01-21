using Discord;
using Discord.Commands;

namespace Jinak.CommandHandling;

public class BetterModuleBase : ModuleBase<BetterSocketCommandContext>
{
    public Task ConfirmReact()
        => Context.Message.AddReactionAsync(new Emoji("✅"));
}