using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
#if !NETCOREAPP
using System.Text;
#endif
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Testing.Platform.CommandLine;
using Microsoft.Testing.Platform.Configurations;
using Microsoft.Testing.Platform.Extensions.TestHostOrchestrator;
using YTest.MTP.PipeProtocol;

namespace YTest.MTP.Sharding;

#pragma warning disable TPEXP // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
internal sealed class ShardingOrchestrator : ITestHostExecutionOrchestrator
#pragma warning restore TPEXP // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
{
    private readonly string _resultsDirectory;
    private readonly int? _shardCount;
    
    public ShardingOrchestrator(ICommandLineOptions commandLineOptions, IConfiguration configuration)
    {
        _resultsDirectory = configuration.GetTestResultDirectory();
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

        var currentArgs = Environment.GetCommandLineArgs().Skip(1).ToArray();

        var commonChildArgs = new List<string>()
        {
            "--no-progress",
            "--no-ansi",
        };

        for (int i = 0; i < currentArgs.Length; i++)
        {
            if (currentArgs[i] == "--server")
            {
                // Each child process gets its own instance id.
                // But they all share the same execution id.
                // This means dotnet test views this as "retries".
                // We need to resolve this, somehow.
                Console.WriteLine("Sharding isn't supported when running under dotnet test");
                return 1;
            }

            if (currentArgs[i] is "--results-directory" or "--shard-count")
            {
                i++;
                continue;
            }

            if (currentArgs[i] is "--no-progress" or "--no-ansi")
            {
                // We will always force no progress and no ansi.
                continue;
            }

            commonChildArgs.Add(currentArgs[i]);
        }

        var tasks = new Task<int>[shards.Length];
        for (int i = 0; i < shards.Length; i++)
        {
            var shardResultsDir = Path.Combine(_resultsDirectory, $"Shard{i + 1}");
            Directory.CreateDirectory(shardResultsDir);
            string responseFilePath = Path.Combine(shardResultsDir, $"shard-filter-uids.rsp");
            using (var stream = new FileStream(responseFilePath, FileMode.Create, FileAccess.Write))
            using (var writer = new StreamWriter(stream))
            {
                await writer.WriteAsync($"--filter-uid").ConfigureAwait(false);
                foreach (DiscoveredTestInformation testInfo in shards[i])
                {
                    await writer.WriteAsync($" \"{testInfo.Uid}\"").ConfigureAwait(false);
                }
            }

            var shardArgs = new List<string>(commonChildArgs)
            {
                "--results-directory",
                shardResultsDir,
                $"@{responseFilePath}",
            };

#if !NETCOREAPP
            var builder = new StringBuilder();
            foreach (var shardArg in shardArgs)
            {
                PasteArguments.AppendArgument(builder, shardArg);
            }
#endif

            var processStartInfo = new ProcessStartInfo(filePath)
            {
                UseShellExecute = false,
                WorkingDirectory = Environment.CurrentDirectory,
#if !NETCOREAPP
                Arguments = builder.ToString(),
#endif
            };

#if NETCOREAPP
            foreach (var shardArg in shardArgs)
            {
                processStartInfo.ArgumentList.Add(shardArg);   
            }
#endif

            var process = Process.Start(processStartInfo);
            tasks[i] = WaitForExitAsync(process!);
        }

        var exitCodes = await Task.WhenAll(tasks);
        int? aggregatedExitCode = null;
        foreach (var exitCode in exitCodes)
        {
            if (aggregatedExitCode is null)
            {
                aggregatedExitCode = exitCode;
            }
            else if (aggregatedExitCode.Value != exitCode)
            {
                if (aggregatedExitCode == 0)
                {
                    aggregatedExitCode = exitCode;
                }
                else if (exitCode != 0)
                {
                    aggregatedExitCode = 1;
                }
            }
        }

        return aggregatedExitCode ?? 1;
    }

    private static async Task<int> WaitForExitAsync(Process process)
    {
        await Task.Yield();
#if NETCOREAPP
        await process.WaitForExitAsync();
#else
        try
        {
            process.EnableRaisingEvents = true;
        }
        catch (InvalidOperationException)
        {
            // CASE 2: If the process has exited, our work is done, otherwise bubble the
            // exception up to the user
            if (process.HasExited)
            {
                return process.ExitCode;
            }

            throw;
        }

        var tcs = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);

        EventHandler handler = (_, _) => tcs.TrySetResult(null);
        process.Exited += handler;

        try
        {
            if (!process.HasExited)
            {
                await tcs.Task.ConfigureAwait(false);
            }
        }
        finally
        {
            process.Exited -= handler;
        }
#endif

        return process.ExitCode;
    }
}
