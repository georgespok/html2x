namespace Html2x.Architecture.Test.Support;

internal sealed class DocumentReference(params string[] pathSegments)
{
    public Document Load() => Document.Load(pathSegments);

    public DocumentSection Section(string heading) =>
        new(this, new MarkdownHeading(heading));
}
