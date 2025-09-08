using Microsoft.Extensions.DependencyInjection;

namespace EGO.Nimozyn.Supervisors;
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
