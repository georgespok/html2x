using System.Text.RegularExpressions;
using Shouldly;

namespace Html2x.LayoutEngine.Test.Architecture;


internal sealed class ArchitectureDocument
{
    private readonly string _path;
    private readonly string _content;

    private ArchitectureDocument(string path)
    {
        _path = path;
        _content = File.ReadAllText(path);
    }

    public static ArchitectureDocument Load(params string[] pathSegments) =>
        new(ArchitecturePaths.PathFromRoot(pathSegments));

    public void ShouldMention(params string[] topics)
    {
        foreach (var topic in topics)
        {
            _content.Contains(topic, StringComparison.Ordinal)
                .ShouldBeTrue($"{_path} should mention {topic}.");
        }
    }

    public void ShouldNotMention(params string[] topics)
    {
        foreach (var topic in topics)
        {
            _content.Contains(topic, StringComparison.Ordinal)
                .ShouldBeFalse($"{_path} should not mention {topic}.");
        }
    }

    public void ShouldMentionTopicsInSection(string heading, params string[] topics)
    {
        var sectionContent = SectionContent(heading);
        foreach (var topic in topics)
        {
            sectionContent.Contains(topic, StringComparison.Ordinal)
                .ShouldBeTrue($"{_path} section '{heading}' should mention {topic}.");
        }
    }

    private string SectionContent(string heading)
    {
        var lines = _content.Split(["\r\n", "\n"], StringSplitOptions.None);
        var headingIndex = -1;
        var headingLevel = 0;

        for (var i = 0; i < lines.Length; i++)
        {
            if (TryReadHeading(lines[i], out var level, out var title) &&
                title.Equals(heading, StringComparison.Ordinal))
            {
                headingIndex = i;
                headingLevel = level;
                break;
            }
        }

        headingIndex.ShouldNotBe(-1, $"{_path} should contain heading {heading}.");

        var endIndex = lines.Length;
        for (var i = headingIndex + 1; i < lines.Length; i++)
        {
            if (TryReadHeading(lines[i], out var level, out _) && level <= headingLevel)
            {
                endIndex = i;
                break;
            }
        }

        return string.Join("\n", lines[(headingIndex + 1)..endIndex]);
    }

    private static bool TryReadHeading(string line, out int level, out string title)
    {
        var match = Regex.Match(line, "^(?<level>#{1,6})\\s+(?<title>.+?)\\s*$");
        if (!match.Success)
        {
            level = 0;
            title = string.Empty;
            return false;
        }

        level = match.Groups["level"].Value.Length;
        title = match.Groups["title"].Value;
        return true;
    }
}
