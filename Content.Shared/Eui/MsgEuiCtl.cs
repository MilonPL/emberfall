﻿using Lidgren.Network;
using Robust.Shared.Network;

namespace Content.Shared.Eui;

/// <summary>
///     Sent server -> client to signal that the client should open an EUI.
/// </summary>
public sealed class MsgEuiCtl : NetMessage
{
    public override MsgGroups MsgGroup => MsgGroups.Command;

    public CtlType Type;
    public string OpenType = string.Empty;
    public uint Id;

    public override void ReadFromBuffer(NetIncomingMessage buffer)
    {
        Id = buffer.ReadUInt32();
        Type = (CtlType) buffer.ReadByte();
        switch (Type)
        {
            case CtlType.Open:
                OpenType = buffer.ReadString();
                break;
        }
    }

    public override void WriteToBuffer(NetOutgoingMessage buffer)
    {
        buffer.Write(Id);
        buffer.Write((byte) Type);
        switch (Type)
        {
            case CtlType.Open:
                buffer.Write(OpenType);
                break;
        }
    }

    public enum CtlType : byte
    {
        Open,
        Close
    }
}