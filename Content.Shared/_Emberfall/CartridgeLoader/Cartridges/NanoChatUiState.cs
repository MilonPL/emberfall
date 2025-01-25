// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.
//
// This Source Code Form is "Incompatible With Secondary
// Licenses", as defined by the Mozilla Public License, v. 2.0.

using Robust.Shared.Serialization;

namespace Content.Shared._Emberfall.CartridgeLoader.Cartridges;

[Serializable, NetSerializable]
public sealed class NanoChatUiState : BoundUserInterfaceState
{
    public readonly Dictionary<uint, NanoChatRecipient> Recipients = new();
    public readonly Dictionary<uint, List<NanoChatMessage>> Messages = new();
    public readonly uint? CurrentChat;
    public readonly uint OwnNumber;
    public readonly int MaxRecipients;
    public readonly bool NotificationsMuted;

    public NanoChatUiState(
        Dictionary<uint, NanoChatRecipient> recipients,
        Dictionary<uint, List<NanoChatMessage>> messages,
        uint? currentChat,
        uint ownNumber,
        int maxRecipients,
        bool notificationsMuted)
    {
        Recipients = recipients;
        Messages = messages;
        CurrentChat = currentChat;
        OwnNumber = ownNumber;
        MaxRecipients = maxRecipients;
        NotificationsMuted = notificationsMuted;
    }
}
