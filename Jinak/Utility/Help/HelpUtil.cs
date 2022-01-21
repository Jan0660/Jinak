using Discord;
using Discord.Commands;

namespace Jinak.Utility.Help;

public static class HelpUtil
{
    public static Embed GetDescriptionEmbed(this CommandInfo command, bool extraInfo = false)
    {
        EmbedBuilder embed = new EmbedBuilder()
        {
            Title = $"Command help: {command.Name}"
        };
        embed.WithFields(new EmbedFieldBuilder() { Name = "Name", Value = command.Name, IsInline = true },
            new EmbedFieldBuilder()
                { Name = "Aliases", Value = string.Join(", ", command.Aliases), IsInline = true },
            new EmbedFieldBuilder()
                { Name = "Category", Value = command.Module.GetCategory("None"), IsInline = true },
            new EmbedFieldBuilder() { Name = "Usage", Value = command.GetUsage() }
        );
        string channelPerms = command.GetBotChannelPermissionList();
        string guildPerms = command.GetBotGuildPermissionList();
        // handle permissions for bot
        if (channelPerms != null | guildPerms != null)
        {
            string str = null;
            if (channelPerms != null)
                str = channelPerms;
            if (str != null && guildPerms != null)
                str += ", " + guildPerms;
            else if (str == null && guildPerms != null)
                str = guildPerms;
            embed.AddField(new EmbedFieldBuilder()
            {
                Name = "Bot permissions",
                Value = str,
                IsInline = true
            });
        }

        // handle permission for user
        string? channelUserPerms = command.GetUserChannelPermissionList();
        string? guildUserPerms = command.GetUserGuildPermissionList();
        if (channelUserPerms != null | guildUserPerms != null)
        {
            string? str = null;
            if (channelUserPerms != null)
            {
                str = channelUserPerms;
            }

            if (str != null && guildUserPerms != null)
            {
                str += ", " + guildUserPerms;
            }
            else if (str == null && guildUserPerms != null)
            {
                str = guildUserPerms;
            }

            embed.AddField(new EmbedFieldBuilder()
            {
                Name = "User permissions",
                Value = str,
                IsInline = true
            });
        }

        if (extraInfo)
        {
            List<string> attrs = new List<string>();
            foreach (Attribute att in command.Attributes)
            {
                attrs.Add($"[{att.GetType().Name}]");
            }

            embed.AddField(new EmbedFieldBuilder()
            {
                Name = "Attributes",
                IsInline = true,
                Value = string.Join(", ", attrs)
            });
        }

        if (command.GetDescription() != null)
        {
            embed.AddField(new EmbedFieldBuilder()
            {
                Name = "Description",
                Value = command.GetDescription()
            });
        }

        // if (command.Attributes.FirstOrDefault(a => a is HelpWebPageAttribute) is HelpWebPageAttribute helpWebPageAttribute)
        // {
        //     embed.AddField(new EmbedFieldBuilder()
        //     {
        //         Name = "Help Page",
        //         Value = $"[Click here]({helpWebPageAttribute.Url})"
        //     });
        // }


        return embed.Build();
    }

    public static PreconditionAttribute[] GetAllPreconditions(this CommandInfo command)
    {
        List<PreconditionAttribute> result = new List<PreconditionAttribute>();
        result.AddRange(command.Preconditions);
        result.AddRange(command.Module.GetAllPreconditions());
        return result.ToArray();
    }

    public static List<PreconditionAttribute> GetAllPreconditions(this ModuleInfo module)
    {
        List<PreconditionAttribute> result = new List<PreconditionAttribute>();
        result.AddRange(module.Preconditions);
        if (module.Parent != null)
        {
            result.AddRange(module.Parent.GetAllPreconditions());
        }

        return result;
    }

    public static string GetCategory(this ModuleInfo module, string nullReturn = null)
    {
        // todo(cleanup): use GetAttribute<T>
        var ass = (HelpPageAttribute)module.Attributes.FirstOrDefault((att) => att as HelpPageAttribute != null);
        if (ass == null)
            return nullReturn;
        return ass.Name;
    }

