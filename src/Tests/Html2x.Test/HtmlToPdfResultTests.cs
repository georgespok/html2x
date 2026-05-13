using Shouldly;

namespace Html2x.Test;

public sealed class HtmlToPdfResultTests
{
    [Fact]
    public void PdfBytes_ReturnsDefensiveCopies()
    {
        var source = new byte[] { 1, 2, 3 };
        var result = new HtmlToPdfResult(source);

        source[0] = 9;
        var firstRead = result.PdfBytes;
        firstRead.ShouldBe([1, 2, 3]);

        firstRead[1] = 8;
        var secondRead = result.PdfBytes;

        secondRead.ShouldBe([1, 2, 3]);
        firstRead.ShouldNotBeSameAs(secondRead);
    }
}