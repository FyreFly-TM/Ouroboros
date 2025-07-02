using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Ouro.Tools.Opm
{
    /// <summary>
    /// Package publishing tools
    /// </summary>
    public class PackagePublisher
    {
        private readonly PackageRegistry registry;
        private readonly PackageValidator validator;
        private readonly string workingDirectory;
        
        public PackagePublisher(PackageRegistry registry, string workingDirectory)
        {
            this.registry = registry;
            this.validator = new PackageValidator();
            this.workingDirectory = workingDirectory;
        }
        
        /// <summary>
        /// Prepare package for publishing
        /// </summary>
        public async Task<PublishResult> PreparePackageAsync(PublishOptions options)
        {
            Console.WriteLine("Preparing package for publication...");
            
            // Load and validate metadata
            var metadata = await LoadPackageMetadataAsync();
            var validationResult = await validator.ValidateAsync(metadata, workingDirectory);
            
            if (!validationResult.IsValid)
            {
                return new PublishResult
                {
                    Success = false,
                    Errors = validationResult.Errors
                };
            }
            
            // Check version conflicts
            if (!options.Force)
            {
                var existing = await registry.GetPackageAsync(metadata.Name, metadata.Version);
                if (existing != null)
                {
                    return new PublishResult
                    {
                        Success = false,
                        Errors = new[] { $"Version {metadata.Version} already exists. Use --force to overwrite." }
                    };
                }
            }
            
            // Build package
            var packagePath = await BuildPackageAsync(metadata, options);
            
            // Sign package if configured
            if (options.Sign && !string.IsNullOrEmpty(options.SigningKey))
            {
                await SignPackageAsync(packagePath, options.SigningKey);
            }
            
            return new PublishResult
            {
                Success = true,
                PackagePath = packagePath,
                Metadata = metadata
            };
        }
        
        /// <summary>
        /// Publish package to registry
        /// </summary>
        public async Task<bool> PublishAsync(PublishResult prepareResult, PublishOptions options)
        {
            if (!prepareResult.Success || prepareResult.PackagePath == null)
            {
                throw new InvalidOperationException("Package preparation failed");
            }
            
            Console.WriteLine($"Publishing {prepareResult.Metadata.Name}@{prepareResult.Metadata.Version}...");
            
            // Calculate checksum
            var checksum = await CalculateChecksumAsync(prepareResult.PackagePath);
            
            // Add publication metadata
            prepareResult.Metadata.PublishedAt = DateTime.UtcNow.ToString("O");
            prepareResult.Metadata.Checksum = checksum;
            
            // Tag the release if specified
            if (!string.IsNullOrEmpty(options.Tag))
            {
                prepareResult.Metadata.Tags ??= new List<string>();
                prepareResult.Metadata.Tags.Add(options.Tag);
            }
            
            // Publish to registry
            var success = await registry.PublishAsync(
                prepareResult.PackagePath, 
                prepareResult.Metadata, 
                checksum
            );
            
            if (success)
            {
                Console.WriteLine($"✅ Successfully published {prepareResult.Metadata.Name}@{prepareResult.Metadata.Version}");
                
                // Clean up temp files unless keep flag is set
                if (!options.KeepPackage && File.Exists(prepareResult.PackagePath))
                {
                    File.Delete(prepareResult.PackagePath);
                }
            }
            else
            {
                Console.WriteLine("❌ Failed to publish package");
            }
            
            return success;
        }
        
        private async Task<PackageMetadata> LoadPackageMetadataAsync()
        {
            var metadataPath = Path.Combine(workingDirectory, "package.ouro.json");
            if (!File.Exists(metadataPath))
            {
                throw new FileNotFoundException("package.ouro.json not found");
            }
            
            var json = await File.ReadAllTextAsync(metadataPath);
            return JsonSerializer.Deserialize<PackageMetadata>(json, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            }) ?? throw new InvalidOperationException("Invalid package metadata");
        }
        
        private async Task<string> BuildPackageAsync(PackageMetadata metadata, PublishOptions options)
        {
            var outputDir = Path.Combine(workingDirectory, ".ouro", "publish");
            Directory.CreateDirectory(outputDir);
            
            var packageName = $"{metadata.Name}-{metadata.Version}.ouro.zip";
            var packagePath = Path.Combine(outputDir, packageName);
            
            // Delete existing package
            if (File.Exists(packagePath))
            {
                File.Delete(packagePath);
            }
            
            // Get files to include
            var files = await GetPackageFilesAsync(options);
            
            // Create package archive
            using (var archive = ZipFile.Open(packagePath, ZipArchiveMode.Create))
            {
                foreach (var file in files)
                {
                    var relativePath = Path.GetRelativePath(workingDirectory, file);
                    var entry = archive.CreateEntryFromFile(file, relativePath);
                    
                    // Set appropriate permissions
                    if (IsExecutable(file))
                    {
                        entry.ExternalAttributes = unchecked((int)0x81ED0000); // Unix executable permissions
                    }
                }
                
                // Add metadata
                var metadataEntry = archive.CreateEntry("package.ouro.json");
                using (var stream = metadataEntry.Open())
                using (var writer = new StreamWriter(stream))
                {
                    var json = JsonSerializer.Serialize(metadata, new JsonSerializerOptions
                    {
                        WriteIndented = true,
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    });
                    await writer.WriteAsync(json);
                }
            }
            
            Console.WriteLine($"📦 Package created: {packagePath} ({new FileInfo(packagePath).Length / 1024}KB)");
            return packagePath;
        }
        
        private async Task<List<string>> GetPackageFilesAsync(PublishOptions options)
        {
            var files = new List<string>();
            var ignorePatterns = await LoadIgnorePatternsAsync();
            
            // Add default ignore patterns
            ignorePatterns.AddRange(new[]
            {
                ".git", ".ouro", "node_modules", "packages",
                "bin", "obj", ".vs", "*.user", "*.suo"
            });
            
            // Include source files
            foreach (var file in Directory.GetFiles(workingDirectory, "*", SearchOption.AllDirectories))
            {
                var relativePath = Path.GetRelativePath(workingDirectory, file);
                
                // Check ignore patterns
                if (ShouldIgnore(relativePath, ignorePatterns))
                    continue;
                    
                // Check include patterns if specified
                if (options.Include?.Any() == true)
                {
                    if (!options.Include.Any(pattern => MatchesPattern(relativePath, pattern)))
                        continue;
                }
                
                files.Add(file);
            }
            
            return files;
        }
        
        private async Task<List<string>> LoadIgnorePatternsAsync()
        {
            var patterns = new List<string>();
            var ignorePath = Path.Combine(workingDirectory, ".ouroignore");
            
            if (File.Exists(ignorePath))
            {
                var lines = await File.ReadAllLinesAsync(ignorePath);
                patterns.AddRange(lines.Where(l => !string.IsNullOrWhiteSpace(l) && !l.StartsWith('#')));
            }
            
            return patterns;
        }
        
        private bool ShouldIgnore(string path, List<string> patterns)
        {
            return patterns.Any(pattern => MatchesPattern(path, pattern));
        }
        
        private bool MatchesPattern(string path, string pattern)
        {
            // Simple glob pattern matching
            return path.Contains(pattern) || 
                   Path.GetFileName(path).Contains(pattern);
        }
        
        private bool IsExecutable(string path)
        {
            var ext = Path.GetExtension(path).ToLower();
            return ext == ".exe" || ext == ".dll" || ext == ".sh" || ext == ".bat";
        }
        
        private async Task SignPackageAsync(string packagePath, string signingKey)
        {
            Console.WriteLine("🔐 Signing package...");
            
            using var rsa = RSA.Create();
            rsa.ImportFromPem(signingKey);
            
            var packageData = await File.ReadAllBytesAsync(packagePath);
            var signature = rsa.SignData(packageData, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            
            // Write signature file
            var signaturePath = packagePath + ".sig";
            await File.WriteAllBytesAsync(signaturePath, signature);
            
            Console.WriteLine($"✅ Package signed: {signaturePath}");
        }
        
        private async Task<string> CalculateChecksumAsync(string filePath)
        {
            using var stream = File.OpenRead(filePath);
            using var sha256 = SHA256.Create();
            var hash = await sha256.ComputeHashAsync(stream);
            return Convert.ToBase64String(hash);
        }
    }
    
    /// <summary>
    /// Package validator
    /// </summary>
    public class PackageValidator
    {
        public async Task<ValidationResult> ValidateAsync(PackageMetadata metadata, string workingDirectory)
        {
            var errors = new List<string>();
            
            // Validate required fields
            if (string.IsNullOrWhiteSpace(metadata.Name))
                errors.Add("Package name is required");
                
            if (!IsValidPackageName(metadata.Name))
                errors.Add("Invalid package name. Use lowercase letters, numbers, and hyphens only.");
                
            if (string.IsNullOrWhiteSpace(metadata.Version))
                errors.Add("Package version is required");
                
            if (!IsValidVersion(metadata.Version))
                errors.Add("Invalid version format. Use semantic versioning (e.g., 1.0.0)");
                
            if (string.IsNullOrWhiteSpace(metadata.Description))
                errors.Add("Package description is required");
                
            if (string.IsNullOrWhiteSpace(metadata.Author))
                errors.Add("Package author is required");
                
            if (string.IsNullOrWhiteSpace(metadata.License))
                errors.Add("Package license is required");
            
            // Validate main entry point if specified
            if (!string.IsNullOrEmpty(metadata.Main))
            {
                var mainPath = Path.Combine(workingDirectory, metadata.Main);
                if (!File.Exists(mainPath))
                    errors.Add($"Main entry point not found: {metadata.Main}");
            }
            
            // Validate scripts
            if (metadata.Scripts != null)
            {
                foreach (var script in metadata.Scripts)
                {
                    if (string.IsNullOrWhiteSpace(script.Value))
                        errors.Add($"Empty script: {script.Key}");
                }
            }
            
            // Validate dependencies
            if (metadata.Dependencies != null)
            {
                foreach (var dep in metadata.Dependencies)
                {
                    if (!IsValidPackageName(dep.Key))
                        errors.Add($"Invalid dependency name: {dep.Key}");
                        
                    if (!IsValidVersionRange(dep.Value))
                        errors.Add($"Invalid version range for {dep.Key}: {dep.Value}");
                }
            }
            
            // Check package size
            var totalSize = GetDirectorySize(workingDirectory);
            if (totalSize > 100 * 1024 * 1024) // 100MB limit
            {
                errors.Add($"Package too large: {totalSize / (1024 * 1024)}MB (limit: 100MB)");
            }
            
            return new ValidationResult
            {
                IsValid = errors.Count == 0,
                Errors = errors.ToArray()
            };
        }
        
        private bool IsValidPackageName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return false;
                
            // Package names should be lowercase, alphanumeric with hyphens
            return System.Text.RegularExpressions.Regex.IsMatch(
                name, 
                @"^[a-z][a-z0-9-]*$"
            );
        }
        
        private bool IsValidVersion(string version)
        {
            try
            {
                VersionResolver.SemanticVersion.Parse(version);
                return true;
            }
            catch
            {
                return false;
            }
        }
        
        private bool IsValidVersionRange(string range)
        {
            try
            {
                VersionResolver.ParseRange(range);
                return true;
            }
            catch
            {
                return false;
            }
        }
        
        private long GetDirectorySize(string path)
        {
            return Directory.GetFiles(path, "*", SearchOption.AllDirectories)
                .Sum(file => new FileInfo(file).Length);
        }
    }
    
    public class PublishOptions
    {
        public bool Force { get; set; }
        public bool Sign { get; set; }
        public string? SigningKey { get; set; }
        public string? Tag { get; set; }
        public List<string>? Include { get; set; }
        public bool KeepPackage { get; set; }
    }
    
    public class PublishResult
    {
        public bool Success { get; set; }
        public string[]? Errors { get; set; }
        public string? PackagePath { get; set; }
        public PackageMetadata? Metadata { get; set; }
    }
    
    public class ValidationResult
    {
        public bool IsValid { get; set; }
        public string[] Errors { get; set; } = Array.Empty<string>();
    }
} 