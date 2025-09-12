using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace YTest.MTP.PipeProtocol;

/// <summary>
/// A class that is used to run tests in an MTP test project using the pipe protocol that is used by dotnet test.
/// </summary>
public sealed class MTPPipeRunner
{
    private readonly string _pathToExe;
    private readonly string _arguments;
    private readonly string? _workingDirectory;

    /// <summary>
    /// Creates an instance of MTPPipeRunner that will run all tests.
    /// </summary>
    /// <param name="pathToExe">The full path to the test application to discover tests in.</param>
    /// <param name="arguments">Arguments to pass to the test application, not including --list-tests.</param>
    /// <param name="workingDirectory">The working directory of the starting test application.</param>
    // TODO: Replace these parameters with a TestApplicationRunParameters class to encapsulate them
    public MTPPipeRunner(string pathToExe, string arguments, string? workingDirectory = null)
    {
        _pathToExe = pathToExe;
        _arguments = arguments;
        _workingDirectory = workingDirectory;
    }

    /// <summary>
    /// Creates an instance of MTPPipeRunner that will run only the given tests Uids.
    /// This is only supported starting with Microsoft.Testing.Platform 1.8.
    /// </summary>
    /// <param name="pathToExe">The full path to the test application to discover tests in.</param>
    /// <param name="arguments">Arguments to pass to the test application, not including --list-tests.</param>
    /// <param name="testNodeUids">The test node uids to run.</param>
    /// <param name="workingDirectory">The working directory of the starting test application.</param>
    // TODO: Replace these parameters with a TestApplicationRunParameters class to encapsulate them
    public MTPPipeRunner(string pathToExe, string arguments, List<string> testNodeUids, string? workingDirectory = null)
    {
        var filter = $"--filter-uid {string.Join(" ", testNodeUids)}";
        // TODO: If too long, write to a temp rsp file and pass the rsp file with "@".
        // TODO: Escape testNodeUids. The escaping logic could be reused from https://github.com/dotnet/sdk/blob/a9c5b0cb6d9e37f97e13c2d10dd2044dbc9d94be/src/RazorSdk/Tool/CommandLine/ArgumentEscaper.cs#L23
        _pathToExe = pathToExe;
        _arguments = $"{arguments} {filter}";
        _workingDirectory = workingDirectory;
    }

    /// <summary>
    /// Runs the test application to run tests.
    /// </summary>
    /// <returns></returns>
    public async Task<(List<TestResultInformation> TestResults, TestProcessExitInformation ExitInformation)> RunTestsAsync(Func<int, Task>? afterProcessStart = null)
    {
        using var testApplication = new TestApplication(_pathToExe, _arguments, _workingDirectory);
        var results = new List<TestResultInformation>();
        testApplication.OnTestResult = message => {
            foreach (var result in message.SuccessfulTestMessages)
            {
                var uid = result.Uid ?? throw new InvalidOperationException("Uid is expected to be non-null");
                var displayName = result.DisplayName ?? throw new InvalidOperationException("DisplayName is expected to be non-null");
                var state = result.State ?? throw new InvalidOperationException("State is expected to be non-null");
                results.Add(new TestResultInformation(uid, displayName, ToOutcome(state), ToTimeSpan(result.Duration), result.Reason, result.StandardOutput, result.ErrorOutput, null));
            }

            foreach (var result in message.FailedTestMessages)
            {
                var uid = result.Uid ?? throw new InvalidOperationException("Uid is expected to be non-null");
                var displayName = result.DisplayName ?? throw new InvalidOperationException("DisplayName is expected to be non-null");
                var state = result.State ?? throw new InvalidOperationException("State is expected to be non-null");
                results.Add(new TestResultInformation(uid, displayName, ToOutcome(state), ToTimeSpan(result.Duration), result.Reason, result.StandardOutput, result.ErrorOutput, ToExceptions(result.Exceptions)));
            }

            return Task.CompletedTask;
        };

        TestProcessExitInformation exitInformation = await testApplication.RunAsync(afterProcessStart).ConfigureAwait(false);
        return (results, exitInformation);
    }

    /// <summary>
    /// Runs the test application to run tests.
    /// </summary>
    /// <returns></returns>
    public async Task<TestProcessExitInformation> RunTestsAsync(Func<TestResultInformation, Task> onTestResult, Func<int, Task>? afterProcessStart = null)
    {
        using var testApplication = new TestApplication(_pathToExe, _arguments, _workingDirectory);
        var results = new List<TestResultInformation>();

        testApplication.OnTestResult = async message => {
            foreach (var result in message.SuccessfulTestMessages)
            {
                var uid = result.Uid ?? throw new InvalidOperationException("Uid is expected to be non-null");
                var displayName = result.DisplayName ?? throw new InvalidOperationException("DisplayName is expected to be non-null");
                var state = result.State ?? throw new InvalidOperationException("State is expected to be non-null");
                await onTestResult(new TestResultInformation(uid, displayName, ToOutcome(state), ToTimeSpan(result.Duration), result.Reason, result.StandardOutput, result.ErrorOutput, null));
            }

            foreach (var result in message.FailedTestMessages)
            {
                var uid = result.Uid ?? throw new InvalidOperationException("Uid is expected to be non-null");
                var displayName = result.DisplayName ?? throw new InvalidOperationException("DisplayName is expected to be non-null");
                var state = result.State ?? throw new InvalidOperationException("State is expected to be non-null");
                await onTestResult(new TestResultInformation(uid, displayName, ToOutcome(state), ToTimeSpan(result.Duration), result.Reason, result.StandardOutput, result.ErrorOutput, ToExceptions(result.Exceptions)));
            }
        };

        return await testApplication.RunAsync(afterProcessStart).ConfigureAwait(false);
    }

    private static TestResultOutcome ToOutcome(byte state)
        => state switch
        {
            TestStates.Passed => TestResultOutcome.Passed,
            TestStates.Skipped => TestResultOutcome.Skipped,
            TestStates.Failed => TestResultOutcome.Failed,
            TestStates.Error => TestResultOutcome.Error,
            TestStates.Timeout => TestResultOutcome.Timeout,
            TestStates.Cancelled => TestResultOutcome.Cancelled,
            TestStates.InProgress => TestResultOutcome.InProgress,
            _ => throw new ArgumentException($"Unknown state '{state}'."),
        };

    private static TimeSpan? ToTimeSpan(long? durationInTicks)
        => durationInTicks.HasValue
            ? TimeSpan.FromTicks(durationInTicks.Value)
            : null;

    private static TestResultExceptionInfo[]? ToExceptions(ExceptionMessage[]? exceptions)
    {
        if (exceptions is null || exceptions.Length == 0)
        {
            return null;
        }

        var result = new TestResultExceptionInfo[exceptions.Length];
        for (var i = 0; i < exceptions.Length; i++)
        {
            var flatException = exceptions[i];
            result[i] = new TestResultExceptionInfo(
                flatException.ErrorMessage ?? string.Empty,
                flatException.ErrorType ?? string.Empty,
                flatException.StackTrace ?? string.Empty);
        }

        return result;
    }
}
