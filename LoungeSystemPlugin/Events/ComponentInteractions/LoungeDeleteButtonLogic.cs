using Dapper;
using Dapper.Contrib.Extensions;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using LoungeSystemPlugin.PluginHelper;
using LoungeSystemPlugin.Records;
using MySqlConnector;
using Serilog;

namespace LoungeSystemPlugin.Events.ComponentInteractions;

public static class LoungeDeleteButtonLogic
{
    public static async Task ButtonInteracted(ComponentInteractionCreatedEventArgs eventArgs, DiscordMember owningMember)
    {
        await eventArgs.Interaction.DeferAsync();

        var existsAsOwner = await LoungeOwnerCheck.IsLoungeOwnerAsync(owningMember, eventArgs.Channel, eventArgs.Guild);
        
        //Only non owners can delete
        if (!existsAsOwner)
            return;

        var connectionString = LoungeSystemPlugin.MySqlConnectionHelper.GetMySqlConnectionString();
        List<LoungeDbRecord> loungeDbRecordList;
        try
        {
            await using var mySqlConnection = new MySqlConnection(connectionString);
            await mySqlConnection.OpenAsync();

            var loungeDbRecordEnumerable = await mySqlConnection.QueryAsync<LoungeDbRecord>("SELECT * FROM LoungeIndex WHERE GuildId = @GuildId AND ChannelId= @ChannelId", new {GuildId = eventArgs.Guild.Id, ChannelId = eventArgs.Channel.Id});
            await mySqlConnection.CloseAsync();
            loungeDbRecordList = loungeDbRecordEnumerable.ToList();
        }
        catch (MySqlException ex)
        {
            Log.Error(ex, "Error while querying lounge-db-records in the LoungeSystem Delete Button Logic. Guild: {GuildName}/{GuildId}, Channel: {ChannelName}/{ChannelId}",
                eventArgs.Guild.Name, eventArgs.Guild.Id, eventArgs.Channel.Name,eventArgs.Channel.Id);
            return;
        }


        if (loungeDbRecordList.Count == 0)
        {
            Log.Error("No LoungeDbRecord from Lounge Index on Guild {GuildId} at Channel {ChannelId}", eventArgs.Guild.Id, eventArgs.Channel.Id);
            return;
        }

        var loungeChannel = eventArgs.Channel;

        var afkChannel = await eventArgs.Guild.GetAfkChannelAsync();
        
        foreach (var loungeChannelUser in loungeChannel.Users)
        {
            await loungeChannelUser.PlaceInAsync(afkChannel);
        }

        await loungeChannel.DeleteAsync();
        bool deleteSuccess;
        try
        {
            await using var mySqlConnection =  new MySqlConnection(connectionString);
            await mySqlConnection.OpenAsync();
            
            deleteSuccess = await mySqlConnection.DeleteAsync(loungeDbRecordList.First());
            await mySqlConnection.CloseAsync();
        }
        catch (MySqlException ex)
        {
            Log.Error(ex, "Unable to delete lounge database record in LoungeSystem. Guild: {GuildName}/{GuildId}, Channel: {ChannelName}/{ChannelId}",
                eventArgs.Guild.Name, eventArgs.Guild.Id, eventArgs.Channel.Name,eventArgs.Channel.Id);
            return;
        }
        
        if (deleteSuccess == false)
            Log.Error("Unable to delete the Sql Record for Lounge {LoungeName} with the Id {LoungeId} in Guild {GuildId}",loungeChannel.Name, eventArgs.Channel.Id, eventArgs.Guild.Id);

    }
    
}