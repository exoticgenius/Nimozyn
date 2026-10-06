namespace EGO.Nimozyn.Interfaces;

public interface INimNeutralBlock : INimBlock
{
    Task Execute(CancellationToken ct);
}
