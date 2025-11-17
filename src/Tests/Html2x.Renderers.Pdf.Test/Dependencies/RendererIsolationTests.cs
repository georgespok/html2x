using System.Reflection;
using Html2x.Renderers.Pdf.Mapping;
using Html2x.Renderers.Pdf.Rendering;
using Shouldly;

namespace Html2x.Renderers.Pdf.Test.Dependencies;

public sealed class RendererIsolationTests
{
    private static readonly string[] ForbiddenNamespaces =
    [
        "Html2x.LayoutEngine",
        "Html2x.LayoutEngine.Style",
        "Html2x.LayoutEngine.Models"
    ];

    [Fact]
    public void RendererAssembly_ShouldNotReferenceLayoutEngine()
    {
        var assembly = typeof(QuestPdfFragmentRenderer).Assembly;
        var referenced = assembly
            .GetReferencedAssemblies()
            .Select(a => a.Name)
            .ToArray();

        referenced.ShouldContain("Html2x.Abstractions");
        referenced.ShouldNotContain("Html2x.LayoutEngine");
    }

    [Fact]
    public void RendererTypes_ShouldNotExposeLayoutEngineTypes()
    {
        var assembly = typeof(QuestPdfFragmentRenderer).Assembly;
        var violations = new List<string>();

        foreach (var type in assembly.GetTypes()
                     .Where(t => t.Namespace?.StartsWith("Html2x.Renderers.Pdf", StringComparison.Ordinal) == true))
        {
            InspectTypeMembers(type, violations);
        }

        violations.ShouldBeEmpty();
    }

    private static void InspectTypeMembers(Type type, ICollection<string> violations)
    {
        var bindingFlags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public |
                           BindingFlags.NonPublic | BindingFlags.DeclaredOnly;

        foreach (var field in type.GetFields(bindingFlags))
        {
            CheckType(field.FieldType, $"{type.FullName}.{field.Name}", violations);
        }

        foreach (var property in type.GetProperties(bindingFlags))
        {
            CheckType(property.PropertyType, $"{type.FullName}.{property.Name}", violations);
        }

        foreach (var method in type.GetMethods(bindingFlags))
        {
            CheckType(method.ReturnType, $"{type.FullName}.{method.Name} return", violations);
            foreach (var parameter in method.GetParameters())
            {
                CheckType(parameter.ParameterType,
                    $"{type.FullName}.{method.Name}({parameter.Name})", violations);
            }
        }

        foreach (var nested in type.GetNestedTypes(bindingFlags))
        {
            InspectTypeMembers(nested, violations);
        }
    }

    private static void CheckType(Type type, string context, ICollection<string> violations)
    {
        if (type == typeof(void))
        {
            return;
        }

        if (TypeUsesForbiddenNamespace(type))
        {
            violations.Add($"{context} -> {type.FullName}");
        }
    }

    private static bool TypeUsesForbiddenNamespace(Type type)
    {
        if (type.Namespace is not null &&
            ForbiddenNamespaces.Any(ns => type.FullName?.StartsWith(ns, StringComparison.Ordinal) == true))
        {
            return true;
        }

        if (type.HasElementType && type.GetElementType() is { } elementType &&
            TypeUsesForbiddenNamespace(elementType))
        {
            return true;
        }

        if (type.IsGenericType)
        {
            foreach (var argument in type.GetGenericArguments())
            {
                if (TypeUsesForbiddenNamespace(argument))
                {
                    return true;
                }
            }
        }

        return false;
    }
}
