namespace YTest.MTP.PipeProtocol;

internal sealed record DiscoveredTestMessage(string Uid, string DisplayName);

internal sealed record DiscoveredTestMessages(string? ExecutionId, string? InstanceId, DiscoveredTestMessage[] DiscoveredMessages) : IRequest;
