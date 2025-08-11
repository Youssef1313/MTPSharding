using System.Threading.Tasks;
using Microsoft.Testing.Platform.Builder;
using Microsoft.Testing.Platform.Capabilities.TestFramework;
using YTest.MTP.Batching.FakeBatchingFramework;

namespace YTest.MTP.Batching;

/// <summary>
/// Helper extension methods for test application builder
/// </summary>
public static class TestApplicationBuilderExtensions
{
    /// <summary>
    /// Builds test application that correctly considers --batch-count.
    /// </summary>
    public static async Task<ITestApplication> BuildForBatchingAsync(this ITestApplicationBuilder builder)
    {
        var originalApp = await builder.BuildAsync().ConfigureAwait(false);
        if (BatchingCommandLineOptionsProvider.BatchCount is { } batchCount)
        {
            originalApp.Dispose();

            // Create a new test application builder for the "fake" framework.
            string[] argsForFakeApp = BatchingCommandLineOptionsProvider.DotnetTestPipeName is null
                ? []
                : ["--server", "dotnettestcli", "--dotnet-test-pipe", BatchingCommandLineOptionsProvider.DotnetTestPipeName];
            ITestApplicationBuilder fakeBuilder = await TestApplication.CreateBuilderAsync(argsForFakeApp);
            fakeBuilder.RegisterTestFramework(_ => new TestFrameworkCapabilities(), (_, _) => new BatchingTestFramework(batchCount));
            ITestApplication fakeApp = await fakeBuilder.BuildAsync();
            return fakeApp;
        }

        return originalApp;
    }
}
