namespace EGO.Nimozyn.Interfaces;

public interface INimTransparentBlock : INimBlock;
public interface INimTransparentBlock<T> : INimTransparentBlock
{
    Task<T> Execute(T input);
}