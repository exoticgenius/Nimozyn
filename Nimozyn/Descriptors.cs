using System.Collections.Immutable;
using System.Reflection;

using static Nimozyn.NimDiscovery;

namespace Nimozyn;

internal class ExpandedHandler
{
    public required Type ServiceType { get; init; }
    public required ImmutableList<ExpandedHandlerMethod> Methods { get; init; }
    public NimLifetime Lifetime { get; set; }
    public required BaseMatrix BaseMatrix { get; init; }
}

internal class ExpandedHandlerMethod
{
    public ExpandedHandler? HandlerWrapper { get; set; }
    public required Type InputType { get; set; }
    public required MethodInfo handlerMethod { get; set; }
    public NimLifetime Lifetime { get; set; }
    public NimCompatibilityMode CompatibilityMode { get; set; }
}
public class NimServiceDescriptor
{
    public List<INimTransparentBlock> InputFilterBlocks { get; set; }
    public List<INimBlock> PreExecuteBlocks { get; set; }
    public List<INimErrorBlock> OnErrorBlocks { get; set; }
    public List<INimBlock> PostExecuteBlocks { get; set; }
    public List<INimTransparentBlock> OutputFilterBlocks { get; set; }
}