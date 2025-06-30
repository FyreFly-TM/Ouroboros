# Ouroboros Build Script
# PowerShell script to build the Ouroboros programming language

param(
    [Parameter(Position=0)]
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Release',
    
    [Parameter()]
    [switch]$Clean,
    
    [Parameter()]
    [switch]$Test,
    
    [Parameter()]
    [switch]$Package,
    
    [Parameter()]
    [string]$OutputDir = ".\bin"
)

$ErrorActionPreference = "Stop"

# Define paths
$RootDir = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$SrcDir = Join-Path $RootDir "src"
$TestDir = Join-Path $RootDir "tests"
$BinDir = Join-Path $RootDir $OutputDir
$ObjDir = Join-Path $RootDir "obj"

# Colors for output
function Write-Header {
    param([string]$Message)
    Write-Host "`n=== $Message ===" -ForegroundColor Cyan
}

function Write-Success {
    param([string]$Message)
    Write-Host "[+] $Message" -ForegroundColor Green
}

function Write-Error {
    param([string]$Message)
    Write-Host "[-] $Message" -ForegroundColor Red
}

function Write-Info {
    param([string]$Message)
    Write-Host "  $Message" -ForegroundColor Gray
}

# Clean build artifacts
function Clean-Build {
    Write-Header "Cleaning build artifacts"
    
    if (Test-Path $BinDir) {
        Remove-Item -Path $BinDir -Recurse -Force
        Write-Success "Removed bin directory"
    }
    
    if (Test-Path $ObjDir) {
        Remove-Item -Path $ObjDir -Recurse -Force
        Write-Success "Removed obj directory"
    }
    
    # Clean any temporary files
    Get-ChildItem -Path $RootDir -Include "*.tmp", "*.log" -Recurse | Remove-Item -Force
    Write-Success "Cleaned temporary files"
}

# Ensure required directories exist
function Ensure-Directories {
    $dirs = @($BinDir, $ObjDir, (Join-Path $BinDir $Configuration))
    
    foreach ($dir in $dirs) {
        if (!(Test-Path $dir)) {
            New-Item -ItemType Directory -Path $dir -Force | Out-Null
        }
    }
}

# Find C# compiler
function Find-CSharpCompiler {
    # Try to find MSBuild first
    $msbuild = Get-Command msbuild -ErrorAction SilentlyContinue
    if ($msbuild) {
        return @{
            Type = "MSBuild"
            Path = $msbuild.Path
        }
    }
    
    # Try to find dotnet CLI
    $dotnet = Get-Command dotnet -ErrorAction SilentlyContinue
    if ($dotnet) {
        return @{
            Type = "DotNet"
            Path = $dotnet.Path
        }
    }
    
    # Try to find csc.exe in common locations
    $cscPaths = @(
        "C:\Program Files\Microsoft Visual Studio\2022\*\MSBuild\Current\Bin\Roslyn\csc.exe",
        "C:\Program Files (x86)\Microsoft Visual Studio\2019\*\MSBuild\Current\Bin\Roslyn\csc.exe",
        "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
    )
    
    foreach ($pattern in $cscPaths) {
        $found = Get-ChildItem -Path $pattern -ErrorAction SilentlyContinue | Select-Object -First 1
        if ($found) {
            return @{
                Type = "CSC"
                Path = $found.FullName
            }
        }
    }
    
    throw "No C# compiler found. Please install Visual Studio or .NET SDK."
}

# Compile source files
function Build-Ouroboros {
    Write-Header "Building Ouroboros"
    
    $compiler = Find-CSharpCompiler
    Write-Info "Found compiler: $($compiler.Type) at $($compiler.Path)"
    
    # Collect all C# source files
    $sourceFiles = Get-ChildItem -Path $SrcDir -Filter "*.cs" -Recurse
    Write-Info "Found $($sourceFiles.Count) source files"
    
    $outputPath = Join-Path $BinDir $Configuration
    $outputExe = Join-Path $outputPath "ouroboros.exe"
    
    # Build based on compiler type
    switch ($compiler.Type) {
        "DotNet" {
            # Create a temporary project file
            $projFile = Join-Path $ObjDir "Ouroboros.csproj"
            $projContent = @"
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net6.0</TargetFramework>
    <AssemblyName>ouroboros</AssemblyName>
    <RootNamespace>Ouroboros</RootNamespace>
    <Configuration>$Configuration</Configuration>
  </PropertyGroup>
</Project>
"@
            $projContent | Out-File -FilePath $projFile -Encoding UTF8
            
            # Copy source files to obj directory
            foreach ($file in $sourceFiles) {
                $relativePath = $file.FullName.Substring($SrcDir.Length + 1)
                $destPath = Join-Path $ObjDir $relativePath
                $destDir = Split-Path $destPath -Parent
                
                if (!(Test-Path $destDir)) {
                    New-Item -ItemType Directory -Path $destDir -Force | Out-Null
                }
                
                Copy-Item -Path $file.FullName -Destination $destPath -Force
            }
            
            # Build with dotnet
            & $compiler.Path build $projFile -c $Configuration -o $outputPath
            
            if ($LASTEXITCODE -eq 0) {
                Write-Success "Build completed successfully"
            } else {
                throw "Build failed with exit code $LASTEXITCODE"
            }
        }
        
        "CSC" {
            # Build with csc.exe directly
            $references = @(
                "System.dll",
                "System.Core.dll",
                "System.Linq.dll",
                "System.Collections.dll"
            )
            
            $args = @(
                "/out:$outputExe",
                "/target:exe",
                "/optimize$(if ($Configuration -eq 'Release') {'+' } else {'-'})",
                "/debug$(if ($Configuration -eq 'Debug') {'+' } else {'-'})"
            )
            
            foreach ($ref in $references) {
                $args += "/reference:$ref"
            }
            
            foreach ($file in $sourceFiles) {
                $args += $file.FullName
            }
            
            & $compiler.Path $args
            
            if ($LASTEXITCODE -eq 0) {
                Write-Success "Build completed successfully"
            } else {
                throw "Build failed with exit code $LASTEXITCODE"
            }
        }
        
        default {
            throw "Unsupported compiler type: $($compiler.Type)"
        }
    }
    
    return $outputExe
}

