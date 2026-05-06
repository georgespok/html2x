namespace Html2x.TestConsole;

internal sealed record ConsoleRunContext(IReadOnlyList<string> RawArguments)
{
    public static ConsoleRunContext FromArguments(IEnumerable<string> args) => new(args.ToArray());
}