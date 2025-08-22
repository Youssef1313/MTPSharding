using System;

namespace YTest.MTP.PipeProtocol;

/// <summary>
/// Represents the test result of a test.
/// This type is returned by <see cref="MTPPipeRunner.RunTestsAsync(Func{int, System.Threading.Tasks.Task}?)"/> or <see cref="MTPPipeRunner.RunTestsAsync(Func{YTest.MTP.PipeProtocol.TestResultInformation, System.Threading.Tasks.Task}, Func{int, System.Threading.Tasks.Task}?)"/>
/// </summary>
public sealed class TestResultInformation
{
    internal TestResultInformation(
        string uid,
        string displayName,
        TestResultOutcome outcome,
        TimeSpan? duration,
        string? reason,
        string? standardOutput,
        string? standardError,
        TestResultExceptionInfo[]? exceptions)
    {
        Uid = uid;
        DisplayName = displayName;
        Outcome = outcome;
        Duration = duration;
        Reason = reason;
        StandardOutput = standardOutput;
        StandardError = standardError;
        Exceptions = exceptions;
    }

    /// <summary>
    /// The unique identifier of the test. In many cases it's a Guid, but that's not a guarantee!
    /// </summary>
    public string Uid { get; }

    /// <summary>
    /// The display name of the test.
    /// </summary>
    public string DisplayName { get; }

    /// <summary>
    /// The outcome of the test result.
    /// </summary>
    public TestResultOutcome Outcome { get; }

    /// <summary>
    /// Gets the duration of the test run.
    /// </summary>
    public TimeSpan? Duration { get; }

    /// <summary>
    /// Gets the reason associated with the current state or operation, if available.
    /// </summary>
    public string? Reason { get; }

    /// <summary>
    /// Gets the collection of exceptions that occurred during the test execution.
    /// </summary>
    /// <remarks>
    /// The returned array may be null if no exceptions were recorded. Use this property to inspect
    /// any errors that were encountered while running the test.
    /// </remarks>
    public TestResultExceptionInfo[]? Exceptions { get; }

    /// <summary>
    /// Gets the standard output produced by the test process, if available.
    /// </summary>
    public string? StandardOutput { get; }

    /// <summary>
    /// Gets the standard error produced by the test process, if available.
    /// </summary>
    public string? StandardError { get; }
}
