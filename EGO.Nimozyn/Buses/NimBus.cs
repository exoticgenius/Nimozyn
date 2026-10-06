using EGO.Nimozyn.Descriptors;
using EGO.Nimozyn.Interfaces;
using EGO.Nimozyn.Managers;

using Microsoft.Extensions.DependencyInjection;

using System;
using System.Data;
using System.Diagnostics;

namespace EGO.Nimozyn.Buses;

internal sealed class NimBus : INimBus
{
    private readonly IServiceProvider serviceProvider;
    private readonly NimManager manager;

    public NimBus(IServiceProvider serviceProvider, NimManager manager)
    {
        this.serviceProvider = serviceProvider;
        this.manager = manager;
    }

    [DebuggerStepThrough]
    public Task<T> Run<T>(INimInput<T> input, CancellationToken ct)
    {
        PrepareData(input, ct, out var handler, out var service);

        return ((ILLauncher<INimInput, Task<T>>)handler.LauncherInstance).Execute(service, input, ct);
    }

    [DebuggerStepThrough]
    public Task Run(INimInput input, CancellationToken ct)
    {
        PrepareData(input, ct, out var handler, out var service);

        return ((ILLauncher<INimInput, Task>)handler.LauncherInstance).Execute(service, input, ct);
    }
    [DebuggerStepThrough]
    private void PrepareData(INimInput input, CancellationToken ct, out ExpandedHandlerMethod handler, out INimHandler service)
    {
        handler = manager.GetHandlerMethod(input.GetType()) ??
            throw new NoNullAllowedException(); ;

        service = (INimHandler)serviceProvider
            .GetRequiredService(handler.HandlerWrapper!.ServiceType) ??
            throw new NoNullAllowedException("No handler found for input type");
    }
}
