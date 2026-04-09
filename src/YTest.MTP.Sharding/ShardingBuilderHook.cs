using Microsoft.Testing.Platform.Builder;
using Microsoft.Testing.Platform.Services;

namespace YTest.MTP.Sharding;

/// <summary>
/// Registers the sharding extension.
/// </summary>
public static class ShardingBuilderHook
{
    /// <summary>
    /// Registers the sharding extension
    /// </summary>
    public static void AddExtensions(ITestApplicationBuilder testApplicationBuilder, string[] _)
    {
        testApplicationBuilder.CommandLine.AddProvider(() => new ShardingCommandLineOptionsProvider());
#pragma warning disable TPEXP // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
        testApplicationBuilder.TestHostOrchestrator.AddTestHostOrchestrator(sp => new ShardingOrchestrator(sp.GetCommandLineOptions(), sp.GetConfiguration()));
#pragma warning restore TPEXP // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
    }
}
