using EGO.Nimozyn.Enums;

using System.Collections.Immutable;

using static EGO.Nimozyn.Discoveries.NimDiscovery;

namespace EGO.Nimozyn.Descriptors;

public class ExpandedHandler
{
    public required Type ServiceType { get; init; }
    public required ImmutableList<ExpandedHandlerMethod> Methods { get; init; }
    public NimLifetime Lifetime { get; set; }
    public required BaseMatrix BaseMatrix { get; init; }
}
