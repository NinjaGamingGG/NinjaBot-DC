﻿using System.Net;
using Dapper;
using Dapper.Contrib.Extensions;
using DSharpPlus;
using Newtonsoft.Json;
using NinjaBot_DC.Models;


namespace NinjaBot_DC.Extensions;

public static class TwitchAlerts
{
    private static readonly DiscordClient DiscordClient; //Discord Client used for command execution
    private static readonly PeriodicTimer StreamerRoleUpdateTimer = new PeriodicTimer(TimeSpan.FromSeconds(10)); //Refresh-Rate for updating Twitch Channel Info

    private static readonly HttpClientHandler HcHandle = new HttpClientHandler(); //HttpClientHandler used for updating Channel Info

    private static readonly string ClientId; //Twitch Client Id Key
    private static readonly string AuthKey; //Twitch Oauth Key
    

    static TwitchAlerts()
    {
        //Load ClientId & AuthKey from Settings File
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("config.json", optional: false, reloadOnChange: true)
            .Build();
        
        ClientId = configuration.GetValue<string>("twitch-extension:clientId");
        AuthKey = configuration.GetValue<string>("twitch-extension:authKey");
        
        //Get the Discord Client
        DiscordClient = Worker.GetServiceDiscordClient();
    }
    
    public static async Task InitExtensionAsync()
    {
        //Start the main task of this Extension
        await UpdateCreators();
    }

    private static async Task UpdateCreators()
    {
        //Wait for next task execution
        while (await StreamerRoleUpdateTimer.WaitForNextTickAsync())
        {
            //Query all registered creator channels from sqlite db
            var creatorChannels = 
               await Worker.SqLiteConnection.QueryAsync<TwitchCreatorSocialMediaChannelModel>(
                    "SELECT * FROM TwitchCreatorSocialMediaChannelIndex WHERE Platform = 'twitch'");

            //Convert to list so we can iterate trough it wit for loop
            var creatorChannelsAsList = creatorChannels.ToList();
            
            //Iterate trough list
            for (var i = 0; i < creatorChannelsAsList.Count; i++)
            {
                //Query if channel is live
                var channelData = await IsTwitchChannelOnline(creatorChannelsAsList[i].SocialMediaChannel);
                
                //Check if channelData contains elements
                if (channelData == null || !channelData.Data.Any())
                    continue;

                //Handle the channel data
                await HandleChannelData(channelData, creatorChannelsAsList[i]);
            }

        }
    }

    private static async Task HandleChannelData(TwitchChannelStatusModel channelData, TwitchCreatorSocialMediaChannelModel creatorChannel)
    {
        //Iterate trough channel Data
        for (var i = 0; i < channelData.Data.Count; i++)
        {
            //Check if the channel is live
            if (channelData.Data[i].Type != "live")
                continue;
            
            //Create local variables from record
            var channelName = channelData.Data[i].UserName;
            var streamId = channelData.Data[i].Id;
            var channelId = channelData.Data[i].UserId;

            //Create cache Record
            var streamCacheRecord = new TwitchStreamCacheModel()
                {Id = streamId, ChannelId = channelId, ChannelName = channelName};

            //Check if Record Exists
            var recordExists = await Worker.SqLiteConnection.QueryAsync(
                $"SELECT * FROM TwitchStreamCacheIndex WHERE (ChannelId = {channelId} AND Id = {streamId} AND ChannelName = '{channelName}')");
            
            //If yes we already pushed a notification and can just continue
            if (recordExists.Any())
                continue;

            //If not the channel just went live and we have to Insert into sqlite db and push a notification
            await Worker.SqLiteConnection.InsertAsync(streamCacheRecord);

            //push the notification
            await PushDiscordNotification(channelData.Data[i], creatorChannel);
        }
    }

    private static async Task PushDiscordNotification(ChannelData channelData,TwitchCreatorSocialMediaChannelModel creatorChannel)
    {
        //Create local variables
        var guildId = creatorChannel.GuildId;
        var roleTag = creatorChannel.RoleTag;
            
        //Get a list of linked output channels for this guild and role-tag
        var outPutChannels = await Worker.SqLiteConnection.QueryAsync<TwitchDiscordChannelModel>($"SELECT * FROM TwitchDiscordChannelIndex WHERE (GuildId = {guildId}) AND RoleTag = '{roleTag}'");

        //Convert to list so we can loop over with for
        var outPutChannelsAsList = outPutChannels.ToList();
        
        //If none are linked return
        if (!outPutChannelsAsList.Any())
            return;
        
        //get the guild from discord client
        var discordGuild = await DiscordClient.GetGuildAsync(guildId);
        
        //Iterate over linked channels
        for (var i = 0; i < outPutChannelsAsList.Count; i++)
        {
            //get the output channel
            var channel = discordGuild.GetChannel(outPutChannelsAsList[i].ChannelId);

            //get the role mention
            var roleMention = discordGuild.GetRole(1041099026717745222).Mention;

            //Push the message to the Channel
            // ReSharper disable once StringLiteralTypo
            await channel.SendMessageAsync($"Hey {roleMention}, {channelData.UserName} ist jetzt live! Schaut doch mal vorbei: https://twitch.tv/{creatorChannel.SocialMediaChannel}");
        }

    }

    private static async Task<TwitchChannelStatusModel?> IsTwitchChannelOnline(string channelName)
    {
        //Create new HttpClient from Handle
        using (var httpClient = new HttpClient(HcHandle, false))
        {
            //Set Request Headers and Timeout
            httpClient.DefaultRequestHeaders.Add("Client-ID", ClientId);
            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", AuthKey);
            httpClient.Timeout = TimeSpan.FromSeconds(5);
            
            //Get the Response
            using (var response = await httpClient.GetAsync($"https://api.twitch.tv/helix/streams?user_login={channelName}"))
            {
                //If we get a bad response return
                if (response.StatusCode != HttpStatusCode.OK)
                    return null;
                
                //Get the Response Content
                var jsonString = await response.Content.ReadAsStringAsync();
                
                //Deserialize the json Response
                var myDeserializedClass = JsonConvert.DeserializeObject<TwitchChannelStatusModel>(jsonString);
                

                //Return the Result
                return myDeserializedClass;
            }
        }
    }
}