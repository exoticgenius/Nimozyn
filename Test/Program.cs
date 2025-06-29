using Microsoft.Extensions.DependencyInjection;

using Nimozyn;

var col = new ServiceCollection();

col.AddTransient<ITestService, TestService>();

col.ScanNimHandlersAsync();

var provider = col.BuildServiceProvider();





public interface ITestService
{
    int TestMethod1(TestRequest1 req);
    string TestMethod2(TestRequest2 req);
}

public class TestService : ITestService
{
    public int TestMethod1(TestRequest1 req)
    {
        return req.Val;
    }

    public string TestMethod2(TestRequest2 req)
    {
        return req.Val;
    }
}

public class TestRequest1 : INimRequest<int>
{
    public int Val { get; set; }
}

public class TestRequest2 : INimRequest<string>
{
    public string Val { get; set; }
}

[NimRequestLinker<TestRequest1>(ServiceLifetime.Scoped)]
public class NimLogger1
{

}