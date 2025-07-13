namespace YTest.MTP.PipeProtocol;

public sealed class DiscoveredTestInformation
{
    public DiscoveredTestInformation(string uid, string displayName)
    {
        Uid = uid;
        DisplayName = displayName;
    }

    public string Uid { get; }

    public string DisplayName { get; }
}
