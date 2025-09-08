using EGO.Nimozyn.Enums;
using EGO.Nimozyn.Interfaces;

using System.Reflection;

namespace EGO.Nimozyn.Descriptors;

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
