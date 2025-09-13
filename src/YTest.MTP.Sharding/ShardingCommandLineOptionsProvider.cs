using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Testing.Platform.CommandLine;
using Microsoft.Testing.Platform.Extensions;
using Microsoft.Testing.Platform.Extensions.CommandLine;

internal sealed class ShardingCommandLineOptionsProvider : ICommandLineOptionsProvider
{
    public static int? ShardCount { get; private set; }

    public static string? DotnetTestPipeName { get; private set; }

    public string Uid => nameof(ShardingCommandLineOptionsProvider);

    public string Version => "1.0.0";

    public string DisplayName => "Sharding support";

    public string Description => "Sharding support";

    public IReadOnlyCollection<CommandLineOption> GetCommandLineOptions()
        => [
            new CommandLineOption("shard-count", "shard count", ArgumentArity.ExactlyOne, isHidden: false),
           ];

    public Task<bool> IsEnabledAsync() => Task.FromResult(true);

    public Task<ValidationResult> ValidateCommandLineOptionsAsync(ICommandLineOptions commandLineOptions)
    {
        if (commandLineOptions.TryGetOptionArgumentList("shard-count", out _) &&
            commandLineOptions.TryGetOptionArgumentList("list-tests", out _))
        {
            return ValidationResult.InvalidTask("The --shard-count option cannot be used with --list-tests.");
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
        if (commandOption.Name == "shard-count")
        {
            if (!int.TryParse(arguments[0], out var shardCount) || shardCount <= 1)
            {
                return ValidationResult.InvalidTask("--shard-count must be integer and must be greater than 1.");
            }

            ShardCount = shardCount;
        }

        return ValidationResult.ValidTask;
    }
}
