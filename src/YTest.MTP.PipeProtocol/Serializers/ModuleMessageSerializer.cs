using System.IO;

namespace YTest.MTP.PipeProtocol;

internal sealed class ModuleMessageSerializer : BaseSerializer, INamedPipeSerializer
{
    public int Id => ModuleFieldsId.MessagesSerializerId;

    public object Deserialize(Stream stream)
    {
        string modulePath = ReadString(stream);
        string projectPath = ReadString(stream);
        string targetFramework = ReadString(stream);
        string isTestingPlatformApplication = ReadString(stream);
        return new ModuleMessage(modulePath.Trim(), projectPath.Trim(), targetFramework.Trim(), isTestingPlatformApplication.Trim());
    }

    public void Serialize(object objectToSerialize, Stream stream)
    {
        WriteString(stream, ((ModuleMessage)objectToSerialize).DllOrExePath);
        WriteString(stream, ((ModuleMessage)objectToSerialize).ProjectPath);
        WriteString(stream, ((ModuleMessage)objectToSerialize).TargetFramework);
        WriteString(stream, ((ModuleMessage)objectToSerialize).IsTestingPlatformApplication);
    }
}
