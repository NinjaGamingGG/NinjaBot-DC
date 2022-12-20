using Dapper;
using Dapper.Contrib.Extensions;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using NinjaBot_DC.Models;
using NinjaBot_DC.Models.TwitchAlertModels;

// ReSharper disable ForCanBeConvertedToForeach

namespace NinjaBot_DC.CommandModules;

// ReSharper disable once ClassNeverInstantiated.Global
public class TwitchAlertsCommandModule : BaseCommandModule
{
    [Command("twitch-alerts")]
    private async Task AddCreatorCommand(CommandContext context,string action, DiscordUser user, string roleTag) //Command for Adding or Removing a Creator from the list
    {

        switch (action.ToLower())//Handle add / remove commands
        {
            case ("add-creator"):
            {
                await AddCreator(context, user, roleTag);
            } break;
            

            case ("remove-creator"):
            {
                await RemoveCreator(context, user, roleTag);
            } break;

            default://Error message if arguments are wrong
            {
                await context.Message.RespondAsync($"❌ Error | Wrong Argument: {action}. Please use (!Twitch-Alerts help) for Information on Command Usage");
            } break;
            
        }
        
    }

    private static async Task AddCreator(CommandContext context, DiscordUser user, string roleTag)
    {
        //Check if selected role exists
        var sqLite = Worker.GetServiceSqLiteConnection();
        var alertRoleModel = await sqLite.QueryAsync<TwitchAlertRoleDbModel>(
            $"SELECT * FROM TwitchAlertRoleIndex WHERE (GuildId = {context.Guild.Id} AND RoleTag = '{roleTag}')");
        
        //Convert to list so we can later iterate over it. If it is Empty return
        var alertRoleModelAsList = alertRoleModel.ToList();
        if (!alertRoleModelAsList.Any())
        {
            await context.Message.RespondAsync($"❌ Error | Unable to retrieve linked role");
            return;
        }
        
        //Create new Record for the database and Insert it
        var creatorModel = new TwitchCreatorDbModel() {GuildId = context.Guild.Id, UserId = user.Id, RoleTag = roleTag};
        await sqLite.InsertAsync(creatorModel);
        
        //Get the guild from Command context
        var guild = context.Guild;

        //Get the Discord member
        var guildMember = await context.Guild.GetMemberAsync(user.Id);

       //Add user to all roles associated with the role tag 
        for (var i = 0; i < alertRoleModelAsList.Count; i++)
        {
            //Get the role and add user
            var discordRole = guild.GetRole(alertRoleModelAsList[i].RoleId);
            await guildMember.GrantRoleAsync(discordRole);
        }
        
        //Create success Reaction
        await context.Message.CreateReactionAsync(DiscordEmoji.FromName(context.Client,":white_check_mark:"));
    }
    
    private static async Task RemoveCreator(CommandContext context, DiscordUser user, string roleTag)
    {
        //Delete record from db
        var sqLite = Worker.GetServiceSqLiteConnection();
        await sqLite.ExecuteAsync(
            $"DELETE FROM TwitchCreatorIndex WHERE (GuildId = {context.Guild.Id} AND UserId = {user.Id} AND RoleTag = '{roleTag}')");
        
        //Get the roles associated with role tag so we can remove user from them
        var alertRoleModel = await sqLite.QueryAsync<TwitchAlertRoleDbModel>(
            $"SELECT * FROM TwitchAlertRoleIndex WHERE (GuildId = {context.Guild.Id} AND RoleTag = '{roleTag}')");
        
        //Convert to list so we can iterate over it. Return if list is Empty
        var alertRoleModelAsList = alertRoleModel.ToList();
        if (!alertRoleModelAsList.Any())
        {
            await context.Message.RespondAsync($"❌ Error | Unable to retrieve linked role");
            return;
        }

        //Get guild & discord member from Command context
        var guild = context.Guild;
        var guildMember = await context.Guild.GetMemberAsync(user.Id);

        for (var i = 0; i < alertRoleModelAsList.Count; i++)
        {
            //Get the discord role and remove user from it
            var discordRole = guild.GetRole(alertRoleModelAsList[i].RoleId);
            await guildMember.RevokeRoleAsync(discordRole);
        }
        
        //Create success Reaction
        await context.Message.CreateReactionAsync(DiscordEmoji.FromName(context.Client,":white_check_mark:"));
    }
    
