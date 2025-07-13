using System;
using System.Collections.Generic;
using System.Globalization;

namespace YTest.MTP.PipeProtocol;

internal abstract class NamedPipeBase
{
    private readonly Dictionary<Type, INamedPipeSerializer> _typeSerializer = [];
    private readonly Dictionary<int, INamedPipeSerializer> _idSerializer = [];

    public void RegisterSerializer(INamedPipeSerializer namedPipeSerializer, Type type)
    {
        _typeSerializer.Add(type, namedPipeSerializer);
        _idSerializer.Add(namedPipeSerializer.Id, namedPipeSerializer);
    }

    protected INamedPipeSerializer GetSerializer(int id, bool skipUnknownMessages = false)
    {
        if (_idSerializer.TryGetValue(id, out INamedPipeSerializer? serializer))
        {
            return serializer;
        }
        else
        {
            return skipUnknownMessages
                ? new UnknownMessageSerializer(id)
                : throw new ArgumentException((string.Format(
                CultureInfo.InvariantCulture,
                "No serializer registered with ID '{0}'",
                id)));
        }
    }


    protected INamedPipeSerializer GetSerializer(Type type)
        => _typeSerializer.TryGetValue(type, out INamedPipeSerializer? serializer)
            ? serializer
            : throw new ArgumentException(string.Format(
                CultureInfo.InvariantCulture,
                "No serializer registered with type '{0}'",
                type));
}
