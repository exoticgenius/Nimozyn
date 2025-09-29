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

    //[DebuggerStepThrough]
    //public void Run(INimInput input)
    //{
    //    var handler = manager.GetHandlerMethod(input.GetType());

    //    if (handler is null)
    //        throw new InvalidOperationException($"No handler found for input type {input.GetType().Name}");

    //    var service = serviceProvider.GetRequiredService(handler?.HandlerWrapper?.ServiceType ?? throw new InvalidOperationException("No handler found for input type"));

    //    _ = handler.handlerMethod.Invoke(service, [input]);
    //}

    //[DebuggerStepThrough]
    //public T Run<T>(INimInput input)
    //{
    //    var handler = manager.GetHandlerMethod(input.GetType());

    //    if (handler is null)
    //        throw new InvalidOperationException($"No handler found for input type {input.GetType().Name}");

    //    if (handler.handlerMethod.ReturnType != typeof(T) && handler.handlerMethod.ReturnType != typeof(Task<T>))
    //        throw new InvalidOperationException($"Handler method {handler.handlerMethod.Name} does not return type {typeof(T).Name}");

    //    var service = serviceProvider.GetRequiredService(handler?.HandlerWrapper?.ServiceType ?? throw new InvalidOperationException("No handler found for input type"));

    //    var res = handler.handlerMethod.Invoke(service, [input]);

    //    return (T)res;
    //}

    [DebuggerStepThrough]
    public Task<T> Run<T>(INimInput<T> input)
    {
        PrepareData(input, out var handler, out var service);

        return ((ILLauncher<INimInput, Task<T>>)handler.LauncherInstance).Execute(service, input);
    }

    [DebuggerStepThrough]
    public Task Run(INimInput input)
    {
        PrepareData(input, out var handler, out var service);

        return ((ILLauncher<INimInput, Task>)handler.LauncherInstance).Execute(service, input);
    }
    [DebuggerStepThrough]
    private void PrepareData(INimInput input, out ExpandedHandlerMethod handler, out INimHandler service)
    {
        handler = manager.GetHandlerMethod(input.GetType()) ??
            throw new NoNullAllowedException(); ;

        service = (INimHandler)serviceProvider
            .GetRequiredService(handler.HandlerWrapper!.ServiceType) ??
            throw new NoNullAllowedException("No handler found for input type");
    }
}
