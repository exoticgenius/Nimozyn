namespace EGO.Nimozyn.Interfaces;

public interface INimHandler;
public interface INimHandler<T, R> : INimHandler where T : INimInput<R>
{
    Task<R> Handle(T input);
}