    [Command("twitch-alerts")]
    private async Task LinkRoleCommand(CommandContext context,string action, DiscordRole role, string roleTag) //Command for linking and unlinking a role
    {

        switch (action.ToLower())
        {
            case ("link-role"):
            {
                await LinkRole(context, role, roleTag);
            } break;

            case ("unlink-role"):
            {
                await UnlinkRole(context, roleTag);
            } break;

            default:
            {
                await context.Message.RespondAsync($"❌ Error | Wrong Argument: {action}. Please use (!Twitch-Alerts help) for Information on Command Usage");
            } break;
            
        }
    }

    private static async Task LinkRole(CommandContext context, DiscordRole role, string roleTag)
    {
        //Create new record for database
        var roleModel = new TwitchAlertRoleDbModel()
            {GuildId = context.Guild.Id, RoleId = role.Id, RoleTag = roleTag};

        //Try to Insert
        var sqLite = Worker.GetServiceSqLiteConnection();
        var success = await sqLite.InsertAsync(roleModel);

        //If insert was successfully return with positive reaction
        if (success == 1)
        {
            await context.Message.CreateReactionAsync(DiscordEmoji.FromName(context.Client,":white_check_mark:"));  
            return;
        }

        //Respond with error message
        await context.Message.RespondAsync("❌ Error | Unable to Link the Role");
    }
    
    private static async Task UnlinkRole(CommandContext context, string roleTag)
    {
        //Try to delete the role from the db
        var sqLite = Worker.GetServiceSqLiteConnection();
        var success = await sqLite.ExecuteAsync("DELETE FROM TwitchAlertRoleIndex WHERE " +
                                                $"(GuildId = {context.Guild.Id} " +
                                                $"AND RoleTag = '{roleTag}')");

        //If delete was successfully respond with positive reaction and return
        if (success == 1)
        {
            await context.Message.CreateReactionAsync(DiscordEmoji.FromName(context.Client,":white_check_mark:"));  
            return;
        }
        
        //Respond with error message
        await context.Message.RespondAsync("❌ Error | Unable to Unlink the Role");
    }
    
    [Command("twitch-alerts")] //Command for linking creator social media accounts
    private async Task CreatorAddSocialMediaChannelCommand(CommandContext context,string action, DiscordUser user, string roleTag, string socialMediaChannel, string platform)
    {
        switch (action.ToLower())
        {
            case ("creator-add-channel"):
            {
                await CreatorAddSocialMediaChannel(context, user, roleTag, socialMediaChannel, platform);
            } break;

            case ("creator-remove-channel"):
            {
                await CreatorRemoveSocialMediaChannel(context, user, roleTag, socialMediaChannel, platform);
            } break;

            default:
            {
                await context.Message.RespondAsync($"❌ Error | Wrong Argument: {action}. Please use (!Twitch-Alerts help) for Information on Command Usage");
            } break;
            
        }
        
    }

    private static async Task CreatorAddSocialMediaChannel(CommandContext context, DiscordUser user, string roleTag, string socialMediaChannel, string platform)
    {
        //Create record for db
        var creatorSocialMediaModel = new TwitchCreatorSocialMediaChannelDbModel()
        {
            GuildId = context.Guild.Id, UserId = user.Id, RoleTag = roleTag, SocialMediaChannel = socialMediaChannel,
            Platform = platform
        };

        //Try to insert the record
        var sqLite = Worker.GetServiceSqLiteConnection();
        var insertSuccess = await sqLite.InsertAsync(creatorSocialMediaModel);

        //If insert was successfully respond positive to message and return
        if (insertSuccess == 0)
        {
            await context.Message.RespondAsync($"❌ Error | Unable to to Add Social Media Channel" +
                                               $"");
            return;
        }

        //Respond with error message
        await context.Message.CreateReactionAsync(DiscordEmoji.FromName(context.Client,":white_check_mark:"));  
    }
    
    private static async Task CreatorRemoveSocialMediaChannel(CommandContext context, DiscordUser user, string roleTag, string socialMediaChannel, string platform)
    {
        //Try to delete record from database
        var sqLite = Worker.GetServiceSqLiteConnection();
        var deleteSuccess = await sqLite.ExecuteAsync(
            $"DELETE FROM TwitchCreatorSocialMediaChannelIndex WHERE (GuildId = {context.Guild.Id} AND UserId = {user.Id} AND  RoleTag = '{roleTag}' AND SocialMediaChannel = '{socialMediaChannel}' AND Platform = '{platform}')");

        //If delete was successfully Respond with Error Message and return
        if (deleteSuccess == 0)
        {
            await context.Message.RespondAsync($"❌ Error | Unable to Remove Social Media Channel");
            return;
        }
        
        //Respond Positive 
        await context.Message.CreateReactionAsync(DiscordEmoji.FromName(context.Client,":white_check_mark:"));  
    }
    
