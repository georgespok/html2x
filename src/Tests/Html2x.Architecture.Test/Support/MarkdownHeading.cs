namespace Html2x.Architecture.Test.Support;

internal readonly record struct MarkdownHeading(string Value)
{
    public override string ToString() => Value;
}
