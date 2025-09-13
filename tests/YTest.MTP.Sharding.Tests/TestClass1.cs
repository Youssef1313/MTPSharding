using System;
using System.Threading;
using System.Threading.Tasks;

namespace YTest.MTP.Sharding.Tests;

[TestClass]
public class TestClass1
{
    [TestMethod]
    public async Task TestMethod1()
    {
        await Task.Delay(5000);
    }
}
