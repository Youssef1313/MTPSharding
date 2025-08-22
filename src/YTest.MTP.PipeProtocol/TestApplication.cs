using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Pipes;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace YTest.MTP.PipeProtocol;

/// <summary>
/// Information about the test process after it exists
/// </summary>
public class TestProcessExitInformation : EventArgs
{
    /// <summary>
    /// The standard output of the test process.
    /// </summary>
    public required List<string> StandardOutput { get; init; }

    /// <summary>
    /// The standard error of the test process.
    /// </summary>
    public required List<string> StandardError { get; init; }

    /// <summary>
    /// The exit code of the test process.
    /// </summary>
    public required int ExitCode { get; init; }
}

internal sealed class TestApplication : IDisposable
{
    private readonly List<string> _standardOutput = [];
    private readonly List<string> _standardError = [];
    private readonly PipeNameDescription _pipeNameDescription = NamedPipeServer.GetPipeName(Guid.NewGuid().ToString("N"));
    private readonly CancellationTokenSource _cancellationToken = new();
    private readonly string _pathToExe;
    private readonly string _arguments;
    private readonly string? _workingDirectory;
    private Task? _afterProcessStartTask;

    private Task? _testAppPipeConnectionLoop;
    private readonly List<NamedPipeServer> _testAppPipeConnections = [];

    public TestApplication(string pathToExe, string arguments, string? workingDirectory = null)
    {
        _pathToExe = pathToExe;
        _arguments = arguments;
        _workingDirectory = workingDirectory;
    }

    public event EventHandler<HandshakeArgs>? HandshakeReceived;
    public event EventHandler<HelpEventArgs>? HelpRequested;
    public event EventHandler<DiscoveredTestEventArgs>? DiscoveredTestsReceived;
    public event Func<object, TestResultEventArgs, Task>? TestResultsReceived;
    public event EventHandler<FileArtifactEventArgs>? FileArtifactsReceived;
    public event EventHandler<SessionEventArgs>? SessionEventReceived;

    public async Task<TestProcessExitInformation> RunAsync(Func<int, Task>? afterProcessStartCallback = null)
    {
        var processStartInfo = CreateProcessStartInfo(_pathToExe, _arguments, _workingDirectory);
        _testAppPipeConnectionLoop = Task.Run(async () => await WaitConnectionAsync(_cancellationToken.Token).ConfigureAwait(false), _cancellationToken.Token);
        var testProcessResult = await StartProcess(processStartInfo, afterProcessStartCallback).ConfigureAwait(false);

        WaitOnTestApplicationPipeConnectionLoop();

        return testProcessResult;
    }

