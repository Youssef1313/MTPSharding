using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Testing.Platform.CommandLine;
using Microsoft.Testing.Platform.Extensions;
using Microsoft.Testing.Platform.Extensions.CommandLine;

internal sealed class BatchingCommandLineOptionsProvider : ICommandLineOptionsProvider
{
    public static int? BatchCount { get; private set; }

    public static string? DotnetTestPipeName { get; private set; }

    public string Uid => nameof(BatchingCommandLineOptionsProvider);

    public string Version => "1.0.0";

    public string DisplayName => "Batching support";

    public string Description => "Batching support";

    public IReadOnlyCollection<CommandLineOption> GetCommandLineOptions()
        => [
            new CommandLineOption("batch-count", "batch count", ArgumentArity.ExactlyOne, isHidden: false),
           ];

    public Task<bool> IsEnabledAsync() => Task.FromResult(true);

    public Task<ValidationResult> ValidateCommandLineOptionsAsync(ICommandLineOptions commandLineOptions)
    {
        if (commandLineOptions.TryGetOptionArgumentList("batch-count", out _) &&
            commandLineOptions.TryGetOptionArgumentList("list-tests", out _))
        {
            return ValidationResult.InvalidTask("The --batch-count option cannot be used with --list-tests.");
        }

        if (commandLineOptions.TryGetOptionArgumentList("server", out var serverArgs) &&
            serverArgs.Length == 1 &&
            serverArgs[0] == "dotnettestcli")
        {
            if (!commandLineOptions.TryGetOptionArgumentList("dotnet-test-pipe", out var pipeArguments) ||
                pipeArguments.Length != 1)
            {
                return ValidationResult.InvalidTask("The --dotnet-test-pipe option must be specified with a single argument when using the --server dotnettestcli.");
            }

            DotnetTestPipeName = pipeArguments[0];
        }

        return ValidationResult.ValidTask;
    }

    public Task<ValidationResult> ValidateOptionArgumentsAsync(CommandLineOption commandOption, string[] arguments)
    {
        if (commandOption.Name == "batch-count")
        {
            if (!int.TryParse(arguments[0], out var batchCount) || batchCount <= 1)
            {
                return ValidationResult.InvalidTask("--batch-count must be integer and must be greater than 1.");
            }

            BatchCount = batchCount;
        }

        return ValidationResult.ValidTask;
    }
}
