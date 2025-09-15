﻿using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace YTest.MTP.PipeProtocol;

internal sealed class HandshakeMessageSerializer : BaseSerializer, INamedPipeSerializer
{
    public int Id => HandshakeMessageFieldsId.MessagesSerializerId;

    public object Deserialize(Stream stream)
    {
        Dictionary<byte, string> properties = [];

        ushort fieldCount = ReadUShort(stream);

        for (int i = 0; i < fieldCount; i++)
        {
            properties.Add(ReadByte(stream), ReadString(stream));
        }

        return new HandshakeMessage(properties);
    }

    public void Serialize(object objectToSerialize, Stream stream)
    {
        Debug.Assert(stream.CanSeek, "We expect a seekable stream.");

        var handshakeMessage = (HandshakeMessage)objectToSerialize;

        if (handshakeMessage.Properties is null || handshakeMessage.Properties.Count == 0)
        {
            return;
        }

        WriteUShort(stream, (ushort)handshakeMessage.Properties.Count);
        foreach (KeyValuePair<byte, string> property in handshakeMessage.Properties)
        {
            WriteField(stream, property.Key);
            WriteField(stream, property.Value);
        }
    }
}
