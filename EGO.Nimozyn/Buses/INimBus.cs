using EGO.Nimozyn.Interfaces;

namespace EGO.Nimozyn.Buses;

public interface INimBus
{
    Task RunAsync(INimInput input, CancellationToken ct);
    Task<T> RunAsync<T>(INimInput<T> input, CancellationToken ct);
}
