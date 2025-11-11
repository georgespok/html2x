using System.Reflection;
using AngleSharp.Dom;
using Shouldly;

namespace Html2x.LayoutEngine.Test.Assertions;

public static class DeepObjectAssertions
{
    public static void ShouldMatchProperties<T>(this T actual, T expected, string? context = null) where T : class
    {
        if (ReferenceEquals(actual, null) && ReferenceEquals(expected, null)) return;
        if (ReferenceEquals(actual, null)) throw new Xunit.Sdk.XunitException($"{context ?? typeof(T).Name}: actual is null but expected is not");
        if (ReferenceEquals(expected, null)) return;

        var type = typeof(T);
        foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            var expectedValue = prop.GetValue(expected);
            var actualValue = prop.GetValue(actual);

            if (IsDefaultOrNull(expectedValue))
            {
                continue;
            }

            var propertyContext = context is not null ? $"{context}.{prop.Name}" : prop.Name;

            AssertPropertyValue(actualValue, expectedValue, propertyContext, prop);
        }
    }

    private static void AssertPropertyValue(object? actualValue, object? expectedValue, string context, PropertyInfo prop)
    {
        if (expectedValue is null)
        {
            actualValue.ShouldBeNull($"Property '{context}' should be null");
            return;
        }

        if (actualValue is null)
        {
            throw new Xunit.Sdk.XunitException($"Property '{context}' is null but expected {expectedValue}");
        }

        var propertyType = prop.PropertyType;

        if (propertyType == typeof(IElement))
        {
            AssertElement(actualValue, expectedValue, context);
        }
        else if (propertyType.IsGenericType && propertyType.GetGenericTypeDefinition() == typeof(List<>))
        {
            AssertList(actualValue, expectedValue, context, propertyType);
        }
        else if (propertyType.IsClass && propertyType != typeof(string))
        {
            AssertNestedObject(actualValue, expectedValue, context, propertyType);
        }
        else
        {
            try
            {
                actualValue.ShouldBe(expectedValue, $"Property '{context}' mismatch");
            }
            catch (ShouldAssertException)
            {
                throw new Xunit.Sdk.XunitException(
                    $"Property '{context}' mismatch: expected {expectedValue}, actual {actualValue}");
            }
        }
    }

    private static void AssertElement(object actualValue, object expectedValue, string context)
    {
        if (actualValue is not IElement actualElement || expectedValue is not IElement expectedElement)
        {
            throw new Xunit.Sdk.XunitException($"Property '{context}' is not an IElement");
        }

        // Compare by reference equality - if the expected Element is the same instance, they should match
        // This allows tests to use the actual parsed DOM elements
        if (ReferenceEquals(actualElement, expectedElement))
        {
            return;
        }

        // Fallback to tag name comparison if different instances
        actualElement.TagName.ShouldBe(expectedElement.TagName,
            $"Property '{context}' element tag mismatch");
    }

    private static void AssertList(object actualValue, object expectedValue, string context, Type listType)
    {
        if (actualValue is not System.Collections.IList actualList ||
            expectedValue is not System.Collections.IList expectedList)
        {
            throw new Xunit.Sdk.XunitException($"Property '{context}' is not a list");
        }

        actualList.Count.ShouldBe(expectedList.Count,
            $"Property '{context}' count mismatch: expected {expectedList.Count}, actual {actualList.Count}");

        var itemType = listType.GetGenericArguments()[0];
        for (int i = 0; i < expectedList.Count; i++)
        {
            var actualItem = actualList[i];
            var expectedItem = expectedList[i];

            if (actualItem is null || expectedItem is null)
            {
                actualItem.ShouldBe(expectedItem, $"Property '{context}[{i}]' null mismatch");
                continue;
            }

            if (itemType.IsClass && itemType != typeof(string))
            {
                var shouldMatchMethod = typeof(DeepObjectAssertions)
                    .GetMethod(nameof(ShouldMatchProperties), BindingFlags.Public | BindingFlags.Static)
                    ?.MakeGenericMethod(itemType);

                if (shouldMatchMethod is not null)
                {
                    shouldMatchMethod.Invoke(null, [actualItem, expectedItem, $"{context}[{i}]"]);
                }
                else
                {
                    throw new NotSupportedException($"Cannot compare items of type {itemType.Name} in '{context}'");
                }
            }
            else
            {
                actualItem.ShouldBe(expectedItem, $"Property '{context}[{i}]' mismatch");
            }
        }
    }

    private static void AssertNestedObject(object actualValue, object expectedValue, string context, Type objectType)
    {
        var shouldMatchMethod = typeof(DeepObjectAssertions)
            .GetMethod(nameof(ShouldMatchProperties), BindingFlags.Public | BindingFlags.Static)
            ?.MakeGenericMethod(objectType);

        if (shouldMatchMethod is not null)
        {
            shouldMatchMethod.Invoke(null, [actualValue, expectedValue, context]);
        }
        else
        {
            throw new NotSupportedException($"Cannot compare nested object of type {objectType.Name} in '{context}'");
        }
    }

    private static bool IsDefaultOrNull(object? value)
    {
        if (value is null)
            return true;

        var type = value.GetType();

        if (type == typeof(string))
            return string.IsNullOrEmpty((string)value);

        // IElement is never considered default (always compare if set in expected)
        if (type == typeof(IElement))
            return false;

        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
            return ((System.Collections.ICollection)value).Count == 0;

        if (type.IsClass)
            return false;

        if (type.IsValueType)
        {
            var defaultValue = Activator.CreateInstance(type);
            return value.Equals(defaultValue);
        }

        return false;
    }
}

