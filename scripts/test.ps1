param(
    [Parameter(Position=0)]
    [string]$Target = "all"
)

$ErrorActionPreference = "Stop"

switch ($Target.ToLower()) {
    "all" {
        dotnet test src/Html2x.sln -c Release
    }
    "layout" {
        dotnet test src/Html2x.Layout.Test/Html2x.Layout.Test.csproj -c Release
    }
    "pdf" {
        dotnet test src/Html2x.Pdf.Test/Html2x.Pdf.Test.csproj -c Release
    }
    "integration" {
        dotnet test src/Html2x.Test/Html2x.Test.csproj -c Release
    }
    default {
        Write-Host "Unknown target: $Target" -ForegroundColor Red
        Write-Host "Usage: ./test.ps1 [all|layout|pdf|integration]"
        exit 1
    }
}

