using Microsoft.Testing.Platform.Builder;

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
        => testApplicationBuilder.CommandLine.AddProvider(() => new ShardingCommandLineOptionsProvider());
}
