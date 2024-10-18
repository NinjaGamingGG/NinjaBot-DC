﻿using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using LoungeSystemPlugin.PluginHelper;
using LoungeSystemPlugin.Records.Cache;
using Newtonsoft.Json;
using NRedisStack;
using NRedisStack.RedisStackCommands;
using Serilog;
using StackExchange.Redis;

namespace LoungeSystemPlugin.Events.ComponentInteractions.LoungeSetupUi;

public static class LoungeSetupInterfaceSelector
{
    internal static async Task SelectionMade(ComponentInteractionCreatedEventArgs eventArgs, DiscordMember member)
    {
        //Check if User has Admin Permissions
        if (!member.Permissions.HasPermission(DiscordPermissions.Administrator))
        {
            await eventArgs.Interaction.CreateResponseAsync(DiscordInteractionResponseType.UpdateMessage,LoungeSetupUiHelper.NoPermissionsResponseBuilder);
            return;
        }
        
        //The Selection that was made. The Component is set to allow only one option to be selected, so we just get the element at the first position
        var selection = eventArgs.Interaction.Data.Values[0];
        
        if (ReferenceEquals(eventArgs.Interaction.Message, null))
            return;
        
        var messageId = eventArgs.Interaction.Message.Id.ToString();

        try
        {
            var redisConnection = await ConnectionMultiplexer.ConnectAsync(LoungeSystemPlugin.RedisConnectionString);
            var redisDatabase = redisConnection.GetDatabase(LoungeSystemPlugin.RedisDatabase);
            var entryKey = new RedisKey(messageId);

            var json = redisDatabase.JSON();
            var redisResult = json.Get(entryKey, path:"$").ToString().TrimEnd(']').TrimStart('[');
            var deserializedRecord = JsonConvert.DeserializeObject<LoungeSetupRecord>(redisResult);
            if (deserializedRecord is null)
                return;
            
            //Handle the value of the Selection
            switch (selection)
            {
                case ("separate_interface"):
                    await HandleSeparateInterface(eventArgs, deserializedRecord, redisDatabase, entryKey, json, messageId);
                    break;
            
                case ("internal_interface"):
                    await eventArgs.Interaction.CreateResponseAsync(DiscordInteractionResponseType.UpdateMessage,
                        LoungeSetupUiHelper.LoungeSetupComplete);
                    LoungeSetupUiHelper.CompleteSetup(deserializedRecord, eventArgs.Guild.Id);
                    break;
            
                default:
                    await eventArgs.Interaction.CreateResponseAsync(DiscordInteractionResponseType.ChannelMessageWithSource,
                        LoungeSetupUiHelper.InteractionFailedResponseBuilder("The selection made was Invalid, please try again"));
                    break;
            
            }
            
        }
        catch (Exception ex)
        {
            Log.Error(ex,"[{PluginName}] Unable to update LoungeSetupRecord for ui message {messageId}",LoungeSystemPlugin.GetStaticPluginName(), messageId);
            await eventArgs.Interaction.CreateResponseAsync(DiscordInteractionResponseType.UpdateMessage, LoungeSetupUiHelper.InteractionFailedResponseBuilder($"Unable to update LoungeSetupRecord for ui message {messageId}"));
        }

    }

    private static async Task HandleSeparateInterface(ComponentInteractionCreatedEventArgs eventArgs,
        LoungeSetupRecord deserializedRecord, IDatabase redisDatabase, RedisKey entryKey, JsonCommands json, string messageId)
    {
        await eventArgs.Interaction.CreateResponseAsync(DiscordInteractionResponseType.UpdateMessage,
            LoungeSetupUiHelper.InterfaceSelectedResponseBuilder);
                    
        var newLoungeSetupRecord = deserializedRecord with { HasInternalInterface = false };
        
        var remainingTimeToLive = redisDatabase.KeyTimeToLive(entryKey);
             
        json.Set(messageId, "$", newLoungeSetupRecord);
            
        redisDatabase.KeyExpire(messageId, remainingTimeToLive);
    }
}