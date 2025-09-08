using EGO.Nimozyn.Enums;
using EGO.Nimozyn.Interfaces;

namespace EGO.Nimozyn.Attributes;

public class NimAspectLinkerAttribute<T> : ANimAspect where T : INimBlock
{
    public NimAspectLinkerAttribute(AspectPosition position = AspectPosition.NotSpecified) : base(typeof(T), position)
    {
    }
}
