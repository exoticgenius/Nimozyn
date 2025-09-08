using EGO.Nimozyn.Descriptors;

using System.Collections.Immutable;
using System.Diagnostics;

namespace EGO.Nimozyn.Managers;

internal class NimManager
{
    internal Dictionary<Type, ExpandedHandlerMethod> InputTypes;

    public NimManager(ImmutableList<ExpandedHandler> handlers)
    {
        InputTypes = handlers.SelectMany(x => x.Methods)
            .ToDictionary(x => x.InputType, x => x);
    }

    [DebuggerStepThrough]
    public ExpandedHandlerMethod? GetHandlerMethod(Type inputType)
    {
        if (InputTypes.TryGetValue(inputType, out var method))
            return method;

        return null;
    }
}