# Run tests
function Run-Tests {
    param([string]$CompilerPath)
    
    Write-Header "Running tests"
    
    $testFiles = Get-ChildItem -Path $TestDir -Filter "*.ouro" -Recurse
    Write-Info "Found $($testFiles.Count) test files"
    
    $passed = 0
    $failed = 0
    
    foreach ($testFile in $testFiles) {
        Write-Host -NoNewline "  Testing $($testFile.Name)... "
        
        try {
            # Run the test file with the compiler
            $output = & $CompilerPath $testFile.FullName 2>&1
            
            if ($LASTEXITCODE -eq 0) {
                Write-Host "PASSED" -ForegroundColor Green
                $passed++
            } else {
                Write-Host "FAILED" -ForegroundColor Red
                Write-Info "    Output: $output"
                $failed++
            }
        } catch {
            Write-Host "ERROR" -ForegroundColor Red
            Write-Info "    Error: $_"
            $failed++
        }
    }
    
    Write-Info ""
    Write-Success "Tests completed: $passed passed, $failed failed"
    
    if ($failed -gt 0) {
        throw "Some tests failed"
    }
}

# Package the build
function Package-Build {
    param([string]$CompilerPath)
    
    Write-Header "Packaging Ouroboros"
    
    $packageDir = Join-Path $BinDir "package"
    $version = "1.0.0"  # Could read this from a version file
    $packageName = "ouroboros-$version-win-x64"
    $packagePath = Join-Path $packageDir $packageName
    
    # Create package directory
    if (Test-Path $packagePath) {
        Remove-Item -Path $packagePath -Recurse -Force
    }
    New-Item -ItemType Directory -Path $packagePath -Force | Out-Null
    
    # Copy compiler
    Copy-Item -Path $CompilerPath -Destination $packagePath
    
    # Copy standard library
    $stdlibSrc = Join-Path $SrcDir "stdlib"
    $stdlibDest = Join-Path $packagePath "stdlib"
    Copy-Item -Path $stdlibSrc -Destination $stdlibDest -Recurse
    
    # Copy examples
    $examplesSrc = Join-Path $RootDir "examples"
    $examplesDest = Join-Path $packagePath "examples"
    Copy-Item -Path $examplesSrc -Destination $examplesDest -Recurse
    
    # Copy documentation
    $docsSrc = Join-Path $RootDir "docs"
    $docsDest = Join-Path $packagePath "docs"
    Copy-Item -Path $docsSrc -Destination $docsDest -Recurse
    
    # Create README
    $readme = @"
Ouroboros Programming Language v$version
======================================

Installation:
1. Add the bin directory to your PATH
2. Run 'ouroboros' from the command line

Usage:
  ouroboros <source-file>      Compile and run a source file
  ouroboros -c <source-file>   Compile only
  ouroboros -h                 Show help

Examples are provided in the examples/ directory.
Documentation is available in the docs/ directory.

For more information, visit: https://github.com/yourusername/ouroboros
"@
    
    $readme | Out-File -FilePath (Join-Path $packagePath "README.txt") -Encoding UTF8
    
    # Create batch file for easy execution
    $batch = @"
@echo off
"%~dp0\ouroboros.exe" %*
"@
    
    $batch | Out-File -FilePath (Join-Path $packagePath "ouro.bat") -Encoding ASCII
    
    # Create ZIP archive
    $zipPath = Join-Path $packageDir "$packageName.zip"
    Compress-Archive -Path $packagePath -DestinationPath $zipPath -Force
    
    Write-Success "Package created: $zipPath"
    
    # Calculate package size
    $size = (Get-Item $zipPath).Length / 1MB
    Write-Info "Package size: $([math]::Round($size, 2)) MB"
}

# Main build process
try {
    Write-Host "Ouroboros Build System" -ForegroundColor Magenta
    Write-Host "Configuration: $Configuration" -ForegroundColor Yellow
    
    # Change to root directory
    Set-Location $RootDir
    
    # Clean if requested
    if ($Clean) {
        Clean-Build
    }
    
    # Ensure directories exist
    Ensure-Directories
    
    # Build the project
    $compilerPath = Build-Ouroboros
    
    # Run tests if requested
    if ($Test) {
        Run-Tests -CompilerPath $compilerPath
    }
    
    # Package if requested
    if ($Package) {
        Package-Build -CompilerPath $compilerPath
    }
    
    Write-Header "Build completed successfully!"
    Write-Info "Output: $compilerPath"
    
    exit 0
} catch {
    Write-Error "Build failed: $_"
    exit 1
} 