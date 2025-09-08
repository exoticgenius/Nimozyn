namespace EGO.Nimozyn.Interfaces;

public interface INimHandler;
public interface INimHandler<T> : INimHandler where T : INimInput;
