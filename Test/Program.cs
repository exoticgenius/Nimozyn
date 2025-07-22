using Impl;

using Microsoft.Extensions.DependencyInjection;

using Nimozyn;

[assembly: NimAspectLinker<NimLogger1>(AspectPosition.Wrap)]
[assembly: NimAspectLinker<OutputFilter>(AspectPosition.Post)]

var col = new ServiceCollection();

//col.AddTransient<ITestService, TestService>();


col.ScanNimHandlersAsync();
ServiceProvider provider = col.BuildServiceProvider(true);

var scope = provider.CreateScope();
var bus = scope.ServiceProvider.GetService<INimBus>();

var res = bus.Run(new TestInput1 { Val = 10 });
var res2 = bus.Run(new TestInput1 { Val = 120 });
Console.WriteLine("end");
Console.ReadLine();

Console.WriteLine();

