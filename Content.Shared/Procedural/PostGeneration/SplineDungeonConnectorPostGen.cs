namespace Content.Shared.Procedural.PostGeneration;

/// <summary>
/// Connects dungeons via points that get subdivided.
/// </summary>
public sealed partial class SplineDungeonConnectorPostGen : IDunGenLayer
{
    /// <summary>
    /// Will divide the distance between the start and end points so that no subdivision is more than these metres away.
    /// </summary>
    [DataField]
    public int DivisionDistance = 5;

    /// <summary>
    /// How much each subdivision can vary from the middle.
    /// </summary>
    [DataField]
    public float VarianceMax = 0.50f;
}
