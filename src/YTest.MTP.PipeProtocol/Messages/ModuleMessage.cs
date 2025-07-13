namespace YTest.MTP.PipeProtocol;

internal sealed record ModuleMessage(string DllOrExePath, string ProjectPath, string TargetFramework, string IsTestingPlatformApplication) : IRequest;
