using System.IO;

namespace YTest.MTP.PipeProtocol;

internal sealed class UnknownMessageSerializer(int SerializerId) : BaseSerializer, INamedPipeSerializer
{
    public int Id { get; } = SerializerId;

    public object Deserialize(Stream _)
    {
        return new UnknownMessage(Id);
    }

    public void Serialize(object _, Stream stream)
    {
        WriteInt(stream, Id);
    }
}
