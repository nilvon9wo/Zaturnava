using Net.NowhereAtAll.Zaturnava.Application.Chat;
using Net.NowhereAtAll.Zaturnava.Contracts;

namespace Net.NowhereAtAll.Zaturnava.Server.Endpoints;

/// <summary>What is said at a table, translated to and from the contracts.</summary>
internal static class ChatViews
{
    public static ChatMessageDto MessageOf(ChatMessage message) =>
        new(message.Number, message.From.Value, NameOf(message.Channel), message.Text, message.At);

    /// <exception cref="ArgumentException">There is no such channel.</exception>
    public static ChatChannel ChannelOf(string name) =>
        name switch
        {
            ChatChannels.Table => ChatChannel.Table,
            ChatChannels.Team => ChatChannel.Team,
            _ => throw new ArgumentException(
                $"There is no '{name}' channel; it must be {ChatChannels.Table} or {ChatChannels.Team}.",
                nameof(name)
            ),
        };

    private static string NameOf(ChatChannel channel) =>
        channel is ChatChannel.Team ? ChatChannels.Team : ChatChannels.Table;
}
