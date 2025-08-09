namespace YTest.MTP.PipeProtocol;

/// <summary>
/// Represents the information about a discovered test.
/// This type is returned by <see cref="MTPPipeDiscoverer.DiscoverTestsAsync"/>
/// </summary>
public sealed class DiscoveredTestInformation
{
    internal DiscoveredTestInformation(string uid, string displayName)
    {
        Uid = uid;
        DisplayName = displayName;
    }

    /// <summary>
    /// The unique identifier of the test. In many cases it's a Guid, but that's not a guarantee!
    /// </summary>
    public string Uid { get; }

    /// <summary>
    /// The display name of the test.
    /// </summary>
    public string DisplayName { get; }
}
