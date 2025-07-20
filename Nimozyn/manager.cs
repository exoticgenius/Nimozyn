using System.Collections.Immutable;

namespace Nimozyn;

internal class NimManager
{
    internal Dictionary<Type, ExpandedHandlerMethod> InputTypes;

    public NimManager(ImmutableList<ExpandedHandler> handlers)
    {
        InputTypes = handlers.SelectMany(x => x.Methods)
            .ToDictionary(x => x.InputType, x => x);
    }

    public ExpandedHandlerMethod? GetHandlerMethod(Type inputType)
    {
        if (InputTypes.TryGetValue(inputType, out var method))
            return method;

        return null;
    }
}
