#!/usr/bin/env pwsh

# Script to rename Ouroboros namespaces to Ouro
Write-Host "Renaming Ouroboros namespaces to Ouro..."

# Get all C# files
$files = Get-ChildItem -Path "src" -Filter "*.cs" -Recurse

foreach ($file in $files) {
    Write-Host "Processing: $($file.FullName)"
    
    # Read file content
    $content = Get-Content $file.FullName -Raw
    
    # Replace namespace declarations
    $content = $content -replace "namespace Ouroboros", "namespace Ouro"
    
    # Replace using statements
    $content = $content -replace "using Ouroboros", "using Ouro"
    
    # Replace any other Ouroboros references
    $content = $content -replace "Ouroboros\.", "Ouro."
    $content = $content -replace "Ouroboros::", "Ouro::"
    
    # Write back to file
    Set-Content -Path $file.FullName -Value $content -NoNewline
}

Write-Host "Namespace renaming complete!"

# Count changes made
$totalFiles = $files.Count
Write-Host "Processed $totalFiles files" 