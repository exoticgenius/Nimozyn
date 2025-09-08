using EGO.Nimozyn.Enums;

namespace EGO.Nimozyn.Attributes;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
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
