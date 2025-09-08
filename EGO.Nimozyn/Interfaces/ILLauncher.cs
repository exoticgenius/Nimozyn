namespace EGO.Nimozyn.Interfaces;

public interface ILLauncher;
public interface ILLauncher<in Input, Output> : ILLauncher
{
    public Task<Output> Execute(INimHandler target, Input input);
}
