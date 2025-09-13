namespace YTest.MTP.PipeProtocol;

/// <summary>
/// Information about the test process after it exists
/// </summary>
public class TestProcessExitInformation
{
    /// <summary>
    /// The standard output of the test process.
    /// </summary>
    public required string StandardOutput { get; init; }

    /// <summary>
    /// The standard error of the test process.
    /// </summary>
    public required string StandardError { get; init; }

    /// <summary>
    /// The exit code of the test process.
    /// </summary>
    public required int ExitCode { get; init; }
}
