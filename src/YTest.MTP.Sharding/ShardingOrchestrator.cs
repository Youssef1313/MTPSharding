using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Testing.Platform.CommandLine;
using Microsoft.Testing.Platform.Extensions.CommandLine;
using Microsoft.Testing.Platform.Extensions.Messages;
using Microsoft.Testing.Platform.Extensions.TestHostOrchestrator;
using Microsoft.Testing.Platform.Requests;
using YTest.MTP.PipeProtocol;

namespace YTest.MTP.Sharding;

#pragma warning disable TPEXP // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
internal sealed class ShardingOrchestrator : ITestHostExecutionOrchestrator
#pragma warning restore TPEXP // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
{
    private readonly int? _shardCount;

    public ShardingOrchestrator(ICommandLineOptions commandLineOptions)
    {
        if (commandLineOptions.TryGetOptionArgumentList("shard-count", out string[]? shardCountArg) &&
            int.TryParse(shardCountArg[0], out int shardCount))
        {
            _shardCount = shardCount;
        }
    }

    public string Uid => nameof(ShardingOrchestrator);

    public string Version => "1.0.0";

    public string DisplayName => "Sharding Orchestrator";

    public string Description => "An extension to support test sharding across multiple processes on the same machine.";

    public Task<bool> IsEnabledAsync() => Task.FromResult(_shardCount > 1);

    public async Task<int> OrchestrateTestHostExecutionAsync(CancellationToken cancellationToken)
    {
#if NETSTANDARD || NETFRAMEWORK
        string filePath;
        using (var process = Process.GetCurrentProcess())
        {
            filePath = process.MainModule.FileName;
        }
#else
        string filePath = Environment.ProcessPath!;
#endif

        int shardCount = _shardCount!.Value;

        var discoverer = new MTPPipeDiscoverer(filePath, string.Empty, Environment.CurrentDirectory);
        var (tests, exitInfo) = await discoverer.DiscoverTestsAsync();
        var shards = new List<DiscoveredTestInformation>[shardCount];
        for (int i = 0; i < tests.Count; i++)
        {
            var shardIndex = i % shardCount;
            if (shards[shardIndex] == null)
            {
                shards[shardIndex] = [tests[i]];
            }
            else
            {
                shards[shardIndex].Add(tests[i]);
            }
        }

        foreach (var shard in shards)
        {
            // TODO: For each shard, start a child process with the correct filter.
            // TODO: When running with terminal progress, how are we supposed to handle all those child processes? :/
            // TODO: How to handle --results-directory. For example, if user is running with --report-trx --report-trx-filename myfile.trx
            // How can each shard handle stuff correctly?
            // Is it reasonable to alter --results-directory to something like `Path.Combine(original, $"Shard{shardNumber}")` ?
        }

        return 0;

    }
}
