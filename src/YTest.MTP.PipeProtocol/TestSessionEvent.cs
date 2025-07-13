namespace YTest.MTP.PipeProtocol;

internal sealed record TestSessionEvent(byte? SessionType, string? SessionUid, string? ExecutionId) : IRequest;
