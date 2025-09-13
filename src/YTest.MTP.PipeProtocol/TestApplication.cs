using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

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

internal sealed class TestApplication : IDisposable
{
    private readonly string _pipeName = NamedPipeServer.GetPipeName(Guid.NewGuid().ToString("N"));
    private readonly string _pathToExe;
    private readonly string _arguments;
    private readonly string? _workingDirectory;
    private Task? _afterProcessStartTask;

    private readonly List<NamedPipeServer> _testAppPipeConnections = [];
    private readonly Dictionary<NamedPipeServer, HandshakeMessage> _handshakes = new();

    public TestApplication(string pathToExe, string arguments, string? workingDirectory = null)
    {
        _pathToExe = pathToExe;
        _arguments = arguments;
        _workingDirectory = workingDirectory;
    }

    public Func<DiscoveredTestMessages, Task>? OnDiscovered { get; set; }
    public Func<TestResultMessages, Task>? OnTestResult { get; set; }

    public async Task<TestProcessExitInformation> RunAsync(Func<int, Task>? afterProcessStartCallback = null)
    {
        var processStartInfo = CreateProcessStartInfo(_pathToExe, _arguments, _workingDirectory);

        var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        var testAppPipeConnectionLoop = Task.Run(async () => await WaitConnectionAsync(cancellationToken).ConfigureAwait(false));

        try
        {
            using var process = Process.Start(processStartInfo)!;
            if (afterProcessStartCallback is not null)
            {
                var afterProcessStartTask = afterProcessStartCallback(process.Id);
                _afterProcessStartTask = afterProcessStartTask;
                await afterProcessStartTask.ConfigureAwait(false);
            }

            // Reading from process stdout/stderr is done on separate threads to avoid blocking IO on the threadpool.
            // Note: even with 'process.StandardOutput.ReadToEndAsync()' or 'process.BeginOutputReadLine()', we ended up with
            // many TP threads just doing synchronous IO, slowing down the progress of the test run.
            // We want to read requests coming through the pipe and sending responses back to the test app as fast as possible.
            var stdOutTask = Task.Factory.StartNew(static standardOutput => ((StreamReader)standardOutput!).ReadToEnd(), process.StandardOutput, TaskCreationOptions.LongRunning);
            var stdErrTask = Task.Factory.StartNew(static standardError => ((StreamReader)standardError!).ReadToEnd(), process.StandardError, TaskCreationOptions.LongRunning);
            var outputAndError = await Task.WhenAll(stdOutTask, stdErrTask).ConfigureAwait(false);

#if NET
            await process.WaitForExitAsync().ConfigureAwait(false);
#else
            process.WaitForExit();
#endif

            return new TestProcessExitInformation { StandardOutput = outputAndError[0], StandardError = outputAndError[1], ExitCode = process.ExitCode };
        }
        finally
        {
            cancellationTokenSource.Cancel();
            await testAppPipeConnectionLoop;
        }
    }

