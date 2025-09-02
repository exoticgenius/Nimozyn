using Microsoft.Extensions.DependencyInjection;

namespace EGO.Nimozyn;

public interface INimSupervisor
{
    object? GetService(Type serviceType);
    object GetRequiredService(Type serviceType);
}

public interface INimRootSupervisor : INimSupervisor;
public class NimRootSupervisor : INimRootSupervisor
{
    private readonly IServiceProvider sp;

    public NimRootSupervisor(IServiceProvider sp)
    {
        this.sp = sp;
    }

    public object? GetService(Type serviceType)
    {
        return sp.GetService(serviceType);
    }

    public object GetRequiredService(Type serviceType)
    {
        return sp.GetRequiredService(serviceType);
    }
}

public interface INimScopeSupervisor : INimSupervisor;
public class NimScopeSupervisor : INimScopeSupervisor
{
    private readonly IServiceProvider sp;

    public NimScopeSupervisor(IServiceProvider sp)
    {
        this.sp = sp;
    }

    public object? GetService(Type serviceType)
    {
        return sp.GetService(serviceType);
    }

    public object GetRequiredService(Type serviceType)
    {
        return sp.GetRequiredService(serviceType);
    }
}
