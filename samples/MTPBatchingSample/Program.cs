using System.Diagnostics;
using Microsoft.Testing.Platform.Builder;
using MTPBatchingSample.MSTest;
using YTest.MTP.Batching;
using YTest.MTP.PipeProtocol;

Environment.SetEnvironmentVariable("DOTNET_ROOT", "C:\\Users\\ygerges\\Desktop\\aspire\\.dotnet");

var discoverer = new MTPPipeDiscoverer("C:\\Users\\ygerges\\Desktop\\aspire\\artifacts\\bin\\Aspire.Cli.Tests\\Debug\\net8.0\\Aspire.Cli.Tests.exe", "");
var result = await discoverer.DiscoverTestsAsync();

ITestApplicationBuilder builder = await TestApplication.CreateBuilderAsync(args);
SelfRegisteredExtensions.AddSelfRegisteredExtensions(builder, args);

// Usually without batching, this calls BuildAsync.
// However, we provide BuildForBatchingAsync that knows which test framework to build (original vs fake)
using ITestApplication app = await builder.BuildForBatchingAsync();

return await app.RunAsync();

/// <summary>
/// Dummy test class. Serves as a playgound.
/// </summary>
[TestClass]
public class MyTestClass
{
    [TestMethod]
    public void TestMethod1()
    {
    }

    [TestMethod]
    public void TestMethod2()
    {
    }

    [TestMethod]
    public void TestMethod3()
    {
    }

    [TestMethod]
    public void TestMethod4()
    {
    }

    [TestMethod]
    public void TestMethod5()
    {
    }

    [TestMethod]
    public void TestMethod6()
    {
    }

    [TestMethod]
    public void TestMethod7()
    {
    }

    [TestMethod]
    public void TestMethod8()
    {
    }

    [TestMethod]
    public void TestMethod9()
    {
    }

    [TestMethod]
    public void TestMethod10()
    {
    }
}
