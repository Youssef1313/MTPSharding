using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Pipes;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace YTest.MTP.PipeProtocol;

internal sealed class TestApplication : IDisposable
{
    private readonly List<string> _outputData = [];
    private readonly List<string> _errorData = [];
    private readonly PipeNameDescription _pipeNameDescription = NamedPipeServer.GetPipeName(Guid.NewGuid().ToString("N"));
    private readonly CancellationTokenSource _cancellationToken = new();
    private readonly string _pathToExe;
    private readonly string _arguments;
    private readonly string? _workingDirectory;

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
    public event EventHandler<TestResultEventArgs>? TestResultsReceived;
    public event EventHandler<FileArtifactEventArgs>? FileArtifactsReceived;
    public event EventHandler<SessionEventArgs>? SessionEventReceived;
    public event EventHandler<TestProcessExitEventArgs>? TestProcessExited;

    public async Task<int> RunAsync()
    {
        var processStartInfo = CreateProcessStartInfo(_pathToExe, _arguments, _workingDirectory);

        _testAppPipeConnectionLoop = Task.Run(async () => await WaitConnectionAsync(_cancellationToken.Token).ConfigureAwait(false), _cancellationToken.Token);
        var testProcessResult = await StartProcess(processStartInfo).ConfigureAwait(false);

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

    private Task<IResponse> OnRequest(IRequest request)
    {
        try
        {
            switch (request)
            {
                case HandshakeMessage handshakeMessage:
                    if (handshakeMessage.Properties.TryGetValue(HandshakeMessagePropertyNames.ModulePath, out string? value))
                    {
                        OnHandshakeMessage(handshakeMessage);

                        return Task.FromResult((IResponse)CreateHandshakeMessage(GetSupportedProtocolVersion(handshakeMessage)));
                    }
                    break;

                case CommandLineOptionMessages commandLineOptionMessages:
                    OnCommandLineOptionMessages(commandLineOptionMessages);
                    break;

                case DiscoveredTestMessages discoveredTestMessages:
                    OnDiscoveredTestMessages(discoveredTestMessages);
                    break;

                case TestResultMessages testResultMessages:
                    OnTestResultMessages(testResultMessages);
                    break;

                case FileArtifactMessages fileArtifactMessages:
                    OnFileArtifactMessages(fileArtifactMessages);
                    break;

                case TestSessionEvent sessionEvent:
                    OnSessionEvent(sessionEvent);
                    break;

                // If we don't recognize the message, log and skip it
                case UnknownMessage unknownMessage:
                    return Task.FromResult((IResponse)VoidResponse.CachedInstance);

                default:
                    // If it doesn't match any of the above, throw an exception
                    throw new NotSupportedException($"Message Request type '{request.GetType()}' is unsupported.");
            }
        }
        catch (Exception ex)
        {
            Environment.FailFast(ex.ToString());
        }

        return Task.FromResult((IResponse)VoidResponse.CachedInstance);
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
            { HandshakeMessagePropertyNames.SupportedProtocolVersions, version }
        });

    private
#if NET
        async
#endif
        Task<int> StartProcess(ProcessStartInfo processStartInfo)
    {
        var process = Process.Start(processStartInfo)!;
        StoreOutputAndErrorData(process);
#if NET
        await process.WaitForExitAsync().ConfigureAwait(false);
#else
        process.WaitForExit();
#endif
        TestProcessExited?.Invoke(this, new TestProcessExitEventArgs { OutputData = _outputData, ErrorData = _errorData, ExitCode = process.ExitCode });

#if NET
        return process.ExitCode;
#else
        return Task.FromResult(process.ExitCode);
#endif
    }

    private void StoreOutputAndErrorData(Process process)
    {
        process.EnableRaisingEvents = true;

        process.OutputDataReceived += (sender, e) => {
            if (string.IsNullOrEmpty(e.Data))
                return;

            _outputData.Add(e.Data);
        };
        process.ErrorDataReceived += (sender, e) => {
            if (string.IsNullOrEmpty(e.Data))
                return;

            _errorData.Add(e.Data);
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
            DiscoveredTests = [.. discoveredTestMessages.DiscoveredMessages.Select(message => new DiscoveredTest(message.Uid, message.DisplayName))]
        });
    }

    internal void OnTestResultMessages(TestResultMessages testResultMessage)
    {
        TestResultsReceived?.Invoke(this, new TestResultEventArgs
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
