namespace YTest.MTP.PipeProtocol;

internal sealed class VoidResponse : IResponse
{
    public static readonly VoidResponse CachedInstance = new();
}
