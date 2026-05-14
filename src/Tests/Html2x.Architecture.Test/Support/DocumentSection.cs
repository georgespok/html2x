namespace Html2x.Architecture.Test.Support;

internal sealed class DocumentSection(DocumentReference document, MarkdownHeading heading)
{
    public void ShouldMentionTopics(params DocumentTopic[] topics) =>
        document.Load()
            .ShouldMentionTopicsInSection(heading, topics);
}
