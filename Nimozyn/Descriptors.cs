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
    public ExpandedHandler? HandlerWrapper { get; internal set; }
    public required Type InputType { get; internal init; }
    public required MethodInfo handlerMethod { get; internal init; }
    public NimLifetime Lifetime { get; internal init; }
    public NimCompatibilityMode CompatibilityMode { get; internal init; }
    public Type LauncherType { get; internal init; }
    public ILLauncher LauncherInstance { get; internal init; }
}
public class NimServiceDescriptor
{
    public List<INimTransparentBlock> InputFilterBlocks { get; set; }
    public List<INimBlock> PreExecuteBlocks { get; set; }
    public List<INimErrorBlock> OnErrorBlocks { get; set; }
    public List<INimBlock> PostExecuteBlocks { get; set; }
    public List<INimTransparentBlock> OutputFilterBlocks { get; set; }
}