using System.IO;
using YTest.MTP.PipeProtocol;

namespace YTest.MTP.Batching.Tests;

[TestClass]
public class TestClass1
{
    [TestMethod]
    public void TestMethod123()
    {
        DiscoveredTestMessagesSerializer x = new();
        var memoryStream = new MemoryStream();
        x.Serialize(new DiscoveredTestMessages("ExecId", "InstanceId", [new DiscoveredTestMessage("Uid11", "DisplayName11"), new DiscoveredTestMessage("Uid22", "DisplayName22"), new DiscoveredTestMessage("Uid33", "DisplayName33"), new DiscoveredTestMessage("Uid44", "DisplayName44"), new DiscoveredTestMessage("Uid55", "DisplayName55")]), memoryStream);
        memoryStream.Position = 0;
        var y = x.Deserialize(memoryStream);
    }
}
