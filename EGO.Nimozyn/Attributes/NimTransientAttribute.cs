using EGO.Nimozyn.Enums;

namespace EGO.Nimozyn.Attributes;

public class NimTransientAttribute : NimLifetimeAttribute
{
    public NimTransientAttribute(NimCompatibilityMode compatibilityMode = NimCompatibilityMode.Enforce) :
        base(NimLifetime.Transient, compatibilityMode)
    { }
}
