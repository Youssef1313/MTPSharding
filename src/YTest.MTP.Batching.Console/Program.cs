using System;
using YTest.MTP.PipeProtocol;

var discoverer = new MTPPipeDiscoverer("C:\\Users\\ygerges\\Desktop\\MTPBatching\\artifacts\\bin\\YTest.MTP.Batching.Tests\\debug\\YTest.MTP.Batching.Tests.exe", "");
var tests = await discoverer.DiscoverTests();
foreach (var test in tests)
{
    Console.WriteLine(test.Uid + ", " + test.DisplayName);
}
