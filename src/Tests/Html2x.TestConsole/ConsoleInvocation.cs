namespace Html2x.TestConsole;

internal sealed record ConsoleInvocation(IReadOnlyList<string> RawArguments)
{
    public static ConsoleInvocation FromArguments(IEnumerable<string> args) => new(args.ToArray());
}