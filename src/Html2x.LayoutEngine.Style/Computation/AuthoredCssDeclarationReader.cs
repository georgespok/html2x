using AngleSharp.Css.Dom;
using AngleSharp.Dom;

namespace Html2x.LayoutEngine.Style.Computation;

internal static class AuthoredCssDeclarationReader
{
    public static AuthoredCssDeclaration? GetDeclaration(IElement element, string propertyName)
    {
        ArgumentNullException.ThrowIfNull(element);
        ArgumentException.ThrowIfNullOrWhiteSpace(propertyName);

        var rawStyle = element.GetAttribute(HtmlCssVocabulary.HtmlAttributes.Style);
        var inlineStyle = element.GetStyle();
        if (inlineStyle.Length == 0)
        {
            return FindRawDeclaration(rawStyle, propertyName);
        }

        var rawValue = inlineStyle.GetPropertyValue(propertyName);
        if (!string.IsNullOrWhiteSpace(rawValue))
        {
            return CreateDeclaration(inlineStyle, propertyName, rawValue);
        }

        return FindRawDeclaration(rawStyle, propertyName);
    }

    public static string? GetValue(IElement element, string propertyName) =>
        GetDeclaration(element, propertyName)?.RawValue;

    public static string? GetValue(ICssStyleDeclaration styles, IElement element, string propertyName)
    {
        ArgumentNullException.ThrowIfNull(styles);
        ArgumentNullException.ThrowIfNull(element);

        return GetValue(element, propertyName) ?? styles.GetPropertyValue(propertyName);
    }

    private static AuthoredCssDeclaration CreateDeclaration(
        ICssStyleDeclaration inlineStyle,
        string propertyName,
        string rawValue)
    {
        var parsedName = FindParsedPropertyName(inlineStyle, propertyName) ?? propertyName;
        var trimmedValue = rawValue.Trim();
        return new(parsedName, trimmedValue, $"{parsedName}: {trimmedValue}");
    }

    private static string? FindParsedPropertyName(ICssStyleDeclaration inlineStyle, string propertyName)
    {
        for (var i = 0; i < inlineStyle.Length; i++)
        {
            var name = inlineStyle[i];
            if (string.Equals(name, propertyName, StringComparison.OrdinalIgnoreCase))
            {
                return name;
            }
        }

        return null;
    }

    private static AuthoredCssDeclaration? FindRawDeclaration(string? style, string propertyName)
    {
        if (string.IsNullOrWhiteSpace(style))
        {
            return null;
        }

        AuthoredCssDeclaration? match = null;
        var declarationStart = 0;
        var quote = '\0';
        var parenthesisDepth = 0;

        for (var i = 0; i <= style.Length; i++)
        {
            var isEnd = i == style.Length;
            if (!isEnd)
            {
                var current = style[i];
                if (quote != '\0')
                {
                    if (current == quote)
                    {
                        quote = '\0';
                    }

                    continue;
                }

                if (current is '\'' or '"')
                {
                    quote = current;
                    continue;
                }

                if (current == '(')
                {
                    parenthesisDepth++;
                    continue;
                }

                if (current == ')' && parenthesisDepth > 0)
                {
                    parenthesisDepth--;
                    continue;
                }

                if (current != ';' || parenthesisDepth > 0)
                {
                    continue;
                }
            }

            var declaration = CreateRawDeclaration(style.AsSpan(declarationStart, i - declarationStart), propertyName);
            if (declaration is not null)
            {
                match = declaration;
            }

            declarationStart = i + 1;
        }

        return match;
    }

    private static AuthoredCssDeclaration? CreateRawDeclaration(ReadOnlySpan<char> declaration, string propertyName)
    {
        declaration = declaration.Trim();
        if (declaration.IsEmpty)
        {
            return null;
        }

        var separatorIndex = FindDeclarationSeparator(declaration);
        if (separatorIndex <= 0)
        {
            return null;
        }

        var name = declaration[..separatorIndex].Trim();
        if (!name.Equals(propertyName.AsSpan(), StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var rawValue = declaration[(separatorIndex + 1)..].Trim();
        if (rawValue.IsEmpty)
        {
            return null;
        }

        return new(name.ToString(), rawValue.ToString(), declaration.ToString());
    }

    private static int FindDeclarationSeparator(ReadOnlySpan<char> declaration)
    {
        var quote = '\0';
        var parenthesisDepth = 0;

        for (var i = 0; i < declaration.Length; i++)
        {
            var current = declaration[i];
            if (quote != '\0')
            {
                if (current == quote)
                {
                    quote = '\0';
                }

                continue;
            }

            if (current is '\'' or '"')
            {
                quote = current;
                continue;
            }

            if (current == '(')
            {
                parenthesisDepth++;
                continue;
            }

            if (current == ')' && parenthesisDepth > 0)
            {
                parenthesisDepth--;
                continue;
            }

            if (current == ':' && parenthesisDepth == 0)
            {
                return i;
            }
        }

        return -1;
    }
}
