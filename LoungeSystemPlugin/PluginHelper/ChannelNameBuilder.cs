using Dapper;
using LoungeSystemPlugin.Records;
using MySqlConnector;

namespace LoungeSystemPlugin.PluginHelper;

public static class ChannelNameBuilder
{
    public static async Task<string> BuildAsync(ulong guildId, ulong channelId, string customNameContent)
    {
        var connectionString = LoungeSystemPlugin.MySqlConnectionHelper.GetMySqlConnectionString();
        var mySqlConnection = new MySqlConnection(connectionString);
        await mySqlConnection.OpenAsync();
        
        var channelConfigurations = await mySqlConnection.QueryAsync<LoungeSystemConfigurationRecord>("SELECT * FROM LoungeSystemConfigurationIndex WHERE GuildId = @GuildId ", new { GuildId = guildId});
        
        var channelConfigurationList = channelConfigurations.ToList();

        var channelRecords = await mySqlConnection.QueryAsync<LoungeDbRecord>("SELECT * FROM LoungeIndex WHERE GuildId = @GuildId AND ChannelId = @ChannelId", new {GuildId = guildId, ChannelId = channelId});

        var channelRecordsAsList = channelRecords.ToList();

        //If the channel record list doesn't contain any elements this is called from VoiceStateUpdated Event and the Origin ChannelId is the Channel ID.
        //If it contains elements method is invoked from the modal submitted logic, and we query the Origin Channel from the Db Record    
        var originChannelId = channelRecordsAsList.Count == 0 ? channelId : channelRecordsAsList.First().OriginChannel;
        
        var channelNamePattern = string.Empty;

        var separatorPattern = string.Empty;
        var decoratorPrefix = string.Empty;
        var decoratorEmoji = string.Empty;
        var decoratorDecal = string.Empty;

        var nameReplacementRecord = await mySqlConnection.QueryAsync<LoungeMessageReplacement>("SELECT * FROM LoungeMessageReplacementIndex WHERE GuildId= @GuildId AND ChannelId = @ChannelId", new {GuildId = guildId, ChannelId = originChannelId});

        await mySqlConnection.CloseAsync();
        
        var loungeMessageReplacementsAsArray = nameReplacementRecord as LoungeMessageReplacement[] ?? nameReplacementRecord.ToArray();
        if (loungeMessageReplacementsAsArray.Any())
        {
            foreach (var replacement in loungeMessageReplacementsAsArray)
            {
                switch (replacement.ReplacementHandle)
                {
                    case"Separator":
                        separatorPattern = replacement.ReplacementValue;
                        break;

                    case"Decorator_Decal":
                        decoratorDecal = replacement.ReplacementValue;
                        break;
                    
                    case"Decorator_Emoji":
                        decoratorEmoji = replacement.ReplacementValue;
                        break;
                    
                    case"Decorator_Prefix":
                        decoratorPrefix = replacement.ReplacementValue;
                        break;
                }

            }
        }
        
        if (!ReferenceEquals(separatorPattern, null) && separatorPattern.Contains("{Decorator_Decal}"))
            separatorPattern = separatorPattern.Replace("{Decorator_Decal}", decoratorDecal);
        
        if (!ReferenceEquals(separatorPattern, null) && separatorPattern.Contains("{Decorator_Emoji}"))
            separatorPattern = separatorPattern.Replace("{Decorator_Emoji}", decoratorEmoji);
        if (!ReferenceEquals(separatorPattern, null) && separatorPattern.Contains("{Decorator_Prefix}"))
            separatorPattern = separatorPattern.Replace("{Decorator_Prefix}", decoratorPrefix);

        foreach (var channelConfig in channelConfigurationList.Where(channelConfig => originChannelId == channelConfig.TargetChannelId))
        {
            channelNamePattern = channelConfig.LoungeNamePattern;
            
            if (!ReferenceEquals(channelNamePattern, null) && channelNamePattern.Contains("{Separator}"))
                channelNamePattern = channelNamePattern.Replace("{Separator}", separatorPattern);
        
            if (!ReferenceEquals(channelNamePattern, null) && channelNamePattern.Contains("{Custom_Name}"))
                channelNamePattern = channelNamePattern.Replace("{Custom_Name}", customNameContent);
            
            break;
        }

        return channelNamePattern ?? string.Empty;
    }
}