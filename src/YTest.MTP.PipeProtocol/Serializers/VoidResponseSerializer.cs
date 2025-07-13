using System.IO;

namespace YTest.MTP.PipeProtocol;

internal sealed class VoidResponseSerializer : INamedPipeSerializer
{
    public int Id => VoidResponseFieldsId.MessagesSerializerId;

    public object Deserialize(Stream stream)
        => new VoidResponse();

    public void Serialize(object objectToSerialize, Stream stream)
    {
    }
}
