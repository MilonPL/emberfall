﻿using Content.Server.MassMedia.Systems;
using Robust.Shared.Audio;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server.MassMedia.Components;

[RegisterComponent, AutoGenerateComponentPause]
[Access(typeof(NewsSystem))]
public sealed partial class NewsWriterComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite), DataField]
    public bool PublishEnabled;

    [ViewVariables(VVAccess.ReadWrite), DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan NextPublish;

    [ViewVariables(VVAccess.ReadWrite), DataField]
    public float PublishCooldown = 20f;

    [DataField]
    public SoundSpecifier NoAccessSound = new SoundCollectionSpecifier("AccessDeniedSound");

    [DataField]
    public SoundSpecifier ConfirmSound = new SoundCollectionSpecifier("NewsWriterConfirmSound");

    /// <summary>
    /// This stores the working title of the current article
    /// </summary>
    [DataField, ViewVariables]
    public string DraftTitle = "";

    /// <summary>
    /// This stores the working content of the current article
    /// </summary>
    [DataField, ViewVariables]
    public string DraftContent = "";
}
