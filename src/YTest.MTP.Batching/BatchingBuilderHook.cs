using Microsoft.Testing.Platform.Builder;

namespace YTest.MTP.Batching;

/// <summary>
/// Registers the batching extension.
/// </summary>
public static class BatchingBuilderHook
{
    /// <summary>
    /// Registers the batching extension
    /// </summary>
    public static void AddExtensions(ITestApplicationBuilder testApplicationBuilder, string[] _)
        => testApplicationBuilder.CommandLine.AddProvider(() => new BatchingCommandLineOptionsProvider());
}
