using System;
using System.Threading;
using System.Threading.Tasks;

namespace YTest.MTP.Sharding.Tests;

[TestClass]
public class TestClass1
{
    private static TaskCompletionSource s_tcs = new TaskCompletionSource();

    [TestMethod]
    public async Task TestMethod1()
    {
        await Task.Delay(5000);
        s_tcs.SetResult();
    }

    [TestMethod]
    public async Task TestMethod2()
    {
        await s_tcs.Task;
        Thread.Sleep(100);
        Environment.FailFast("CRASH TEST HOST!");
    }

    [TestMethod]
    public async Task TestMethod3()
    {
        await s_tcs.Task;
    }

    [TestMethod]
    public async Task TestMethod4()
    {
        await s_tcs.Task;
    }

    [TestMethod]
    public async Task TestMethod5()
    {
        await s_tcs.Task;
    }

    [TestMethod]
    public async Task TestMethod6()
    {
        await s_tcs.Task;
    }

    [TestMethod]
    public async Task TestMethod7()
    {
        await s_tcs.Task;
    }

    [TestMethod]
    public async Task TestMethod8()
    {
        await s_tcs.Task;
    }

    [TestMethod]
    public async Task TestMethod9()
    {
        await s_tcs.Task;
    }

    [TestMethod]
    public async Task TestMethod10()
    {
        await s_tcs.Task;
    }

    [TestMethod]
    public async Task TestMethod11()
    {
        await s_tcs.Task;
    }
}
