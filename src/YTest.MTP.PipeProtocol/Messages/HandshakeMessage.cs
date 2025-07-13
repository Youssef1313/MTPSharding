using System.Collections.Generic;

namespace YTest.MTP.PipeProtocol;

internal sealed record HandshakeMessage(Dictionary<byte, string> Properties) : IRequest, IResponse;
