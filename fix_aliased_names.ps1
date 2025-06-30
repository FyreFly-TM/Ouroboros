# Fix aliased name errors by reverting some global:: prefixes
param(
    [string]$RootPath = "."
)

Write-Host "Fixing aliased name errors..." -ForegroundColor Cyan

$filesToFix = @{
    "src/stdlib/math/Vector.cs" = @(
        @{ Old = "global::System.Math."; New = "System.Math." }
    )
    "src/stdlib/math/Matrix.cs" = @(
        @{ Old = "global::System.Math."; New = "System.Math." }
    )
    "src/stdlib/math/Quaternion.cs" = @(
        @{ Old = "global::System.Math."; New = "System.Math." }
    )
    "src/stdlib/math/Transform.cs" = @(
        @{ Old = "global::System.Math."; New = "System.Math." }
    )
    "src/stdlib/ui/Graphics.cs" = @(
        @{ Old = "global::System.Math."; New = "System.Math." }
    )
    "src/stdlib/ui/Layout.cs" = @(
        @{ Old = "global::System.Math."; New = "System.Math." }
    )
}

foreach ($file in $filesToFix.Keys) {
    $fullPath = Join-Path $RootPath $file
    
    if (Test-Path $fullPath) {
        Write-Host "Fixing $file..."
        $content = Get-Content $fullPath -Raw
        
        foreach ($replacement in $filesToFix[$file]) {
            $content = $content -replace [regex]::Escape($replacement.Old), $replacement.New
        }
        
        Set-Content $fullPath $content -NoNewline
    }
}

Write-Host "Aliased name errors fixed!" -ForegroundColor Green 