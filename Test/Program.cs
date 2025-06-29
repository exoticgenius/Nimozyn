using Microsoft.Extensions.DependencyInjection;

using Nimozyn;

[assembly: NimAspectLinker<NimLogger1>(ServiceLifetime.Scoped)]

var col = new ServiceCollection();

col.AddTransient<ITestService, TestService>();

col.ScanNimHandlersAsync();

var provider = col.BuildServiceProvider();

Console.ReadLine();


public interface ITestService : INimHandler
{
    int TestMethod1(TestRequest1 req);
    string TestMethod2(TestRequest2 req);
}

[NimAspectLinker<NimLogger1>(ServiceLifetime.Scoped, AspectPosition.Pre)]
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
public class NimLogger1 : INimNeutralBlock
{
    public Task Execute()
    {
        throw new NotImplementedException();
    }
}