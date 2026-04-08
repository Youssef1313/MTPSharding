using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Testing.Platform.CommandLine;
using Microsoft.Testing.Platform.Extensions;
using Microsoft.Testing.Platform.Extensions.CommandLine;

internal sealed class ShardingCommandLineOptionsProvider : ICommandLineOptionsProvider
{
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
        }

        return ValidationResult.ValidTask;
    }
}
