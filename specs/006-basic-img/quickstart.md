# Quickstart

1) Restore and build
- dotnet restore Html2x.sln
- dotnet build Html2x.sln -c Release

2) Run tests
- dotnet test Html2x.sln -c Release

3) Manual render smoke test
- dotnet run --project src/Tests/Html2x.TestConsole/Html2x.TestConsole.csproj -- --input src/Tests/Html2x.TestConsole/html/example.html --output build/example.pdf

4) Add sample for this feature
- Place an HTML file in src/Tests/Html2x.TestConsole/html/ that includes <img> tags with width and height variations and an invalid src to exercise placeholders and downscaling.

5) Diagnostics
- Ensure warnings appear for missing images and downscaling; collect Html2x.Diagnostics payloads during tests.
