namespace EGO.Nimozyn.Interfaces;

public interface INimErrorBlock : INimBlock
{
    Task Execute(Exception e, object[] @params);
}
