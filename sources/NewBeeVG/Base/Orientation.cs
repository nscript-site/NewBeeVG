namespace NewBeeVG;

/// <summary>
/// Defines vertical or horizontal orientation.
/// </summary>
public enum Orientation
{
    /// <summary>
    /// Horizontal orientation.
    /// </summary>
    Horizontal,

    /// <summary>
    /// Vertical orientation.
    /// </summary>
    Vertical,
}

/// <summary>
/// Specify options for snap point alignment relative to an edge. Which edge depends on the orientation of the object where the alignment is applied
/// </summary>
public enum SnapPointsAlignment
{
    /// <summary>
    /// Use snap points grouped closer to the orientation edge.
    /// </summary>
    Near,

    /// <summary>
    /// Use snap points that are centered in the orientation.
    /// </summary>
    Center,

    /// <summary>
    /// Use snap points grouped farther from the orientation edge.
    /// </summary>
    Far
}