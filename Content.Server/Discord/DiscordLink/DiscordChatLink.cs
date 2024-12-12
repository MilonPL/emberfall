﻿using Content.Server.Chat.Managers;
using Content.Shared.CCVar;
using Content.Shared.Chat;
using Discord.WebSocket;
using Robust.Shared.Asynchronous;
using Robust.Shared.Configuration;

namespace Content.Server.Discord.DiscordLink;

public sealed class DiscordChatLink
{
    [Dependency] private DiscordLink _discordLink = default!;
    [Dependency] private IConfigurationManager _configurationManager = default!;
    [Dependency] private readonly IChatManager _chatManager = default!;
    [Dependency] private readonly ITaskManager _taskManager = default!;

    private ulong? _oocChannelId;
    private ulong? _adminChannelId;

    public void Initialize()
    {
        _discordLink.OnMessageReceived += OnMessageReceived;

        _configurationManager.OnValueChanged(CCVars.OocDiscordChannelId, OnOocChannelIdChanged, true);
        _configurationManager.OnValueChanged(CCVars.AdminChatDiscordChannelId, OnAdminChannelIdChanged, true);
    }

    public void Shutdown()
    {
        _discordLink.OnMessageReceived -= OnMessageReceived;

        _configurationManager.UnsubValueChanged(CCVars.OocDiscordChannelId, OnOocChannelIdChanged);
        _configurationManager.UnsubValueChanged(CCVars.AdminChatDiscordChannelId, OnAdminChannelIdChanged);
    }

    private void OnOocChannelIdChanged(string channelId)
    {
        if (string.IsNullOrEmpty(channelId))
        {
            _oocChannelId = null;
            return;
        }

        _oocChannelId = ulong.Parse(channelId);
    }

    private void OnAdminChannelIdChanged(string channelId)
    {
        if (string.IsNullOrEmpty(channelId))
        {
            _adminChannelId = null;
            return;
        }

        _adminChannelId = ulong.Parse(channelId);
    }

    private void OnMessageReceived(SocketMessage message)
    {
        if (message.Author.IsBot)
            return;

        _taskManager.RunOnMainThread(() =>
        {
            if (message.Channel.Id == _oocChannelId)
            {
                _chatManager.SendHookOOC(message.Author.GlobalName, message.Content);
            }
            else if (message.Channel.Id == _adminChannelId)
            {
                _chatManager.SendHookAdmin(message.Author.GlobalName, message.Content);
            }
        });
    }

    public void SendMessage(string message, string author, ChatChannel channel)
    {
        var channelId = channel switch
        {
            ChatChannel.OOC => _oocChannelId,
            ChatChannel.AdminChat => _adminChannelId,
            _ => throw new InvalidOperationException("Channel not linked to Discord."),
        };

        if (channelId == null)
        {
            // Configuration not set up. Ignore.
            return;
        }

        // @ and < are both problematic for discord due to pinging. / is sanitized solely to kneecap links to murder embeds via blunt force
        message = message.Replace("@", "\\@").Replace("<", "\\<").Replace("/", "\\/");

        _ = _discordLink.SendMessageAsync(channelId.Value, $"**{channel.ToString().ToUpper()}**: `{author}`: {message}");
    }
}
