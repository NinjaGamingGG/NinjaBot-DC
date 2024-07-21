using DSharpPlus;
using DSharpPlus.Entities;

namespace LoungeSystemPlugin.PluginHelper;

/// <summary>
/// Provides methods to build interface messages for the lounge system plugin.
/// </summary>
public static class InterfaceMessageBuilder
{
    /// <summary>
    /// Returns a DiscordMessageBuilder instance with pre-configured message content and button components.
    /// </summary>
    /// <param name="client">The DiscordClient instance used to construct button components.</param>
    /// <param name="messageContent">The message content to be set for the DiscordMessageBuilder instance.</param>
    /// <returns>A DiscordMessageBuilder instance with pre-configured message content and button components.</returns>
    public static DiscordMessageBuilder GetBuilder(DiscordClient client,string messageContent)
    {
       return new DiscordMessageBuilder()
                .WithContent(messageContent)
                .AddComponents([
                    new DiscordButtonComponent(DiscordButtonStyle.Secondary, "lounge_rename_button",
                          "Rename",false, new DiscordComponentEmoji( DiscordEmoji.FromName(client, ":black_nib:"))),
                    new DiscordButtonComponent(DiscordButtonStyle.Secondary, "lounge_resize_button",
                        "Resize", false, new DiscordComponentEmoji(DiscordEmoji.FromName(client, ":busts_in_silhouette:"))),
                    new DiscordButtonComponent(DiscordButtonStyle.Secondary, "lounge_trust_button",
                        "Trust", false, new DiscordComponentEmoji(DiscordEmoji.FromName(client, ":people_hugging:")))
                ])
                .AddComponents([
                    new DiscordButtonComponent(DiscordButtonStyle.Secondary, "lounge_claim_button",
                        "Claim", false, new DiscordComponentEmoji(DiscordEmoji.FromName(client, ":triangular_flag_on_post:"))),
                    new DiscordButtonComponent(DiscordButtonStyle.Secondary, "lounge_kick_button",
                        "Kick", false, new DiscordComponentEmoji(DiscordEmoji.FromName(client, ":athletic_shoe:"))),
                    new DiscordButtonComponent(DiscordButtonStyle.Secondary, "lounge_un-trust_button",
                        "Un-Trust", false, new DiscordComponentEmoji(DiscordEmoji.FromName(client, ":bust_in_silhouette:")))
                ])
                .AddComponents([
                    new DiscordButtonComponent(DiscordButtonStyle.Danger, "lounge_lock_button",
                        "Un/Lock", false, new DiscordComponentEmoji(DiscordEmoji.FromName(client, ":lock:"))),
                    new DiscordButtonComponent(DiscordButtonStyle.Danger, "lounge_ban_button",
                        "Ban", false, new DiscordComponentEmoji(DiscordEmoji.FromName(client, ":judge:"))),
                    new DiscordButtonComponent(DiscordButtonStyle.Danger, "lounge_delete_button",
                        "Delete", false, new DiscordComponentEmoji(DiscordEmoji.FromName(client, ":put_litter_in_its_place:")))
                ]);
    }
}