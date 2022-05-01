using System.Text;
using Discord;
using Discord.Commands;
using Jinak.CommandHandling;
using Jinak.CommandHandling.Attributes;
using Jinak.Utility.Help;

namespace Jinak.Commands;

[Hidden]
[Group("er")]
[HelpPage("Emote Restriction", "Commands for restricting emotes.", "Emote Restrict", "Emote Restricting", "er")]
[RequireContext(ContextType.Guild)]
[RequireUserPermission(GuildPermission.Administrator)]
public class EmoteRestrictCommands : BetterModuleBase
{
    [Command("restrict")]
    [Summary("Set an emote's access to only the specified role.")]
    public async Task Restrict(string emoteH, IRole role)
    {
        var emote = GetEmote(emoteH);
        await Context.Guild.ModifyEmoteAsync(emote, props =>
        {
            props.Roles = new List<IRole>
            {
                role
            };
        });
        await ReplyAsync($"{emote} restricted to {role.Mention}.", allowedMentions: new());
    }

    [Command("unrestrict")]
    [Alias("clear")]
    [Summary("Clear the restrictions on a command.")]
    public async Task CleanRestrictions(string emoteH)
    {
        var emote = GetEmote(emoteH);
        await Context.Guild.ModifyEmoteAsync(emote, props => props.Roles = new List<IRole>());
        await ReplyAsync($"Cleared restrictions on {emote}.");
    }

    [Command("add")]
    [Summary("Give a role access to an emote.")]
    public async Task AddRole(string emoteH, IRole role)
    {
        var emote = GetEmote(emoteH);
        await Context.Guild.ModifyEmoteAsync(emote, props =>
            props.Roles = new List<IRole>()
            {
                role
            }.Concat(GetRoles(emote)).ToList());
        await ReplyAsync($"Gave {role.Mention} access to {emote}.", allowedMentions: new());
    }

    [Command("remove")]
    [Alias("rm", "revoke")]
    [Summary("Remove a role's access to an emote.")]
    public async Task RemoveRole(string emoteH, IRole role)
    {
        var emote = GetEmote(emoteH);
        var roles = GetRoles(emote);
        roles.Remove(role);
        await Context.Guild.ModifyEmoteAsync(emote, props => props.Roles = roles);
        await ReplyAsync($"Removed {role.Mention}'s access to {emote}.", allowedMentions: new());
    }

    [Command("list")]
    [Alias("ls", "listRole", "roleList")]
    [Summary("List the roles with access to an emote.")]
    public Task RoleList(string emoteH)
    {
        // todo: learn how to use type readers
        var emote = GetEmote(emoteH);
        if (emote.RoleIds.Count == 0)
            return ReplyAsync("This emote is accessible to everyone.");
        StringBuilder str = new();
        foreach (var role in emote.RoleIds)
            str.AppendLine($"• <@&{role}>");

        return ReplyAsync(embed: new EmbedBuilder
        {
            Title = $"List of roles with access to {emote}",
            Description = str.ToString(),
            Footer = new()
            {
                Text = $"Id: {emote.Id}"
            },
            Color = new Color(47, 49, 54)
        }.Build());
    }

    public GuildEmote GetEmote(string str)
    {
        Emote? emote;
        if (!Emote.TryParse(str, out emote))
        {
            if (ulong.TryParse(str, out var id))
            {
                emote = Context.Guild.Emotes.FirstOrDefault(e =>
                    e.Id == id);
            }
            else
                emote = Context.Guild.Emotes.FirstOrDefault(e =>
                    e.Name.Equals(str, StringComparison.InvariantCultureIgnoreCase));

            if (emote == null)
                throw new Exception("Unable to get emote.");
        }

        var guildEmote = Context.Guild.Emotes.First(e => e.Id == emote.Id);
        return guildEmote;
    }

    public List<IRole> GetRoles(GuildEmote emote)
    {
        List<IRole> roles = new();
        foreach (var roleId in emote.RoleIds)
            roles.Add(Context.Guild.Roles.First(r => r.Id == roleId));

        return roles;
    }
}