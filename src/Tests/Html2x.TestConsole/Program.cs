namespace Html2x.TestConsole;

internal static class Program
{
    public static async Task<int> Main(string[] args)
    {
        if (!OptionsParser.TryParse(args, out var options, out var error))
        {
            OptionsParser.ShowUsage(error);
            return 1;
        }

        var service = new HtmlConversionService(options);
        return await service.ExecuteAsync();
    }
}
