using Dapper;
using Dapper.Contrib.Extensions;
using DSharpPlus;

using DSharpPlus.EventArgs;
using NinjaBot_DC.Models;

namespace NinjaBot_DC.Extensions;

public static class LoungeSystem
{
    public static async Task VoiceStateUpdated_ChanelEnter(DiscordClient client, VoiceStateUpdateEventArgs eventArgs)
    {
        if ( eventArgs.Channel == null || eventArgs.Channel.Name != "╠👥»Join 2 Create")
            return;


        var newChannel = await eventArgs.Channel.Guild.CreateVoiceChannelAsync($"╠🥳» {eventArgs.User.Username}'s Lounge",
            eventArgs.Channel.Parent, 128000, position: 9999, user_limit: 4);
            
        if (newChannel == null)
            return;
        
        eventArgs.Channel.Guild.Members.TryGetValue(eventArgs.User.Id, out var discordMember);
        if (discordMember == null)
            return;
        
        var newModel = new Models.LoungeDbModel {ChannelId = newChannel.Id, OwnerId =discordMember.Id, GuildId = eventArgs.Guild.Id};
        
        var updated = await Worker.SqLiteConnection.InsertAsync(newModel);

        if (updated == 0)
        {
            await Task.Run(async () =>
            {
                await newChannel.DeleteAsync();
            });
            
            return;
        }
        
        await  Task.Run(async () =>
        {
            await discordMember.PlaceInAsync(newChannel);
        });

        await  Task.Run(async () =>
        {
            await newChannel.AddOverwriteAsync(discordMember, Permissions.ManageChannels);
        });
        
        await  Task.Run(async () =>
        {
            await newChannel.AddOverwriteAsync(discordMember, Permissions.AccessChannels);
        });
        
        await  Task.Run(async () =>
        {
            await newChannel.AddOverwriteAsync(discordMember, Permissions.UseVoice);
        });
        
                    
        

    }
    
    public static async Task VoiceStateUpdated_ChanelLeave(DiscordClient client, VoiceStateUpdateEventArgs eventArgs)
    {
        if ( eventArgs.Before == null)
            return;
        

        var loungeList = await Worker.SqLiteConnection.GetAllAsync<LoungeDbModel>();

        foreach (var loungeModel in loungeList)
        {
            if (loungeModel.ChannelId != eventArgs.Before.Channel.Id)
                continue;
            
            if (eventArgs.Before.Channel.Users.Count != 0)
                return;

            await Task.Run(async () =>
            {
                await eventArgs.Before.Channel.DeleteAsync();
                await Worker.SqLiteConnection.DeleteAsync(loungeModel);
            });
        }
        

    }

    public static async Task StartupCleanup(DiscordClient discordClient)
    {
        var loungeDbModels = await Worker.SqLiteConnection.GetAllAsync<LoungeDbModel>();

        foreach (var loungeDbModel in loungeDbModels)
        {
            var loungeChannel = await discordClient.GetChannelAsync(loungeDbModel.ChannelId);
            var guild = await discordClient.GetGuildAsync(loungeDbModel.GuildId);
            
            //For some reason in this case the user count has to be queried by the guild not the channel because that would often result in an exception 
            if (guild.Channels[loungeDbModel.ChannelId].Users.Count == 0)
                await loungeChannel.DeleteAsync();

            await Worker.SqLiteConnection.DeleteAsync(loungeDbModel);
        }
    }
}