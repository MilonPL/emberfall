using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared._Emberfall.Medical.CPR.Events;

// ReSharper disable InconsistentNaming
[Serializable, NetSerializable]
public sealed partial class CPRDoAfterEvent : SimpleDoAfterEvent;
