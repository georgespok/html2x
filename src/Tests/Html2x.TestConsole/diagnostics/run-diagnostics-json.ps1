param(
    [string]$InputHtml = "src/Tests/Html2x.TestConsole/html/example.html",
    [string]$OutputPdf = "build/diagnostics/example.pdf",
    [string]$DiagnosticsJson = "build/diagnostics/session.json"
)

$project = "src/Tests/Html2x.TestConsole/Html2x.TestConsole.csproj"
$arguments = @(
    $InputHtml,
    $OutputPdf,
    "--diagnostics",
    "--diagnostics-json", $DiagnosticsJson
)

if (-not (Test-Path (Split-Path $DiagnosticsJson -Parent))) {
    New-Item -ItemType Directory -Path (Split-Path $DiagnosticsJson -Parent) -Force | Out-Null
}

& dotnet run --project $project -- $arguments
