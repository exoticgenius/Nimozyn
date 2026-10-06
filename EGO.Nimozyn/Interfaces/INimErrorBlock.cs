namespace EGO.Nimozyn.Interfaces;

public interface INimErrorBlock : INimBlock
{
    Task Execute(Exception e, CancellationToken ct, object[] @params);
}
