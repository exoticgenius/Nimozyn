namespace EGO.Nimozyn.Interfaces;

public interface INimHandler;

public interface INimHandler<T> : INimHandler where T: INimInput
{
    Task Handle(T input, CancellationToken ct);
}

public interface INimHandler<T, R> : INimHandler where T : INimInput<R>
{
    Task<R> Handle(T input, CancellationToken ct);
}
