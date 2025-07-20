namespace Nimozyn;

public enum NimLifetime
{
    Singleton,
    Scoped,
    Transient,
}

public enum NimCompatibilityMode
{
    Enforce,
    Ignore,
    Abort,
}

public enum AspectPosition
{
    NotSpecified,
    Pre,
    Post,
    Wrap,
}
