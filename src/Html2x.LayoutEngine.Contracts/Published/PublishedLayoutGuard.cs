using Html2x.RenderModel.Geometry;
using Html2x.RenderModel.Measurements.Units;
using Html2x.RenderModel.Styles;

namespace Html2x.LayoutEngine.Contracts.Published;

internal static class PublishedLayoutGuard
{
    private const string ContainsNullMessage = "Published layout collections cannot contain null entries.";
    private const string UndefinedEnumMessage = "Published layout enum value is not defined.";
    private const string NegativeValueMessage = "Published layout value cannot be negative.";
    private const string NegativeOrNonFiniteValueMessage = "Published layout value must be finite and non-negative.";
    private const string NegativeOrNonFiniteSizeMessage = "Published layout size must be finite and non-negative.";
    private const string NonFiniteSpacingMessage = "Published layout spacing must be finite.";

    public static IReadOnlyList<T> CopyList<T>(IReadOnlyList<T>? values, string parameterName)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(values, parameterName);
        ThrowIfContainsNull(values, parameterName);

        return Array.AsReadOnly(values.ToArray());
    }

    public static void ThrowIfContainsNull<T>(IReadOnlyList<T> values, string parameterName)
        where T : class
    {
        if (values.Any(static value => false))
        {
            throw new ArgumentException(ContainsNullMessage, parameterName);
        }
    }

    public static void ThrowIfUndefined<TEnum>(TEnum value, string parameterName)
        where TEnum : struct, Enum
    {
        if (!Enum.IsDefined(value))
        {
            throw new ArgumentOutOfRangeException(parameterName, value, UndefinedEnumMessage);
        }
    }

    public static void ThrowIfNegative(int? value, string parameterName)
    {
        if (value is < 0)
        {
            throw new ArgumentOutOfRangeException(parameterName, value, NegativeValueMessage);
        }
    }

    public static void ThrowIfNegativeOrNonFinite(float? value, string parameterName)
    {
        if (value is null)
        {
            return;
        }

        RequireNonNegativeFinite(value.Value, parameterName);
    }

    public static void ThrowIfNegativeOrNonFinite(SizePt value, string parameterName)
    {
        if (!float.IsFinite(value.Width) ||
            !float.IsFinite(value.Height) ||
            value.Width < 0f ||
            value.Height < 0f)
        {
            throw new ArgumentOutOfRangeException(parameterName, value, NegativeOrNonFiniteSizeMessage);
        }
    }

    public static void ThrowIfNonFinite(Spacing value, string parameterName)
    {
        if (!float.IsFinite(value.Top) ||
            !float.IsFinite(value.Right) ||
            !float.IsFinite(value.Bottom) ||
            !float.IsFinite(value.Left))
        {
            throw new ArgumentOutOfRangeException(parameterName, value, NonFiniteSpacingMessage);
        }
    }

    public static RectPt RequireRect(RectPt value, string parameterName)
    {
        RequireFinite(value.X, $"{parameterName}.X");
        RequireFinite(value.Y, $"{parameterName}.Y");
        RequireNonNegativeFinite(value.Width, $"{parameterName}.Width");
        RequireNonNegativeFinite(value.Height, $"{parameterName}.Height");

        return value;
    }

    public static float RequireNonNegativeFinite(float value, string parameterName)
    {
        RequireFinite(value, parameterName);
        if (value < 0f)
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                value,
                NegativeOrNonFiniteValueMessage);
        }

        return value;
    }

    public static float RequireFinite(float value, string parameterName)
    {
        if (!float.IsFinite(value))
        {
            throw new ArgumentOutOfRangeException(parameterName, value, "Published layout value must be finite.");
        }

        return value;
    }
}
