namespace YTest.MTP.PipeProtocol;

internal sealed record class UnknownMessage(int SerializerId) : IRequest;
