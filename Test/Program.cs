using Microsoft.Extensions.DependencyInjection;

using Nimozyn;

[assembly: NimAspectLinker<NimLogger1>(AspectPosition.Wrap)]
[assembly: NimAspectLinker<OutputFilter>(AspectPosition.Post)]

var col = new ServiceCollection();

col.AddTransient<ITestService, TestService>();

col.ScanNimHandlersAsync();

IServiceProvider provider = col.BuildServiceProvider();

Console.ReadLine();


public interface ITestService : INimHandler
{
    int TestMethod1(TestInput1 req);
    string TestMethod2(TestInput2 req);
}

[NimAspectLinker<OutputFilter>(AspectPosition.Post)]
[NimAspectLinker<NimLogger1>(AspectPosition.Pre)]
public class TestService : ITestService
{
    public int TestMethod1(TestInput1 req)
    {
        return req.Val;
    }

    [NimAspectLinker<OutputFilter>(AspectPosition.Post)]
    [NimAspectLinker<NimLogger1>(AspectPosition.Post)]
    public string TestMethod2(TestInput2 req)
    {
        return req.Val;
    }
}

public class TestInput1 : INimInput<int>
{
    public int Val { get; set; }
}

public class TestInput2 : INimInput<string>
{
    public string Val { get; set; }
}

[NimInputLinker<TestInput1>(AspectPosition.Wrap)]
public class NimLogger1 : INimNeutralBlock
{
    public Task Execute()
    {
        throw new NotImplementedException();
    }
}

public class OutputFilter : INimTransparentBlock<int>
{
    public Task<int> Execute(int Input)
    {
        return Task.FromResult(Input + 1);
    }
}
