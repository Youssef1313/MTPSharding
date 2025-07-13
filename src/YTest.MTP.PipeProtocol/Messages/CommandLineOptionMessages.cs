namespace YTest.MTP.PipeProtocol;

internal sealed record CommandLineOptionMessage(string Name, string Description, bool? IsHidden, bool? IsBuiltIn);

internal sealed record CommandLineOptionMessages(string? ModulePath, CommandLineOptionMessage[] CommandLineOptionMessageList) : IRequest;
