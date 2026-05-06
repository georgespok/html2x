using Shouldly;

namespace Html2x.LayoutEngine.Test.Architecture;

internal sealed class CSharpSourceSet
{
    private CSharpSourceSet(IReadOnlyList<CSharpSourceFile> files)
    {
        Files = files;
    }

    public IReadOnlyList<CSharpSourceFile> Files { get; }

    public static CSharpSourceSet FromDirectory(params string[] pathSegments)
    {
        var directory = ArchitecturePaths.PathFromRoot(pathSegments);
        var files = Directory.GetFiles(directory, "*.cs", SearchOption.AllDirectories)
            .Where(file => !ArchitecturePaths.IsBuildOutputPath(Path.GetRelativePath(directory, file)))
            .Select(CSharpSourceFile.Load)
            .ToArray();

        return new(files);
    }

    public void ShouldDeclareNamespace(string expectedNamespace)
    {
        foreach (var file in Files)
        {
            file.ShouldDeclareNamespace(expectedNamespace);
        }
    }

    public void ShouldNotDeclareNamespaces(params string[] forbiddenNamespaces)
    {
        foreach (var file in Files)
        {
            foreach (var forbiddenNamespace in forbiddenNamespaces)
            {
                file.ShouldNotDeclareNamespace(forbiddenNamespace);
            }
        }
    }

    public void ShouldNotUseNamespaces(params string[] forbiddenNamespaces)
    {
        foreach (var file in Files)
        {
            foreach (var forbiddenNamespace in forbiddenNamespaces)
            {
                file.ShouldNotUseNamespace(forbiddenNamespace);
            }
        }
    }

    public void ShouldNotUseIdentifiers(params string[] identifiers)
    {
        foreach (var file in Files)
        {
            foreach (var identifier in identifiers)
            {
                file.ShouldNotUseIdentifier(identifier);
            }
        }
    }

    public void ShouldNotContainPublicTypes(params string[] typeNames)
    {
        foreach (var file in Files)
        {
            foreach (var typeName in typeNames)
            {
                file.ShouldNotContainPublicType(typeName);
            }
        }
    }

    public void ShouldNotUseObjectType()
    {
        foreach (var file in Files)
        {
            file.ShouldNotUseObjectType();
        }
    }

    public void ShouldNotConstructTypes(params string[] typeNames)
    {
        foreach (var file in Files)
        {
            foreach (var typeName in typeNames)
            {
                file.ShouldNotConstructType(typeName);
            }
        }
    }

    public void ShouldNotInvokeMemberOn(string receiverName, params string[] memberNames)
    {
        foreach (var file in Files)
        {
            foreach (var memberName in memberNames)
            {
                file.ShouldNotInvokeMemberOn(receiverName, memberName);
            }
        }
    }

    public void ShouldInvokeMemberOn(string receiverName, string memberName)
    {
        Files.Any(file => file.InvokesMemberOn(receiverName, memberName))
            .ShouldBeTrue($"Source set should invoke {receiverName}.{memberName}.");
    }

    public void ShouldNotContainStringLiterals(params string[] values)
    {
        foreach (var file in Files)
        {
            foreach (var value in values)
            {
                file.ShouldNotContainStringLiteral(value);
            }
        }
    }
}