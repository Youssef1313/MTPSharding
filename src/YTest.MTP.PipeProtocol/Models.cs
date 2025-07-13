using System.Collections.Generic;

namespace YTest.MTP.PipeProtocol;

internal sealed record Handshake(Dictionary<byte, string>? Properties);

internal sealed record CommandLineOption(string Name, string Description, bool? IsHidden, bool? IsBuiltIn);

internal sealed record DiscoveredTest(string Uid, string DisplayName);

internal sealed record SuccessfulTestResult(string? Uid, string? DisplayName, byte? State, long? Duration, string? Reason, string? StandardOutput, string? ErrorOutput, string? SessionUid);

internal sealed record FailedTestResult(string? Uid, string? DisplayName, byte? State, long? Duration, string? Reason, FlatException[]? Exceptions, string? StandardOutput, string? ErrorOutput, string? SessionUid);

internal sealed record FlatException(string? ErrorMessage, string? ErrorType, string? StackTrace);

internal sealed record FileArtifact(string? FullPath, string? DisplayName, string? Description, string? TestUid, string? TestDisplayName, string? SessionUid);

internal sealed record TestSession(byte? SessionType, string? SessionUid, string? ExecutionId);