    public static string GetUsage(this CommandInfo command)
    {
        string result = command.Name;
        foreach (Discord.Commands.ParameterInfo param in command.Parameters)
        {
            result += $" <{((param.Summary == "" | param.Summary == null) ? param.Name : param.Summary)}>";
        }

        return result;
    }

    public static string GetDescription(this CommandInfo command)
        => command.GetAttribute<RemarksAttribute>()?.Text ?? command.Summary;

    public static string? GetBotChannelPermissionList(this CommandInfo command)
    {
        List<string> result = new List<string>();
        foreach (PreconditionAttribute att in command.GetAllPreconditions())
        {
            if (att is RequireBotPermissionAttribute)
            {
                var perm = att as RequireBotPermissionAttribute;
                if (perm.ChannelPermission.HasValue)
                {
                    result.Add(perm.ChannelPermission.ToString()!);
                }
            }
        }

        /*
        foreach (PreconditionAttribute att in command.Module.Preconditions)
        {
            if (att is RequireBotPermissionAttribute)
            {
                var perm = att as RequireBotPermissionAttribute;
                if (perm.ChannelPermission.HasValue)
                {
                    result.Add(perm.ChannelPermission.ToString());
                }
            }
        }*/
        return result.Count == 0 ? null : string.Join(", ", result);
    }

    public static string? GetBotGuildPermissionList(this CommandInfo command)
    {
        List<string> result = new List<string>();
        foreach (PreconditionAttribute att in command.GetAllPreconditions())
        {
            if (att is RequireBotPermissionAttribute)
            {
                var perm = att as RequireBotPermissionAttribute;
                if (perm.GuildPermission.HasValue)
                    result.Add(perm.GuildPermission.ToString()!);
            }
        }

        /*
        foreach (PreconditionAttribute att in command.Module.Preconditions)
        {
            if (att is RequireBotPermissionAttribute)
            {
                var perm = att as RequireBotPermissionAttribute;
                if (perm.GuildPermission.HasValue)
                {
                    result.Add(perm.GuildPermission.ToString());
                }
            }
        }*/
        return result.Count == 0 ? null : string.Join(", ", result);
    }

    public static string? GetUserChannelPermissionList(this CommandInfo command)
    {
        List<string> result = new List<string>();
        foreach (PreconditionAttribute att in command.GetAllPreconditions())
        {
            if ((att as RequireUserPermissionAttribute) == null)
                continue;
            var perm = att as RequireUserPermissionAttribute;
            if (perm.ChannelPermission.HasValue)
            {
                result.Add(perm.ChannelPermission.ToString()!);
            }
        }

        /*
        foreach (PreconditionAttribute att in command.Module.Preconditions)
        {
            if ((att as RequireUserPermissionAttribute) == null)
                continue;
            var perm = att as RequireUserPermissionAttribute;
            if (perm.ChannelPermission.HasValue)
            {
                result.Add(perm.ChannelPermission.ToString());
            }
        }*/
        return result.Count == 0 ? null : string.Join(", ", result);
    }

    public static string? GetUserGuildPermissionList(this CommandInfo command)
    {
        List<string> result = new List<string>();
        foreach (PreconditionAttribute att in command.GetAllPreconditions())
        {
            if ((att as RequireUserPermissionAttribute) == null)
                continue;
            var perm = att as RequireUserPermissionAttribute;
            if (perm.GuildPermission.HasValue)
                result.Add(perm.GuildPermission.ToString()!);
        }

        /*
        foreach (PreconditionAttribute att in command.Module.Preconditions)
        {
            if ((att as RequireUserPermissionAttribute) == null)
                continue;
            var perm = att as RequireUserPermissionAttribute;
            if (perm.GuildPermission.HasValue)
            {
                result.Add(perm.GuildPermission.ToString());
            }
        }*/
        return result.Count == 0 ? null : string.Join(", ", result);
    }
}