using System.Diagnostics.CodeAnalysis;
using Dapper;
using Dapper.Contrib.Extensions;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.Net.Models;
using DSharpPlus.SlashCommands;
using StatsPlugin.Models;
using StatsPlugin.PluginHelper;

namespace StatsPlugin;

[SlashCommandGroup("stats", "Stats Plugin Commands")]
// ReSharper disable once ClassNeverInstantiated.Global
public class SlashCommandModule : ApplicationCommandModule
{
    [SlashCommand("setup", "Setup for Stats Channel")]
    [SuppressMessage("Performance", "CA1822:Member als statisch markieren")]
    public async Task SetupChannelCommand(InteractionContext ctx)
    {
        await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);
        
        var guild = ctx.Guild;
        var newCategory = await guild.CreateChannelCategoryAsync(@"· • ●  📊 Stats 📊 ● • ·");

        void NewEditModel(ChannelEditModel editModel)
        {
            editModel.PermissionOverwrites = new List<DiscordOverwriteBuilder>()
            {
                new DiscordOverwriteBuilder(guild.EveryoneRole)
                    .Allow(Permissions.AccessChannels)
                    .Deny(Permissions.SendMessages)
                    .Deny(Permissions.UseVoice)
                    .Deny(Permissions.SendMessages)
                    .Deny(Permissions.CreatePublicThreads)
                    .Deny(Permissions.CreatePrivateThreads)
                    .Deny(Permissions.ManageThreads)
                    .For(guild.EveryoneRole)
            };
        }
        
        await newCategory.ModifyAsync(NewEditModel);
        
        var memberCountChannel = await guild.CreateChannelAsync("╔😎～Mitglieder:", ChannelType.Voice, newCategory);
        var botCountChannel = await guild.CreateChannelAsync("╠🤖～Bot Count:", ChannelType.Voice, newCategory);
        var teamCountChannel = await guild.CreateChannelAsync("╚🥷～Teammitglieder:", ChannelType.Voice, newCategory);
        
        var statsChannelModel = new StatsChannelIndexModel()
        {
            GuildId = guild.Id, 
            CategoryChannelId = newCategory.Id, 
            MemberCountChannelId = memberCountChannel.Id, 
            BotCountChannelId = botCountChannel.Id, 
            TeamCountChannelId = teamCountChannel.Id
        };

        var sqlite = SqLiteConnectionHelper.GetSqLiteConnection();
        
        var hasUpdated = await sqlite.UpdateAsync(statsChannelModel);
        
        if (hasUpdated == false)
            await sqlite.InsertAsync(statsChannelModel);

        await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Done!"));
        
    }

    [SlashCommand("link", "Links Stats Channel")]
    [SuppressMessage("Performance", "CA1822:Member als statisch markieren")]
    public async Task LinkChannelCommand(InteractionContext ctx, [Option("Channel", "Target Channel to Link")] DiscordChannel channel, 
        [Option("Channel-Handle", "Handle of the Channel you want to Link")]
ChannelHandleEnum channelHandle = ChannelHandleEnum.NoChannel  )
    {
        await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);

        var sqlite = SqLiteConnectionHelper.GetSqLiteConnection();

        var channelHandleInDb = DatabaseHandleHelper.GetHandleFromEnum(channelHandle);
        
        if (channelHandleInDb == "NoChannel")
        {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Error, Invalid Channel Handle!"));
            return;
        }
        
        var hasUpdated = await sqlite.ExecuteAsync("UPDATE StatsChannelsIndex SET " + channelHandleInDb + " = @ChannelId WHERE GuildId = @GuildId", new { ChannelId = channel.Id, GuildId = ctx.Guild.Id });

        if (hasUpdated == 0)
        {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Error, Unable to Update Channel in Database!"));
            return;
        }

        await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Done!"));
    }
    
    [SlashCommand("rename","Set a custom name for the specified channel")]
    [SuppressMessage("Performance", "CA1822:Member als statisch markieren")]
    public async Task RenameChannelCommand(InteractionContext ctx, [Option("Channel-Handle", "Handle of the Channel you want to Link")] ChannelHandleEnum channelHandle, [Option("Name", "New Name for the Channel")] string name)
    {
        await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);
        
        var channelHandleInDb = DatabaseHandleHelper.GetHandleFromEnum(channelHandle);
        
        if (channelHandleInDb == "NoChannel")
        {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Error, Invalid Channel Handle!"));
            return;
        }
        
        var sqlite = SqLiteConnectionHelper.GetSqLiteConnection();
        
        var hasUpdated = await sqlite.ExecuteAsync("UPDATE StatsChannelCustomNamesIndex SET CustomName = @Name WHERE GuildId = @GuildId AND ChannelHandle = @ChannelHandle", new { Name = name, GuildId = ctx.Guild.Id, ChannelHandle = channelHandleInDb });
        
        if (hasUpdated == 0)
        {
            var renameRecord = new StatsChannelCustomNamesIndex()
            {
                GuildId = ctx.Guild.Id,
                ChannelHandle = channelHandleInDb,
                CustomName = name
            };

            await sqlite.InsertAsync(renameRecord);
        }


        
        await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Done!"));
    }

    public enum ChannelHandleEnum
    {
        [ChoiceName("Category Channel")]
        CategoryChannel,
        [ChoiceName("Member Counter Channel")]
        MemberChannel,
        [ChoiceName("Bot Counter Channel")]
        BotChannel,
        [ChoiceName("Team Counter Channel")]
        TeamChannel,
        NoChannel
    }

}