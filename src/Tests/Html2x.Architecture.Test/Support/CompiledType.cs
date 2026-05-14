using System.Reflection;
using Shouldly;

namespace Html2x.Architecture.Test.Support;

internal sealed class CompiledType
{
    private const BindingFlags MemberBindings =
        BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

    private const string PrivateAccessibility = "private";
    private const string ProtectedAccessibility = "protected";
    private const string ProtectedInternalAccessibility = "protected internal";
    private const string PrivateProtectedAccessibility = "private protected";

    private readonly Type _type;

    private CompiledType(Type type)
    {
        _type = type;
    }

    public static CompiledType For<T>() => new(typeof(T));

    public static CompiledType For(Type type) => new(type);

    public void ShouldHaveAccessibility(string expectedAccessibility) =>
        AccessibilityOf(_type).ShouldBe(expectedAccessibility, $"{_type.FullName} accessibility mismatch.");

    public void ShouldContainConstructor(string accessibility, params Type[] parameterTypes)
    {
        var constructor = _type.GetConstructors(MemberBindings)
            .FirstOrDefault(constructor => ParameterTypes(constructor).SequenceEqual(parameterTypes));

        constructor.ShouldNotBeNull($"{_type.FullName} should contain matching constructor.");
        AccessibilityOf(constructor).ShouldBe(accessibility, $"{_type.FullName} constructor accessibility mismatch.");
    }

    public void ShouldContainMethod(string methodName, Type returnType, string? accessibility = null)
    {
        var method = _type.GetMethods(MemberBindings)
            .FirstOrDefault(method => method.Name.Equals(methodName, StringComparison.Ordinal));

        method.ShouldNotBeNull($"{_type.FullName} should contain method {methodName}.");
        method.ReturnType.ShouldBe(returnType, $"{_type.FullName}.{methodName} return type mismatch.");
        if (accessibility is not null)
        {
            AccessibilityOf(method).ShouldBe(accessibility, $"{_type.FullName}.{methodName} accessibility mismatch.");
        }
    }

    public void ShouldContainProperty(string propertyName, Type propertyType, string? accessibility = null)
    {
        var property = _type.GetProperties(MemberBindings)
            .FirstOrDefault(property => property.Name.Equals(propertyName, StringComparison.Ordinal));

        property.ShouldNotBeNull($"{_type.FullName} should contain property {propertyName}.");
        property.PropertyType.ShouldBe(propertyType, $"{_type.FullName}.{propertyName} type mismatch.");
        if (accessibility is not null)
        {
            var accessor = property.GetMethod ?? property.SetMethod;
            accessor.ShouldNotBeNull($"{_type.FullName}.{propertyName} should expose an accessor.");
            AccessibilityOf(accessor).ShouldBe(
                accessibility,
                $"{_type.FullName}.{propertyName} accessibility mismatch.");
        }
    }

    private static IReadOnlyList<Type> ParameterTypes(MethodBase method) =>
        method.GetParameters()
            .Select(static parameter => parameter.ParameterType)
            .ToArray();

    private static string AccessibilityOf(Type type)
    {
        if (type.IsPublic || type.IsNestedPublic)
        {
            return TestSupport.PublicAccessibility;
        }

        if (type.IsNotPublic || type.IsNestedAssembly)
        {
            return TestSupport.InternalAccessibility;
        }

        if (type.IsNestedPrivate)
        {
            return PrivateAccessibility;
        }

        if (type.IsNestedFamily)
        {
            return ProtectedAccessibility;
        }

        return type.IsNestedFamORAssem ? ProtectedInternalAccessibility : PrivateProtectedAccessibility;
    }

    private static string AccessibilityOf(MethodBase method)
    {
        if (method.IsPublic)
        {
            return TestSupport.PublicAccessibility;
        }

        if (method.IsAssembly)
        {
            return TestSupport.InternalAccessibility;
        }

        if (method.IsPrivate)
        {
            return PrivateAccessibility;
        }

        if (method.IsFamily)
        {
            return ProtectedAccessibility;
        }

        return method.IsFamilyOrAssembly ? ProtectedInternalAccessibility : PrivateProtectedAccessibility;
    }
}
