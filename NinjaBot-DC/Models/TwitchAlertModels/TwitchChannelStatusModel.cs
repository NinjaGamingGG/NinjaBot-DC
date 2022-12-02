using Newtonsoft.Json;

namespace NinjaBot_DC.Models;

public record ChannelData(
    [property: JsonProperty("id")] string Id,   //Id of the Twitch Stream or vod
    [property: JsonProperty("user_id")] string UserId,  //Id of the Twitch Channel
    [property: JsonProperty("user_login")] string UserLogin,    //Account Name of the User
    [property: JsonProperty("user_name")] string UserName,  //Display Name of the User
    [property: JsonProperty("game_id")] string GameId,  //Id of the Game that is streamed
    [property: JsonProperty("game_name")] string GameName,  //Name of the game that is streamed
    [property: JsonProperty("type")] string Type,   //The type of stream. Value is "live" or empty string
    [property: JsonProperty("title")] string Title, //Streams title
    [property: JsonProperty("viewer_count")] int ViewerCount,   //How much users are watching the stream
    [property: JsonProperty("started_at")] DateTime StartedAt,  //UTC date and time when the stream began
    [property: JsonProperty("language")] string Language,   //The language that the stream uses
    [property: JsonProperty("thumbnail_url")] string ThumbnailUrl,  //A URL to an image of a frame from the last 5 minutes of the stream
    [property: JsonProperty("tag_ids")] IReadOnlyList<string> TagIds,   //The IDs of the tags applied to the stream
    [property: JsonProperty("is_mature")] bool IsMature //Indicates whether the stream is meant for mature audiences.
);

public record Pagination(
    [property: JsonProperty("cursor")] string Cursor    //The cursor used to get the next page of results
);

public record TwitchChannelStatusModel(
    [property: JsonProperty("data")] IReadOnlyList<ChannelData> Data,   //The list of streams.
    [property: JsonProperty("pagination")] Pagination Pagination    //The information used to page through the list of results
);


