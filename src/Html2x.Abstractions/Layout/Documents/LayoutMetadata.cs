namespace Html2x.Abstractions.Layout.Documents;

public sealed record LayoutMetadata(
    string? Title = null,
    string? Author = null,
    string? Subject = null,
    string? Language = null, // e.g., "en-CA"
    DateTime? CreatedUtc = null,
    string? BaseUrl = null // for resolving relative URLs (images, CSS)
);