using System.IO;

namespace YTest.MTP.PipeProtocol;

internal interface INamedPipeSerializer
{
    int Id { get; }

    void Serialize(object objectToSerialize, Stream stream);

    object Deserialize(Stream stream);
}
