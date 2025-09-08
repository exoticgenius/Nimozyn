using EGO.Nimozyn.Enums;

namespace EGO.Nimozyn.Attributes;

public class NimSingletonAttribute : NimLifetimeAttribute
{
    public NimSingletonAttribute(NimCompatibilityMode compatibilityMode = NimCompatibilityMode.Enforce) :
        base(NimLifetime.Singleton, compatibilityMode)
    { }
}
