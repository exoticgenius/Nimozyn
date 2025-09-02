using System.Collections.Immutable;
using System.Reflection;

using static Nimozyn.NimDiscovery;

namespace EGO.Nimozyn;

public class ExpandedHandler
{
    public required Type ServiceType { get; init; }
    public required ImmutableList<ExpandedHandlerMethod> Methods { get; init; }
    public NimLifetime Lifetime { get; set; }
    public required BaseMatrix BaseMatrix { get; init; }
}

public class ExpandedHandlerMethod
{
    public ExpandedHandler? HandlerWrapper { get; set; }
    public required Type InputType { get; init; }
    public required MethodInfo handlerMethod { get; init; }
    public NimLifetime Lifetime { get; init; }
    public NimCompatibilityMode CompatibilityMode { get; init; }
    public Type LauncherType { get; init; }
    public ILLauncher LauncherInstance { get; init; }
}
public class NimServiceDescriptor
{
    public List<INimTransparentBlock> InputFilterBlocks { get; set; }
    public List<INimBlock> PreExecuteBlocks { get; set; }
    public List<INimErrorBlock> OnErrorBlocks { get; set; }
    public List<INimBlock> PostExecuteBlocks { get; set; }
    public List<INimTransparentBlock> OutputFilterBlocks { get; set; }
}