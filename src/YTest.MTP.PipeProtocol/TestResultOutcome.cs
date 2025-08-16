namespace YTest.MTP.PipeProtocol;

/// <summary>
/// The outcome of a test result.
/// </summary>
public enum TestResultOutcome
{
    // TestResultOutcome should never be discovered.
    // I'm matching the rest of the values with the TestStates values from the protocol.
    // Discovered = 0,

    /// <summary>
    /// Indicating a passing test.
    /// </summary>
    Passed = 1,

    /// <summary>
    /// Indicating a skipped test.
    /// </summary>
    Skipped = 2,

    /// <summary>
    /// Indicating a test failure.
    /// </summary>
    Failed = 3,
    
    /// <summary>
    /// Indicating a test error.
    /// </summary>
    Error = 4,
    
    /// <summary>
    /// Indicating a test timeout.
    /// </summary>
    Timeout = 5,

    /// <summary>
    /// Indicating a test that was cancelled.
    /// </summary>
    Cancelled = 6,

    /// <summary>
    /// Indicating a test that is in progress.
    /// </summary>
    InProgress = 7
}
