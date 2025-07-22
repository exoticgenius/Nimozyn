namespace Nimozyn;

public interface INimHandler;
public interface INimHandler<T> : INimHandler where T : INimInput;


public interface INimInput;
public interface INimInput<T> : INimInput;

public interface ILLauncher;
public interface ILLauncher<in Input, Output> : ILLauncher
{
    public Output Execute(INimHandler target, Input input);
}


public interface INimBlock;
public interface INimNeutralBlock : INimBlock
{
    Task Execute();
}

public interface INimErrorBlock : INimBlock
{
    Task Execute(Exception e, object[] @params);
}

public interface INimTransparentBlock : INimBlock;
public interface INimTransparentBlock<T> : INimTransparentBlock
{
    Task<T> Execute(T input);
}