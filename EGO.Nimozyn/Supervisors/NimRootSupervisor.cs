using Microsoft.Extensions.DependencyInjection;

namespace EGO.Nimozyn.Supervisors;

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
