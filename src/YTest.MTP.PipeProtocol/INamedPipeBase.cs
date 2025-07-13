using System;

namespace YTest.MTP.PipeProtocol;

internal interface INamedPipeBase
{
    void RegisterSerializer(INamedPipeSerializer namedPipeSerializer, Type type);
}
