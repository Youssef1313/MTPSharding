namespace YTest.MTP.PipeProtocol;

/// <summary>
/// Represents information about an exception that occurred during a test run.
/// </summary>
public sealed class TestResultExceptionInfo
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TestResultExceptionInfo"/> class.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="type">The type of the exception.</param>
    /// <param name="stackTrace">The stack trace of the exception.</param>
    public TestResultExceptionInfo(string message, string type, string stackTrace)
    {
        Message = message;
        Type = type;
        StackTrace = stackTrace;
    }

    /// <summary>
    /// Gets the message of the exception.
    /// </summary>
    public string Message { get; }

    /// <summary>
    /// Gets the type of the exception.
    /// </summary>
    public string Type { get; }
    /// <summary>
    /// Gets the stack trace of the exception.
    /// </summary>
    public string StackTrace { get; }
}
