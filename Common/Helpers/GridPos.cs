namespace Common.Helpers
{
    public readonly record struct GridPos (int Row, int Col);
}

namespace System.Runtime.CompilerServices
{
    // This is a "dummy" class that lets the compiler use 'init' and 'record' 
    // properties on older .NET versions.
    public static class IsExternalInit { }
}