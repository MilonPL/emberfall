﻿using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Server.GameTicking.Prototypes;

/// <summary>
/// Prototype for a lobby background the game can choose.
/// </summary>
[Prototype("lobbyBackground")]
public readonly record struct LobbyBackgroundPrototype : IPrototype
{
    /// <inheritdoc/>
    [IdDataField]
    public string ID { get; } = default!;

    /// <summary>
    /// The sprite to use as the background. This should ideally be 1920x1080.
    /// </summary>
    [DataField("background", required: true)]
    public readonly ResourcePath Background = default!;
}
