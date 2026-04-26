param(
    [string]$InputHtml = "src/Tests/Html2x.TestConsole/html/centralize-layout-font-policy.html",
    [string]$OutputPdf = "build/centralize-layout-font-policy.pdf",
    [string]$DiagnosticsJson = "build/diagnostics/centralize-layout-font-policy.json"
)

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "../../../..")).Path
$project = Join-Path $repoRoot "src/Tests/Html2x.TestConsole/Html2x.TestConsole.csproj"
$inputPath = if ([System.IO.Path]::IsPathRooted($InputHtml)) { $InputHtml } else { Join-Path $repoRoot $InputHtml }
$outputPath = if ([System.IO.Path]::IsPathRooted($OutputPdf)) { $OutputPdf } else { Join-Path $repoRoot $OutputPdf }
$diagnosticsPath = if ([System.IO.Path]::IsPathRooted($DiagnosticsJson)) { $DiagnosticsJson } else { Join-Path $repoRoot $DiagnosticsJson }
$arguments = @(
    $inputPath,
    $outputPath,
    "--diagnostics",
    "--diagnostics-json", $diagnosticsPath
)

if (-not (Test-Path (Split-Path $diagnosticsPath -Parent))) {
    New-Item -ItemType Directory -Path (Split-Path $diagnosticsPath -Parent) -Force | Out-Null
}

& dotnet run --project $project -- $arguments
