using System.Threading.Tasks;
using Microsoft.Testing.Platform.Builder;
using Microsoft.Testing.Platform.Capabilities.TestFramework;
using YTest.MTP.Sharding.FakeShardingFramework;

namespace YTest.MTP.Sharding;

/// <summary>
/// Helper extension methods for test application builder
/// </summary>
public static class TestApplicationBuilderExtensions
{
    /// <summary>
    /// Builds test application that correctly considers --sharding-count.
    /// </summary>
    public static async Task<ITestApplication> BuildForShardingAsync(this ITestApplicationBuilder builder)
    {
        var originalApp = await builder.BuildAsync().ConfigureAwait(false);
        if (ShardingCommandLineOptionsProvider.ShardCount is { } shardCount)
        {
            originalApp.Dispose();

            // Create a new test application builder for the "fake" framework.
            string[] argsForFakeApp = ShardingCommandLineOptionsProvider.DotnetTestPipeName is null
                ? []
                : ["--server", "dotnettestcli", "--dotnet-test-pipe", ShardingCommandLineOptionsProvider.DotnetTestPipeName];
            ITestApplicationBuilder fakeBuilder = await TestApplication.CreateBuilderAsync(argsForFakeApp);
            fakeBuilder.RegisterTestFramework(_ => new TestFrameworkCapabilities(), (_, _) => new ShardingTestFramework(shardCount));
            ITestApplication fakeApp = await fakeBuilder.BuildAsync();
            return fakeApp;
        }

        return originalApp;
    }
}