    [Command("twitch-alerts")] //Command for linking discord channels to role Tags
    private async Task LinkDiscordChannelCommand(CommandContext context,string action, DiscordChannel channel, string roleTag)
    {

        switch (action.ToLower())
        {
            case ("link-discord-channel"):
            {
                await LinkDiscordChannel(context, channel, roleTag);
            } break;
            
            case ("unlink-discord-channel"):
            {
                await UnlinkDiscordChannel(context, channel, roleTag);
            } break;

            default:
            {
                await context.Message.RespondAsync($"❌ Error | Wrong Argument: {action}. Please use (!Twitch-Alerts help) for Information on Command Usage");
            } break;
            
        }
    }

    private static async Task LinkDiscordChannel(CommandContext context, DiscordChannel channel, string roleTag)
    {
        //Create Record for database
        var twitchDiscordChannel = new TwitchDiscordChannelDbModel()
            {GuildId = context.Guild.Id, ChannelId = channel.Id, RoleTag = roleTag};

        //Try to insert the record
        var sqLite = Worker.GetServiceSqLiteConnection();
        var insertSuccess = await sqLite.InsertAsync(twitchDiscordChannel);

        //If insert was unsuccessful respond with error message and return
        if (insertSuccess == 0)
        {
            await context.Message.RespondAsync($"❌ Error | Unable to Link Discord Channel");
            return;
        }
        
        //Respond with Positive reaction
        await context.Message.CreateReactionAsync(DiscordEmoji.FromName(context.Client,":white_check_mark:"));
    }
    
    private static async Task UnlinkDiscordChannel(CommandContext context, DiscordChannel channel, string roleTag)
    {
        //Try to delete the record from the db
        var sqLite = Worker.GetServiceSqLiteConnection();
        var deleteSuccess = await sqLite.ExecuteAsync(
            $"DELETE FROM TwitchDiscordChannelIndex WHERE (GuildId = {context.Guild.Id} AND ChannelId = {channel.Id} AND  RoleTag = '{roleTag}')");

        //if delete was unsuccessful respond with error message and return
        if (deleteSuccess == 0)
        {
            await context.Message.RespondAsync($"❌ Error | Unable to Unlink Discord Channel");
            return;
        }
        
        //Respond with positive reaction 
        await context.Message.CreateReactionAsync(DiscordEmoji.FromName(context.Client,":white_check_mark:"));
    }
    
    [Command("twitch-alerts")]// The help Command
    private async Task HelpCommand(CommandContext context,string action)
    {

        switch (action.ToLower())
        {
            case ("help"):
            {
                var helpMessage = new DiscordMessageBuilder()
                    .WithContent("Twitch Alerts Command usage\n\n" +
                                 "!twitch-alerts add-creator <@user> <role-tag>     Link the creator with provided role tag\n" +
                                 "!twitch-alerts remove-creator <@user> <role-tag>      Unlink the creator from provided role tag\n" +
                                 "!twitch-alerts link-role <@role> <role-tag>       Link role and a role-tag\n" +
                                 "!twitch-alerts unlink-role <@role> <role-tag>     Unlink role and a role-tag\n" +
                                 "!twitch-alerts creator-add-Channel <@user> <role-tag> <social-media-channel> <platform>        Adds Social Media Channel & Platform to a user under given role tag\n" +
                                 "!twitch-alerts creator-remove-Channel <@user> <role-tag> <social-media-channel> <platform>     Removes Social Media Channel & Platform from a user under given role tag\n" +
                                 "!twitch-alerts link-discord-channel <#discord-channel> <role-tag>     Link a discord channel where live notifications get pushed\n" +
                                 "!twitch-alerts unlink-discord-channel <#discord-channel> <role-tag>       Unlink a discord channel from live notification pushes\n");

                await context.Message.RespondAsync(helpMessage);
            } break;
            
            default:
            {
                await context.Message.RespondAsync($"❌ Error | Wrong Argument: {action}. Please use (!Twitch-Alerts help) for Information on Command Usage");
            } break;
            
        }
    }
    
    [Command("twitch-alerts")]
    private async Task EmptyCommand(CommandContext context)
    {
        await context.Message.RespondAsync($"Please use (!Twitch-Alerts help) for Information on Command Usage");
    }
}