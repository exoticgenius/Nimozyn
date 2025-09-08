using EGO.Nimozyn.Interfaces;

namespace EGO.Nimozyn.Buses;

public interface INimBus
{
    void Run(INimInput input);
    T Run<T>(INimInput input);
    Task<T> Run<T>(INimInput<T> input);
}
