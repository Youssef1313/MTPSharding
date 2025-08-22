using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace YTest.MTP.PipeProtocol;

/// <summary>
/// A class that is used to discover tests in an MTP test project using the pipe protocol that is used by dotnet test.
/// </summary>
public sealed class MTPPipeDiscoverer
{
    private readonly TestApplication _testApplication;

    /// <summary>
    /// Creates an instance of MTPPipeDiscoverer.
    /// </summary>
    /// <param name="pathToExe">The full path to the test application to discover tests in.</param>
    /// <param name="arguments">Arguments to pass to the test application, not including --list-tests.</param>
    /// <param name="workingDirectory">The working directory of the starting test application.</param>
    // TODO: Replace these parameters with a TestApplicationRunParameters class to encapsulate them
    public MTPPipeDiscoverer(string pathToExe, string arguments, string? workingDirectory = null)
        => _testApplication = new TestApplication(pathToExe, $"{arguments} --list-tests", workingDirectory);

    /// <summary>
    /// Runs the test application with --list-tests to collect discovery information.
    /// </summary>
    /// <returns></returns>
    public async Task<(List<DiscoveredTestInformation> DiscoveredTests, TestProcessExitInformation ExitInformation)> DiscoverTestsAsync(Func<int, Task>? afterProcessStart = null)
    {
        var discoveredTests = new List<DiscoveredTestInformation>();
        EventHandler<DiscoveredTestEventArgs> onDiscovered = (_, e) => {
            foreach (var test in e.DiscoveredTests)
            {
                discoveredTests.Add(new DiscoveredTestInformation(test.Uid, test.DisplayName, test.FilePath, test.LineNumber, test.Namespace, test.TypeName, test.MethodName, test.Traits.Select(t => new TestTrait(t.Key!, t.Value!)).ToArray()));
            }
        };

        _testApplication.DiscoveredTestsReceived += onDiscovered;
        TestProcessExitInformation exitInformation;
        try
        {
            exitInformation = await _testApplication.RunAsync(afterProcessStart).ConfigureAwait(false);
        }
        finally
        {
            _testApplication.DiscoveredTestsReceived -= onDiscovered;
        }

        return (discoveredTests, exitInformation);
    }
}
