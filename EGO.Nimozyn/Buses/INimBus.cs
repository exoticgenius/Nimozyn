using EGO.Nimozyn.Interfaces;

namespace EGO.Nimozyn.Buses;

public interface INimBus
{
    Task Run(INimInput input);
    //T Run<T>(INimInput input);
    Task<T> Run<T>(INimInput<T> input);
}
