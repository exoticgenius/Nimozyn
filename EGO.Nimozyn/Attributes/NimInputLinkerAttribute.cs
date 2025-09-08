using EGO.Nimozyn.Enums;
using EGO.Nimozyn.Interfaces;

namespace EGO.Nimozyn.Attributes;

public class NimInputLinkerAttribute<T> : ANimAspect where T : INimInput
{
    public NimInputLinkerAttribute(AspectPosition position = AspectPosition.NotSpecified) : base(typeof(T), position)
    {
    }
}
