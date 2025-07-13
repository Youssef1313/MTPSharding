namespace YTest.MTP.PipeProtocol;

internal sealed record FileArtifactMessage(string? FullPath, string? DisplayName, string? Description, string? TestUid, string? TestDisplayName, string? SessionUid);

internal sealed record FileArtifactMessages(string? ExecutionId, string? InstanceId, FileArtifactMessage[] FileArtifacts) : IRequest;
