using System;
using YTest.MTP.PipeProtocol;

var discoverer = new MTPPipeDiscoverer("C:\\Users\\ygerges\\Desktop\\MTPBatching\\artifacts\\bin\\YTest.MTP.Batching.Tests\\debug\\YTest.MTP.Batching.Tests.exe", "");
var tests = await discoverer.DiscoverTestsAsync();
foreach (var test in tests)
{
    Console.WriteLine(test.Uid + ", " + test.DisplayName);
}

var runner = new MTPPipeRunner("C:\\Users\\ygerges\\Desktop\\MTPBatching\\artifacts\\bin\\YTest.MTP.Batching.Tests\\debug\\YTest.MTP.Batching.Tests.exe", "");
var results = await runner.RunTestsAsync();
