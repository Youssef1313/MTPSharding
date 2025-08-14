namespace YTest.MTP.PipeProtocol;

internal sealed record DiscoveredTestMessage(string Uid, string DisplayName, string? FilePath, int? LineNumber, string? Namespace, string? TypeName, string? MethodName, TestMetadataProperty[] Traits);

internal sealed record TestMetadataProperty(string? Key, string? Value);

internal sealed record DiscoveredTestMessages(string? ExecutionId, string? InstanceId, DiscoveredTestMessage[] DiscoveredMessages) : IRequest;
