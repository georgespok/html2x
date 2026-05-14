namespace Html2x.Architecture.Test.Support;

internal static class LiteralExceptionLedger
{
    public const string LiteralField = "Literal";

    public const string CategoryField = "Category";

    public const string ReasonField = "Reason";

    public const string FutureCleanupOptionField = "Future Cleanup Option";

    public const string ReviewOutcomeField = "Review Outcome";

    public static IReadOnlyList<string> RequiredFields { get; } =
    [
        LiteralField,
        CategoryField,
        ReasonField,
        FutureCleanupOptionField,
        ReviewOutcomeField
    ];
}