    private ProcessStartInfo CreateProcessStartInfo(string pathToExe, string arguments, string? workingDirectory)
    {
        var processStartInfo = new ProcessStartInfo
        {
            FileName = pathToExe,
            Arguments = $"{arguments} --server dotnettestcli --dotnet-test-pipe {_pipeNameDescription.Name}",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        if (!string.IsNullOrEmpty(workingDirectory))
        {
            processStartInfo.WorkingDirectory = workingDirectory;
        }

        return processStartInfo;
    }

    private void WaitOnTestApplicationPipeConnectionLoop()
    {
        _cancellationToken.Cancel();
        _testAppPipeConnectionLoop?.Wait((int)TimeSpan.FromSeconds(30).TotalMilliseconds);
    }

    private async Task WaitConnectionAsync(CancellationToken token)
    {
        try
        {
            while (!token.IsCancellationRequested)
            {
                NamedPipeServer pipeConnection = new(_pipeNameDescription, OnRequest, NamedPipeServerStream.MaxAllowedServerInstances, token, skipUnknownMessages: true);
                pipeConnection.RegisterAllSerializers();

                await pipeConnection.WaitConnectionAsync(token).ConfigureAwait(false);
                _testAppPipeConnections.Add(pipeConnection);
            }
        }
        catch (OperationCanceledException ex) when (ex.CancellationToken == token)
        {
        }
        catch (Exception ex)
        {
            Environment.FailFast(ex.ToString());
        }
    }

    private async Task<IResponse> OnRequest(IRequest request)
    {
        try
        {
            switch (request)
            {
                case HandshakeMessage handshakeMessage:
                    if (_afterProcessStartTask is not null)
                    {
                        SpinWait.SpinUntil(() => _afterProcessStartTask.IsCompleted);
                    }

                    if (handshakeMessage.Properties.TryGetValue(HandshakeMessagePropertyNames.ModulePath, out string? value))
                    {
                        OnHandshakeMessage(handshakeMessage);

                        return (IResponse)CreateHandshakeMessage(GetSupportedProtocolVersion(handshakeMessage));
                    }
                    break;

                case CommandLineOptionMessages commandLineOptionMessages:
                    OnCommandLineOptionMessages(commandLineOptionMessages);
                    break;

                case DiscoveredTestMessages discoveredTestMessages:
                    OnDiscoveredTestMessages(discoveredTestMessages);
                    break;

                case TestResultMessages testResultMessages:
                    await OnTestResultMessagesAsync(testResultMessages).ConfigureAwait(false);
                    break;

                case FileArtifactMessages fileArtifactMessages:
                    OnFileArtifactMessages(fileArtifactMessages);
                    break;

                case TestSessionEvent sessionEvent:
                    OnSessionEvent(sessionEvent);
                    break;

                // If we don't recognize the message, log and skip it
                case UnknownMessage unknownMessage:
                    return (IResponse)VoidResponse.CachedInstance;

                default:
                    // If it doesn't match any of the above, throw an exception
                    throw new NotSupportedException($"Message Request type '{request.GetType()}' is unsupported.");
            }
        }
        catch (Exception ex)
        {
            Environment.FailFast(ex.ToString());
        }

        return (IResponse)VoidResponse.CachedInstance;
    }

    private static string GetSupportedProtocolVersion(HandshakeMessage handshakeMessage)
    {
        handshakeMessage.Properties.TryGetValue(HandshakeMessagePropertyNames.SupportedProtocolVersions, out string? protocolVersions);

        string version = string.Empty;
        if (protocolVersions is not null && protocolVersions.Split(';').Contains(ProtocolConstants.Version))
        {
            version = ProtocolConstants.Version;
        }

        return version;
    }

    private static HandshakeMessage CreateHandshakeMessage(string version) =>
        new(new Dictionary<byte, string>
        {
            { HandshakeMessagePropertyNames.PID, Process.GetCurrentProcess().Id.ToString() },
            { HandshakeMessagePropertyNames.Architecture, RuntimeInformation.ProcessArchitecture.ToString() },
            { HandshakeMessagePropertyNames.Framework, RuntimeInformation.FrameworkDescription },
            { HandshakeMessagePropertyNames.OS, RuntimeInformation.OSDescription },
            { HandshakeMessagePropertyNames.SupportedProtocolVersions, version },
            { HandshakeMessagePropertyNames.IsIDE, "true" }, // TODO: Make it user configurable.
        });

    private async Task<TestProcessExitInformation> StartProcess(ProcessStartInfo processStartInfo, Func<int, Task>? afterProcessStartCallback)
    {
        var process = Process.Start(processStartInfo)!;
        StoreOutputAndErrorData(process);
        if (afterProcessStartCallback is not null)
        {
            var afterProcessStartTask = afterProcessStartCallback(process.Id);
            _afterProcessStartTask = afterProcessStartTask;
            await afterProcessStartTask;
        }

#if NET
        await process.WaitForExitAsync().ConfigureAwait(false);
#else
        process.WaitForExit();
#endif

        var exitInfo = new TestProcessExitInformation { StandardOutput = _standardOutput, StandardError = _standardError, ExitCode = process.ExitCode };
        return exitInfo;
    }

    private void StoreOutputAndErrorData(Process process)
    {
        process.EnableRaisingEvents = true;

        process.OutputDataReceived += (sender, e) => {
            if (string.IsNullOrEmpty(e.Data))
                return;

            _standardOutput.Add(e.Data);
        };
        process.ErrorDataReceived += (sender, e) => {
            if (string.IsNullOrEmpty(e.Data))
                return;

            _standardError.Add(e.Data);
        };
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
    }

    public void OnHandshakeMessage(HandshakeMessage handshakeMessage)
    {
        HandshakeReceived?.Invoke(this, new HandshakeArgs { Handshake = new Handshake(handshakeMessage.Properties) });
    }

    public void OnCommandLineOptionMessages(CommandLineOptionMessages commandLineOptionMessages)
    {
        HelpRequested?.Invoke(this, new HelpEventArgs { ModulePath = commandLineOptionMessages.ModulePath, CommandLineOptions = [.. commandLineOptionMessages.CommandLineOptionMessageList.Select(message => new CommandLineOption(message.Name, message.Description, message.IsHidden, message.IsBuiltIn))] });
    }

    internal void OnDiscoveredTestMessages(DiscoveredTestMessages discoveredTestMessages)
    {
        DiscoveredTestsReceived?.Invoke(this, new DiscoveredTestEventArgs
        {
            ExecutionId = discoveredTestMessages.ExecutionId,
            InstanceId = discoveredTestMessages.InstanceId,
            DiscoveredTests = [.. discoveredTestMessages.DiscoveredMessages.Select(
                message => new DiscoveredTest(message.Uid, message.DisplayName, message.FilePath, message.LineNumber, message.Namespace, message.TypeName, message.MethodName, message.Traits))]
        });
    }

    internal async Task OnTestResultMessagesAsync(TestResultMessages testResultMessage)
    {
        if (TestResultsReceived is null)
        {
            return;
        }

        await TestResultsReceived.Invoke(this, new TestResultEventArgs
        {
            ExecutionId = testResultMessage.ExecutionId,
            InstanceId = testResultMessage.InstanceId,
            SuccessfulTestResults = [.. testResultMessage.SuccessfulTestMessages.Select(message => new SuccessfulTestResult(message.Uid, message.DisplayName, message.State, message.Duration, message.Reason, message.StandardOutput, message.ErrorOutput, message.SessionUid))],
            FailedTestResults = [.. testResultMessage.FailedTestMessages.Select(message => new FailedTestResult(message.Uid, message.DisplayName, message.State, message.Duration, message.Reason, [.. message.Exceptions.Select(e => new FlatException(e.ErrorMessage, e.ErrorType, e.StackTrace))], message.StandardOutput, message.ErrorOutput, message.SessionUid))]
        });
    }

    internal void OnFileArtifactMessages(FileArtifactMessages fileArtifactMessages)
    {
        FileArtifactsReceived?.Invoke(this, new FileArtifactEventArgs
        {
            ExecutionId = fileArtifactMessages.ExecutionId,
            InstanceId = fileArtifactMessages.InstanceId,
            FileArtifacts = [.. fileArtifactMessages.FileArtifacts.Select(message => new FileArtifact(message.FullPath, message.DisplayName, message.Description, message.TestUid, message.TestDisplayName, message.SessionUid))]
        });
    }

    internal void OnSessionEvent(TestSessionEvent sessionEvent)
    {
        SessionEventReceived?.Invoke(this, new SessionEventArgs { SessionEvent = new TestSession(sessionEvent.SessionType, sessionEvent.SessionUid, sessionEvent.ExecutionId) });
    }

    public void Dispose()
    {
        foreach (var namedPipeServer in _testAppPipeConnections)
        {
            namedPipeServer.Dispose();
        }

        WaitOnTestApplicationPipeConnectionLoop();
    }
}
