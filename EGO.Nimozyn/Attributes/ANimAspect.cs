using EGO.Nimozyn.Enums;

namespace EGO.Nimozyn.Attributes;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Assembly | AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
public abstract class ANimAspect : Attribute
{
    public Type BlockType { get; private init; }
    public AspectPosition AspectPosition { get; private init; }

    protected ANimAspect(Type blockType, AspectPosition position = AspectPosition.NotSpecified)
    {
        BlockType = blockType;
        AspectPosition = position;
    }
}
