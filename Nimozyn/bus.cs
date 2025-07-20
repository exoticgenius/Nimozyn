using Microsoft.Extensions.DependencyInjection;

using System.Diagnostics;

namespace Nimozyn;

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
    public async Task Run(INimInput input)
    {
        await Task.Yield();

        var handler = manager.GetHandlerMethod(input.GetType());

        if (handler is null)
            throw new InvalidOperationException($"No handler found for input type {input.GetType().Name}");

        var service = serviceProvider.GetRequiredService(handler?.HandlerWrapper?.ServiceType ?? throw new InvalidOperationException("No handler found for input type"));

        _ = handler.handlerMethod.Invoke(service, [input]);
    }

    [DebuggerStepThrough]
    public async Task<T> Run<T>(INimInput input)
    {
        await Task.Yield();

        var handler = manager.GetHandlerMethod(input.GetType());

        if (handler is null)
            throw new InvalidOperationException($"No handler found for input type {input.GetType().Name}");

        if (handler.handlerMethod.ReturnType != typeof(T) && handler.handlerMethod.ReturnType != typeof(Task<T>))
            throw new InvalidOperationException($"Handler method {handler.handlerMethod.Name} does not return type {typeof(T).Name}");

        var service = serviceProvider.GetRequiredService(handler?.HandlerWrapper?.ServiceType ?? throw new InvalidOperationException("No handler found for input type"));

        var res = handler.handlerMethod.Invoke(service, [input]);

        if (res is Task<T> task)
            return await task;

        return (T)res!;

    }

    [DebuggerStepThrough]
    public async Task<T> Run<T>(INimInput<T> input)
    {
        await Task.Yield();

        var handler = manager.GetHandlerMethod(input.GetType());

        if (handler is null)
            throw new InvalidOperationException($"No handler found for input type {input.GetType().Name}");

        if (handler.handlerMethod.ReturnType != typeof(T) && handler.handlerMethod.ReturnType != typeof(Task<T>))
            throw new InvalidOperationException($"Handler method {handler.handlerMethod.Name} does not return type {typeof(T).Name}");

        var service = serviceProvider.GetRequiredService(handler?.HandlerWrapper?.ServiceType ?? throw new InvalidOperationException("No handler found for input type"));

        var res = handler.handlerMethod.Invoke(service, [input]);

        if (res is Task<T> task)
            return await task;

        return (T)res!;
    }
}
