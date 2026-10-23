using System;
using System.Runtime.CompilerServices;

// Shim for the compiler-emitted helper that ilspy could not round-trip.
// The original assembly carries a <PrivateImplementationDetails> class whose name
// is mangled to _003CPrivateImplementationDetails_003E in metadata; callers use
// global::_003CPrivateImplementationDetails_003E.ThrowSwitchExpressionException(...)
[CompilerGenerated]
internal static class _003CPrivateImplementationDetails_003E
{
    [CompilerGenerated]
    internal static void ThrowSwitchExpressionException(object unreachable)
    {
        throw new SwitchExpressionException(unreachable);
    }

    [CompilerGenerated]
    internal static void ThrowSwitchExpressionException(int unreachable)
    {
        throw new SwitchExpressionException(unreachable);
    }
}
