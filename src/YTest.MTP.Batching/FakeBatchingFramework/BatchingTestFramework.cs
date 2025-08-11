using System;
using System.Collections.Generic;
#if !NETCOREAPP
using System.Diagnostics;
#endif
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Testing.Platform.Extensions.Messages;
using Microsoft.Testing.Platform.Extensions.TestFramework;
using Microsoft.Testing.Platform.Requests;
using YTest.MTP.PipeProtocol;

namespace YTest.MTP.Batching.FakeBatchingFramework;

internal sealed class BatchingTestFramework : ITestFramework, IDataProducer
{
    public BatchingTestFramework(int batchCount)
        => BatchCount = batchCount;

    public string Uid => nameof(BatchingTestFramework);

    public string Version => "1.0.0";

    public string DisplayName => "Batching Test Framework";

    public string Description => "Batching Test Framework for YTest.MTP.Batching extension";

    public Type[] DataTypesProduced { get; } = [typeof(TestNodeUpdateMessage)];

    internal int BatchCount { get; }

    public Task<CloseTestSessionResult> CloseTestSessionAsync(CloseTestSessionContext context)
        => Task.FromResult(new CloseTestSessionResult() { IsSuccess = true });

    public Task<CreateTestSessionResult> CreateTestSessionAsync(CreateTestSessionContext context)
        => Task.FromResult(new CreateTestSessionResult() { IsSuccess = true });

    public async Task ExecuteRequestAsync(ExecuteRequestContext context)
    {
        if (context.Request is DiscoverTestExecutionRequest)
        {
            throw new InvalidOperationException("The BatchingTestFramework isn't expected to run with discovery.");
        }

        if (context.Request is not RunTestExecutionRequest runRequest)
        {
            throw new InvalidOperationException($"Expected request to be 'RunTestExecutionRequest', but found '{context.Request}'.");
        }

#if NETCOREAPP
        var path = Environment.ProcessPath;
#else
        var path = Process.GetCurrentProcess().MainModule.FileName;
#endif
        if (string.IsNullOrEmpty(path))
        {
            throw new InvalidOperationException("Cannot get current process path.");
        }

        string args = runRequest.Filter switch
        {
#pragma warning disable TPEXP // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
            null or NopFilter or TreeNodeFilter => string.Empty,
#pragma warning restore TPEXP // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
            TestNodeUidListFilter testNodeUidListFilter => $"--filter-uid {string.Join(" ", testNodeUidListFilter.TestNodeUids.Select(uid => uid.Value))}",
            _ => throw new NotSupportedException($"Filter type '{runRequest.Filter.GetType()}' is not supported by the BatchingTestFramework."),
        };

        var discoverer = new MTPPipeDiscoverer(path, args);
        var (tests, exitInfo) = await discoverer.DiscoverTestsAsync();
        var batches = new List<DiscoveredTestInformation>[BatchCount];
        for (int i = 0; i < tests.Count; i++)
        {
#pragma warning disable TPEXP // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
            if (runRequest.Filter is TreeNodeFilter treeNodeFilter)
#pragma warning restore TPEXP // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
            {
                // TODO: Pass traits (as TestMetadataProperty) here when it's supported by pipe protocol.
                if (!treeNodeFilter.MatchesFilter(tests[i].Uid, new PropertyBag()))
                {
                    continue;
                }
            }

            var batchIndex = i % BatchCount;
            if (batches[batchIndex] == null)
            {
                batches[batchIndex] = [tests[i]];
            }
            else
            {
                batches[batchIndex].Add(tests[i]);
            }
        }

        var tasks = new List<Task<TestProcessExitInformation>>();
        foreach (var batch in batches)
        {
            if (batch.Count == 0)
            {
                continue;
            }

            var runner = new MTPPipeRunner(path, "", batch.Select(test => test.Uid).ToList());
            tasks.Add(runner.RunTestsAsync(async result =>
            {
                var properties = new PropertyBag();

                // TODO: Pass traits (as TestMetadataProperty) here when it's supported by pipe protocol.

                properties.Add(result.Outcome switch
                {
                    TestResultOutcome.Passed => PassedTestNodeStateProperty.CachedInstance,
                    TestResultOutcome.Skipped => new SkippedTestNodeStateProperty(result.Reason ?? ""),
                    TestResultOutcome.Failed => new FailedTestNodeStateProperty(result.Reason ?? ""),// TODO: This is likely not enough. We need exact exception details. Look into how that data propagates through the protocol
                    TestResultOutcome.Error => new ErrorTestNodeStateProperty(result.Reason ?? ""),
                    TestResultOutcome.Timeout => new TimeoutTestNodeStateProperty(result.Reason ?? ""),
                    TestResultOutcome.Cancelled => new CancelledTestNodeStateProperty(result.Reason ?? ""),
                    _ => throw new InvalidOperationException($"Unexpected outcome '{result.Outcome}'."),
                });

                if (result.Duration is not null)
                {
                    var endTime = DateTime.UtcNow;
                    var startTime = endTime - result.Duration.Value;
                    properties.Add(new TimingProperty(new TimingInfo(startTime, endTime, result.Duration.Value)));
                }

                if (!string.IsNullOrEmpty(result.StandardOutput))
                {
#pragma warning disable TPEXP // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
                    properties.Add(new StandardOutputProperty(result.StandardOutput!));
#pragma warning restore TPEXP // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
                }

                if (!string.IsNullOrEmpty(result.StandardError))
                {
#pragma warning disable TPEXP // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
                    properties.Add(new StandardErrorProperty(result.StandardError!));
#pragma warning restore TPEXP // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
                }

                await context.MessageBus.PublishAsync(
                    this,
                    new TestNodeUpdateMessage(
                        context.Request.Session.SessionUid,
                        new TestNode()
                        {
                            DisplayName = result.DisplayName,
                            Uid = result.Uid,
                            Properties = properties,
                        }));
            }));
        }

        await Task.WhenAll(tasks);
        for (int i = 0; i < tasks.Count; i++)
        {
            if (tasks[i].Result.ExitCode != 0)
            {
                var batchFailureProperties = new PropertyBag();
                batchFailureProperties.Add(new ErrorTestNodeStateProperty($"""
                    Batch {i + 1} failed with exit code {tasks[i].Result.ExitCode}.
                    Standard Output:
                    {string.Join(Environment.NewLine, tasks[i].Result.StandardOutput)}

                    Standard Error:
                    {string.Join(Environment.NewLine, tasks[i].Result.StandardError)}
                    """));
                await context.MessageBus.PublishAsync(
                    this,
                    new TestNodeUpdateMessage(
                        context.Request.Session.SessionUid,
                        new TestNode()
                        {
                            DisplayName = $"[Batch {i + 1} failure]",
                            Uid = $"Batch-{i + 1}",
                            Properties = batchFailureProperties,
                        }));
            }
        }

        context.Complete();
    }

    public Task<bool> IsEnabledAsync()
        => Task.FromResult(true);
}
