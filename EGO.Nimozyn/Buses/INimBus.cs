using EGO.Nimozyn.Interfaces;

namespace EGO.Nimozyn.Buses;

public interface INimBus
{
    Task RunAsync(INimInput input);

    Task<T> RunAsync<T>(INimInput<T> input);
}
