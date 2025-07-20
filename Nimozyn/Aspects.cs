namespace Nimozyn;



public interface INimAspect;
public abstract class ANimAspect : Attribute, INimAspect
{
    public Type BlockType { get; private init; }
    public AspectPosition AspectPosition { get; private init; }

    protected ANimAspect(Type blockType, AspectPosition position = AspectPosition.NotSpecified)
    {
        BlockType = blockType;
        AspectPosition = position;
    }
}

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Assembly | AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
public class NimInputLinkerAttribute<T> : ANimAspect where T : INimInput
{
    public NimInputLinkerAttribute(AspectPosition position = AspectPosition.NotSpecified) : base(typeof(T), position)
    {
    }
}

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Assembly | AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
public class NimAspectLinkerAttribute<T> : ANimAspect where T : INimBlock
{
    public NimAspectLinkerAttribute(AspectPosition position = AspectPosition.NotSpecified) : base(typeof(T), position)
    {
    }
}


[AttributeUsage(AttributeTargets.Class)]
public class NimLifetimeAttribute : Attribute
{
    public NimLifetime Lifetime { get; init; }
    public NimCompatibilityMode CompatibilityMode { get; init; }
    public NimLifetimeAttribute(NimLifetime lifetime, NimCompatibilityMode compatibilityMode)
    {
        Lifetime = lifetime;
        CompatibilityMode = compatibilityMode;
    }
}

public class NimTransientAttribute : NimLifetimeAttribute
{
    public NimTransientAttribute(NimCompatibilityMode compatibilityMode = NimCompatibilityMode.Enforce) :
        base(NimLifetime.Transient, compatibilityMode)
    { }
}

public class NimScopedAttribute : NimLifetimeAttribute
{
    public NimScopedAttribute(NimCompatibilityMode compatibilityMode = NimCompatibilityMode.Enforce) :
        base(NimLifetime.Scoped, compatibilityMode)
    { }
}

public class NimSingletonAttribute : NimLifetimeAttribute
{
    public NimSingletonAttribute(NimCompatibilityMode compatibilityMode = NimCompatibilityMode.Enforce) :
        base(NimLifetime.Singleton, compatibilityMode)
    { }
}
