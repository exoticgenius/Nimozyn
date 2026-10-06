using EGO.Nimozyn.Interfaces;

namespace EGO.Nimozyn.Buses;

public interface INimBus
{
    Task Run(INimInput input, CancellationToken ct);
    Task<T> Run<T>(INimInput<T> input, CancellationToken ct);
}
