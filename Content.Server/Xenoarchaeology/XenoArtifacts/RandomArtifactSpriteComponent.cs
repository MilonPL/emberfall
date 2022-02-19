﻿namespace Content.Server.Xenoarchaeology.XenoArtifacts;

[RegisterComponent]
public sealed class RandomArtifactSpriteComponent : Component
{
    [DataField("minSprite")]
    public int MinSprite = 1;

    [DataField("maxSprite")]
    public int MaxSprite = 14;

    [DataField("activationTime")]
    public double ActivationTime = 2.0;

    public TimeSpan? ActivationStart;
}
