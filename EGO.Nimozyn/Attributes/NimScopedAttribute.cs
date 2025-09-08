using EGO.Nimozyn.Enums;

namespace EGO.Nimozyn.Attributes;

public class NimScopedAttribute : NimLifetimeAttribute
{
    public NimScopedAttribute(NimCompatibilityMode compatibilityMode = NimCompatibilityMode.Enforce) :
        base(NimLifetime.Scoped, compatibilityMode)
    { }
}
