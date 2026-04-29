using Html2x.Abstractions.Layout.Styles;
using Html2x.Abstractions.Measurements.Units;

namespace Html2x.Abstractions.Utilities.Guards;

internal static class ArgumentGuard
{
    public static void ThrowIfContainsNull<T>(
        IReadOnlyList<T> values,
        string? parameterName = null,
        string? message = null)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(values, parameterName);

        if (values.Any(static value => value is null))
        {
            throw new ArgumentException(message, parameterName);
        }
    }

    public static void ThrowIfUndefined<TEnum>(
        TEnum value,
        string? parameterName = null,
        string? message = null)
        where TEnum : struct, Enum
    {
        if (!Enum.IsDefined(value))
        {
            throw new ArgumentOutOfRangeException(parameterName, value, message);
        }
    }

    public static void ThrowIfNegative(
        int? value,
        string? parameterName = null,
        string? message = null)
    {
        if (value is < 0)
        {
            throw new ArgumentOutOfRangeException(parameterName, value, message);
        }
    }

    public static void ThrowIfNegativeOrNonFinite(
        float? value,
        string? parameterName = null,
        string? message = null)
    {
        if (value is null)
        {
            return;
        }

        if (!float.IsFinite(value.Value) || value.Value < 0f)
        {
            throw new ArgumentOutOfRangeException(parameterName, value, message);
        }
    }

    public static void ThrowIfNegativeOrNonFinite(
        SizePt value,
        string? parameterName = null,
        string? message = null)
    {
        if (!float.IsFinite(value.Width) ||
            !float.IsFinite(value.Height) ||
            value.Width < 0f ||
            value.Height < 0f)
        {
            throw new ArgumentOutOfRangeException(parameterName, value, message);
        }
    }

    public static void ThrowIfNonFinite(
        Spacing value,
        string? parameterName = null,
        string? message = null)
    {
        if (!float.IsFinite(value.Top) ||
            !float.IsFinite(value.Right) ||
            !float.IsFinite(value.Bottom) ||
            !float.IsFinite(value.Left))
        {
            throw new ArgumentOutOfRangeException(parameterName, value, message);
        }
    }
}
