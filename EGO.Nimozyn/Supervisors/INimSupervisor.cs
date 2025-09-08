namespace EGO.Nimozyn.Supervisors;

public interface INimSupervisor
{
    object? GetService(Type serviceType);
    object GetRequiredService(Type serviceType);
}
