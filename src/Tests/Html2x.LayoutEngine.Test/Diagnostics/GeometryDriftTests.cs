using Html2x.LayoutEngine.Test.TestHelpers;
using Shouldly;

namespace Html2x.LayoutEngine.Test.Diagnostics;

public sealed class GeometryDriftTests
{
    [Theory]
    [MemberData(nameof(GetGoldenCases))]
    public async Task Build_GeometrySnapshotMatchesGoldenBaseline(
        string _,
        string html,
        string expectedSnapshot)
    {
        var result = await GeometryTestHarness.BuildAsync(html);

        GeometryInvariantValidator.AssertInvariants(result);

        GeometryTestHarness.NormalizeNewLines(GeometryTestHarness.RenderSnapshot(result.Snapshot))
            .ShouldBe(GeometryTestHarness.NormalizeNewLines(expectedSnapshot));
    }

    public static IEnumerable<object[]> GetGoldenCases()
    {
        yield return
        [
            "mixed-content",
            """
            <html>
              <body style='margin: 0;'>
                <div style='margin: 0; padding: 0;'>
                  Alpha
                  <p style='margin: 0;'>Beta</p>
                </div>
              </body>
            </html>
            """,
            """
            boxes
            1:block path=body/div rect=0,0,595,28.8 content=0,0,595,28.8 marker=0 anonymous=false inlineBlock=false
              2:block path=body/div/div rect=0,0,595,14.4 content=0,0,595,14.4 marker=0 anonymous=true inlineBlock=false
              3:block path=body/div/p rect=0,14.4,595,14.4 content=0,14.4,595,14.4 marker=0 anonymous=false inlineBlock=false
            fragments
            page 1 size=612,792 margin=0,0,0,0
              1:block rect=0,0,595,28.8
                2:line rect=0,0,10,14.4 text="Alpha"
                3:block rect=0,14.4,595,14.4
                  4:line rect=0,14.4,10,14.4 text="Beta"
            pagination
            page 1 content=0..792
              placement order=0 fragment=1 kind=Block oversized=false rect=0,0,595,28.8
            """
        ];

        yield return
        [
            "list-items",
            """
            <html>
              <body style='margin: 0;'>
                <ul style='margin: 0; padding: 0;'>
                  <li style='margin: 0;'>One</li>
                  <li style='margin: 0;'>Two</li>
                </ul>
              </body>
            </html>
            """,
            """
            boxes
            1:block path=body/ul rect=0,0,595,28.8 content=0,0,595,28.8 marker=0 anonymous=false inlineBlock=false
              2:listitem path=body/ul/li rect=0,0,595,14.4 content=0,0,595,14.4 marker=12 anonymous=false inlineBlock=false
              3:listitem path=body/ul/li rect=0,14.4,595,14.4 content=0,14.4,595,14.4 marker=12 anonymous=false inlineBlock=false
            fragments
            page 1 size=612,792 margin=0,0,0,0
              1:block rect=0,0,595,28.8
                2:block rect=0,0,595,14.4 marker=12
                  3:line rect=0,0,20,14.4 text="• One"
                4:block rect=0,14.4,595,14.4 marker=12
                  5:line rect=0,14.4,20,14.4 text="• Two"
            pagination
            page 1 content=0..792
              placement order=0 fragment=1 kind=Block oversized=false rect=0,0,595,28.8
            """
        ];

        yield return
        [
            "table",
            """
            <html>
              <body style='margin: 0;'>
                <table style='margin: 0; width: 160px;'>
                  <tr>
                    <th>A</th>
                    <th>B</th>
                  </tr>
                  <tr>
                    <td>C</td>
                    <td>D</td>
                  </tr>
                </table>
              </body>
            </html>
            """,
            """
            boxes
            1:table path=body/table rect=0,0,120,40 content=0,0,120,40 marker=0 anonymous=false inlineBlock=false columns=2
              2:tablerow path=body/table/tbody/tr rect=0,0,120,20 content=0,0,120,20 marker=0 anonymous=false inlineBlock=false row=0
                3:tablecell path=body/table/tbody/tr/th rect=0,0,60,20 content=0,0,60,20 marker=0 anonymous=false inlineBlock=false column=0 header=true
                4:tablecell path=body/table/tbody/tr/th rect=60,0,60,20 content=60,0,60,20 marker=0 anonymous=false inlineBlock=false column=1 header=true
              5:tablerow path=body/table/tbody/tr rect=0,20,120,20 content=0,20,120,20 marker=0 anonymous=false inlineBlock=false row=1
                6:tablecell path=body/table/tbody/tr/td rect=0,20,60,20 content=0,20,60,20 marker=0 anonymous=false inlineBlock=false column=0 header=false
                7:tablecell path=body/table/tbody/tr/td rect=60,20,60,20 content=60,20,60,20 marker=0 anonymous=false inlineBlock=false column=1 header=false
            fragments
            page 1 size=612,792 margin=0,0,0,0
              1:table rect=0,0,120,40 columns=2
                2:table-row rect=0,0,120,20 row=0
                  3:table-cell rect=0,0,60,20 column=0 header=true
                    4:line rect=0,0,10,14.4 text="A"
                  5:table-cell rect=60,0,60,20 column=1 header=true
                    6:line rect=60,0,10,14.4 text="B"
                7:table-row rect=0,20,120,20 row=1
                  8:table-cell rect=0,20,60,20 column=0 header=false
                    9:line rect=0,20,10,14.4 text="C"
                  10:table-cell rect=60,20,60,20 column=1 header=false
                    11:line rect=60,20,10,14.4 text="D"
            pagination
            page 1 content=0..792
              placement order=0 fragment=1 kind=Table oversized=false rect=0,0,120,40
            """
        ];

        yield return
        [
            "image",
            """
            <html>
              <body style='margin: 0;'>
                <div style='margin: 0;'>
                  <img src='image.png' width='40' height='20' style='padding: 4px; border: 2px solid black;' />
                </div>
              </body>
            </html>
            """,
            """
            boxes
            1:block path=body/div rect=0,0,595,29 content=0,0,595,29 marker=0 anonymous=false inlineBlock=false
            fragments
            page 1 size=612,792 margin=0,0,0,0
              1:block rect=0,0,595,29
                2:image rect=0,0,49,29
            pagination
            page 1 content=0..792
              placement order=0 fragment=1 kind=Block oversized=false rect=0,0,595,29
            """
        ];

        yield return
        [
            "inline-block",
            """
            <html>
              <body style='margin: 0;'>
                <div style='margin: 0;'>
                  before
                  <span style='display: inline-block; width: 40px; height: 20px; border: 1px solid black;'>X</span>
                  after
                </div>
              </body>
            </html>
            """,
            """
            boxes
            1:block path=body/div rect=0,0,595,28.8 content=0,0,595,28.8 marker=0 anonymous=false inlineBlock=false
              2:block path=body/div/div rect=0,0,595,14.4 content=0,0,595,14.4 marker=0 anonymous=true inlineBlock=false
              3:block path=body/div/div rect=0,14.4,595,14.4 content=0,14.4,595,14.4 marker=0 anonymous=true inlineBlock=false
            fragments
            page 1 size=612,792 margin=0,0,0,0
              1:block rect=0,0,595,28.8
                2:line rect=0,0,10,15.9 text="before"
                3:block rect=10,0,11.5,15.9
                  4:line rect=10.75,0.75,10,14.4 text="X"
                5:line rect=0,0,10,15.9 text=" after"
            pagination
            page 1 content=0..792
              placement order=0 fragment=1 kind=Block oversized=false rect=0,0,595,28.8
            """
        ];

        yield return
        [
            "pagination",
            """
            <html>
              <body style='margin: 0;'>
                <div style='height: 860px;'>Block 1</div>
                <div style='height: 300px;'>Block 2</div>
              </body>
            </html>
            """,
            """
            boxes
            1:block path=body/div rect=0,0,595,645 content=0,0,595,645 marker=0 anonymous=false inlineBlock=false
            2:block path=body/div rect=0,645,595,225 content=0,645,595,225 marker=0 anonymous=false inlineBlock=false
            fragments
            page 1 size=612,792 margin=0,0,0,0
              1:block rect=0,0,595,645
                2:line rect=0,0,10,14.4 text="Block 1"
            page 2 size=612,792 margin=0,0,0,0
              3:block rect=0,0,595,225
                4:line rect=0,0,10,14.4 text="Block 2"
            pagination
            page 1 content=0..792
              placement order=0 fragment=1 kind=Block oversized=false rect=0,0,595,645
            page 2 content=0..792
              placement order=1 fragment=2 kind=Block oversized=false rect=0,0,595,225
            """
        ];
    }
}
