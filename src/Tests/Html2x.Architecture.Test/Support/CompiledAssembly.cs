using System.Runtime.CompilerServices;

namespace Html2x.Architecture.Test.Support;

internal sealed class CompiledAssembly
{
    private readonly Type _anchorType;

    private CompiledAssembly(Type anchorType)
    {
        _anchorType = anchorType;
    }

    public static CompiledAssembly For<T>() => new(typeof(T));

    public IReadOnlyList<string> FriendAssemblies() =>
        _anchorType.Assembly
            .GetCustomAttributes(typeof(InternalsVisibleToAttribute), false)
            .Cast<InternalsVisibleToAttribute>()
            .Select(static attribute => attribute.AssemblyName)
            .OrderBy(static assemblyName => assemblyName, StringComparer.Ordinal)
            .ToArray();

    public void ShouldContainFriendAssemblies(params string[] expectedAssemblies) =>
        FriendAssemblies().ShouldContainSet(expectedAssemblies);

    public void ShouldHaveFriendAssemblies(params string[] expectedAssemblies) =>
        FriendAssemblies().ShouldBeSet(expectedAssemblies);
}