    private ProcessStartInfo CreateProcessStartInfo(string pathToExe, string arguments, string? workingDirectory)
    {
        var processStartInfo = new ProcessStartInfo
        {
            FileName = pathToExe,
            Arguments = $"{arguments} --server dotnettestcli --dotnet-test-pipe {_pipeName}",
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

    private async Task WaitConnectionAsync(CancellationToken token)
    {
        try
        {
            while (!token.IsCancellationRequested)
            {
                var pipeConnection = new NamedPipeServer(_pipeName, OnRequest, NamedPipeServerStream.MaxAllowedServerInstances, token);
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

    private async Task<IResponse> OnRequest(NamedPipeServer server, IRequest request)
    {
        try
        {
            switch (request)
            {
                case HandshakeMessage handshakeMessage:
                    _handshakes.Add(server, handshakeMessage);
                    if (_afterProcessStartTask is not null)
                    {
                        SpinWait.SpinUntil(() => _afterProcessStartTask.IsCompleted);
                    }

                    if (handshakeMessage.Properties.TryGetValue(HandshakeMessagePropertyNames.ModulePath, out string? value))
                    {
                        return CreateHandshakeMessage(GetSupportedProtocolVersion(handshakeMessage));
                    }
                    break;

                case CommandLineOptionMessages commandLineOptionMessages:
                    break;

                case DiscoveredTestMessages discoveredTestMessages:
                    if (OnDiscovered is not null)
                    {
                        await OnDiscovered(discoveredTestMessages).ConfigureAwait(false);
                    }
                    break;

                case TestResultMessages testResultMessages:
                    if (OnTestResult is not null)
                    {
                        await OnTestResult(testResultMessages).ConfigureAwait(false);
                    }
                    break;

                case FileArtifactMessages fileArtifactMessages:
                    break;

                case TestSessionEvent sessionEvent:
                    break;

                // If we don't recognize the message, log and skip it
                case UnknownMessage unknownMessage:
                    return VoidResponse.CachedInstance;

                default:
                    // If it doesn't match any of the above, throw an exception
                    throw new NotSupportedException($"Message Request type '{request.GetType()}' is unsupported.");
            }
        }
        catch (Exception ex)
        {
            Environment.FailFast(ex.ToString());
        }

        return VoidResponse.CachedInstance;
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

    private static HandshakeMessage CreateHandshakeMessage(string version)
    {
#if NET
        var processId = Environment.ProcessId.ToString();
#else
        using var process = Process.GetCurrentProcess();
        var processId = process.Id.ToString();
#endif
        return new HandshakeMessage(new Dictionary<byte, string>
        {
            { HandshakeMessagePropertyNames.PID, processId },
            { HandshakeMessagePropertyNames.Architecture, RuntimeInformation.ProcessArchitecture.ToString() },
            { HandshakeMessagePropertyNames.Framework, RuntimeInformation.FrameworkDescription },
            { HandshakeMessagePropertyNames.OS, RuntimeInformation.OSDescription },
            { HandshakeMessagePropertyNames.SupportedProtocolVersions, version },
            { HandshakeMessagePropertyNames.IsIDE, "true" }, // TODO: Make it user configurable.
        });
    }


    public void Dispose()
    {
        Exception? exceptionAggregation = null;
        foreach (var namedPipeServer in _testAppPipeConnections)
        {
            try
            {
                namedPipeServer.Dispose();
            }
            catch (Exception ex)
            {
                if (_handshakes.TryGetValue(namedPipeServer, out var handshake))
                {
                    var messageBuilder = new StringBuilder("Error disposing NamedPipeServer corresponding to handshake:");
                    messageBuilder.AppendLine();
                    messageBuilder.AppendLine($"Test executable path: {_pathToExe}");
                    foreach (var kvp in handshake.Properties)
                    {
                        messageBuilder.AppendLine($"{kvp.Key}: {kvp.Value}");
                    }

                    ex = new Exception(messageBuilder.ToString(), ex);
                }
                else
                {
                    var messageBuilder = new StringBuilder("Error disposing NamedPipeServer, and no handshake was found.");
                    messageBuilder.AppendLine();
                    messageBuilder.AppendLine($"Test executable path: {_pathToExe}");
                    ex = new Exception(messageBuilder.ToString(), ex);
                }

                if (exceptionAggregation is null)
                {
                    exceptionAggregation = ex;
                }
                else
                {
                    if (exceptionAggregation is AggregateException aggregateException)
                    {
                        exceptionAggregation = new AggregateException(aggregateException.InnerExceptions.Concat(new[] { ex }));
                    }
                    else
                    {
                        exceptionAggregation = new AggregateException(exceptionAggregation, ex);
                    }
                }
            }
        }

        if (exceptionAggregation is not null)
        {
            throw exceptionAggregation;
        }
    }
}
