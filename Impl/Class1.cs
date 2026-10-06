
using EGO.Nimozyn.Attributes;
using EGO.Nimozyn.Buses;
using EGO.Nimozyn.Enums;
using EGO.Nimozyn.Interfaces;
using EGO.Nimozyn.Supervisors;

namespace Impl;

public interface ITestService : INimHandler
{
    Task<int> TestMethod1(TestInput1 req, CancellationToken ct);
    string TestMethod2(TestInput2    req);
}



[NimAspectLinker<OutputFilter>(AspectPosition.Post)]
[NimAspectLinker<NimLogger1>(AspectPosition.Pre)]
[NimScoped]
public class TestService : ITestService
{
    private readonly INimBus bus;
    private          int     x = 0;
    public TestService(INimBus bus)
    {
        this.bus = bus;
        x        = 2;
    }
    public async Task<int> TestMethod1(TestInput1 req, CancellationToken ct)
    {
        await Task.Yield();

        try
        {
            await Task.Delay(20_000, ct);
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
        }
        var res = await bus.RunAsync(new TestInput2 { Val = "Hello" }, ct);
        Console.WriteLine("inside");
        return x;
    }
    public async Task TestMethod3(TestInput3 req, CancellationToken ct)
    {
        await Task.Delay(1000);
        Console.WriteLine("inside");
    }
    public Task<int> Execute(INimHandler target, INimInput input)
    {
        return Task.FromResult(3000);
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

public class TestInput3 : INimInput
{
    public int Val { get; set; }
}
public class TestInput2 : INimInput<string>
{
    public string Val { get; set; }
}

[NimInputLinker<TestInput1>(AspectPosition.Wrap)]
[NimTransient]
[NimScoped]
[NimSingleton]
public class NimLogger1 : INimNeutralBlock
{
    private readonly INimScopeSupervisor supervisor;

    public NimLogger1(INimScopeSupervisor supervisor)
    {
        this.supervisor = supervisor;
    }
    public Task Execute(CancellationToken ct)
    {
        throw new NotImplementedException();
    }
}

public class OutputFilter : INimTransparentBlock<int>
{
    public Task<int> Execute(int Input, CancellationToken ct)
    {
        return Task.FromResult(Input + 1);
    }
}