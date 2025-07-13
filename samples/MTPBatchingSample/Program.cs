using Microsoft.Testing.Platform.Builder;
using MTPBatchingSample.MSTest;

ITestApplicationBuilder builder = await TestApplication.CreateBuilderAsync(args);
SelfRegisteredExtensions.AddSelfRegisteredExtensions(builder, args);
using ITestApplication app = await builder.BuildAsync();
return await app.RunAsync();
