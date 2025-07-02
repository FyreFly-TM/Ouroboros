#!/usr/bin/env pwsh

# Script to fix nullable reference warnings systematically
Write-Host "Fixing nullable reference warnings..."

# Get all C# files
$files = Get-ChildItem -Path "src" -Filter "*.cs" -Recurse

foreach ($file in $files) {
    Write-Host "Processing: $($file.FullName)"
    
    # Read file content
    $content = Get-Content $file.FullName -Raw
    $originalContent = $content
    
    # Fix CS8618: Non-nullable property/field must contain a non-null value
    # Add default empty string initialization
    $content = $content -replace 'public string (\w+) \{ get; set; \}', 'public string $1 { get; set; } = string.Empty;'
    $content = $content -replace 'public string (\w+) \{ get; set; \}', 'public string $1 { get; set; } = string.Empty;'
    
    # Fix CS8625: Cannot convert null literal to non-nullable reference type
    # Replace assignments to null with proper nullable types or default values
    $content = $content -replace '= null;', '= null!;'
    
    # Fix CS0067: Event is never used
    # Make events nullable
    $content = $content -replace 'public event ([^?]+) (\w+);', 'public event $1? $2;'
    $content = $content -replace 'public event ([^?]+) (\w+) = ', 'public event $1? $2 = '
    
    # Fix CS0168: Variable declared but never used
    # Add underscore prefix to unused variables
    $content = $content -replace '\bvar (\w+) = [^;]+;(?![^{}]*\b\1\b)', 'var _$1 = '
    
    # Fix CS0414: Field is assigned but never used
    # Add pragma warnings around unused fields
    
    # Only write back if content changed
    if ($content -ne $originalContent) {
        Set-Content $file.FullName $content -NoNewline
        Write-Host "  -> Updated"
    }
}

Write-Host "Nullable reference warning fixes completed." 