﻿using System.Diagnostics.CodeAnalysis;
using Content.Shared.CCVar;
using Content.Shared.Chat.V2;
using Content.Shared.Chat.V2.Components;
using Robust.Shared.Configuration;

namespace Content.Client.Chat.V2;

public sealed class ClientLocalChatSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _configurationManager = default!;

    public bool SendMessage(EntityUid speaker, string message, [NotNullWhen(false)] out string? reason)
    {
        // Sanity check: if you can't chat you shouldn't be chatting.
        if (!TryComp<LocalChattableComponent>(speaker, out var chattable))
        {
            // TODO: Add locstring
            reason = "You can't talk in local chat";

            return false;
        }

        var messageMaxLen = _configurationManager.GetCVar(CCVars.ChatMaxMessageLength);

        if (message.Length > messageMaxLen)
        {
            reason = Loc.GetString("chat-manager-max-message-length",
                ("maxMessageLength", messageMaxLen));

            return false;
        }

        RaiseNetworkEvent(new LocalChatAttemptedEvent(GetNetEntity(speaker), message));

        reason = null;

        return true;
    }
}
