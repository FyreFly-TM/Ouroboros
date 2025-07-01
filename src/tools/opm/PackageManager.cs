using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Ouroboros.Tools.Opm
{
    /// <summary>
    /// Enhanced Ouroboros Package Manager
    /// </summary>
    public class PackageManager
    {
        private readonly PackageRegistry registry;
        private readonly PackageCache cache;
        private readonly DependencyResolver resolver;
        private readonly HttpClient httpClient;
        private readonly string packagesDirectory;

        public PackageManager(string packagesDirectory)
        {
            this.packagesDirectory = packagesDirectory;
            Directory.CreateDirectory(packagesDirectory);
            
            registry = new PackageRegistry();
            cache = new PackageCache(Path.Combine(packagesDirectory, ".cache"));
            resolver = new DependencyResolver();
            httpClient = new HttpClient();
        }

        /// <summary>
        /// Install a package
        /// </summary>
        public async Task<InstallResult> InstallAsync(string packageName, string? version = null)
        {
            Console.WriteLine($"Installing {packageName}{(version != null ? $"@{version}" : "")}...");
            
            try
            {
                // Resolve package and dependencies
                var package = await registry.GetPackageAsync(packageName, version);
                if (package == null)
                {
                    return new InstallResult
                    {
                        Success = false,
                        Error = $"Package '{packageName}' not found"
                    };
                }

                // Check cache first
                if (await cache.ExistsAsync(package))
                {
                    Console.WriteLine($"Using cached version of {package.Name}@{package.Version}");
                }
                else
                {
                    // Download package
                    await DownloadPackageAsync(package);
                }

                // Resolve dependencies
                var dependencies = await resolver.ResolveAsync(package);
                
                // Install dependencies first
                foreach (var dep in dependencies)
                {
                    if (!await IsInstalledAsync(dep))
                    {
                        var depResult = await InstallAsync(dep.Name, dep.Version);
                        if (!depResult.Success)
                        {
                            return depResult;
                        }
                    }
                }

                // Extract and install package
                await ExtractPackageAsync(package);
                
                // Run install scripts
                await RunInstallScriptsAsync(package);
                
                // Update manifest
                await UpdateManifestAsync(package);
                
                return new InstallResult
                {
                    Success = true,
                    InstalledPackage = package,
                    InstalledDependencies = dependencies
                };
            }
            catch (Exception ex)
            {
                return new InstallResult
                {
                    Success = false,
                    Error = ex.Message
                };
            }
        }

        /// <summary>
        /// Uninstall a package
        /// </summary>
        public async Task<bool> UninstallAsync(string packageName)
        {
            Console.WriteLine($"Uninstalling {packageName}...");
            
            var manifest = await LoadManifestAsync();
            if (!manifest.Dependencies.ContainsKey(packageName))
            {
                Console.WriteLine($"Package '{packageName}' is not installed");
                return false;
            }

            // Check if other packages depend on this
            var dependents = GetDependents(packageName, manifest);
            if (dependents.Any())
            {
                Console.WriteLine($"Cannot uninstall {packageName} because it is required by:");
                foreach (var dep in dependents)
                {
                    Console.WriteLine($"  - {dep}");
                }
                return false;
            }

            // Remove package files
            var packageDir = Path.Combine(packagesDirectory, packageName);
            if (Directory.Exists(packageDir))
            {
                Directory.Delete(packageDir, recursive: true);
            }

            // Update manifest
            manifest.Dependencies.Remove(packageName);
            await SaveManifestAsync(manifest);
            
            Console.WriteLine($"Package '{packageName}' uninstalled successfully");
            return true;
        }

        /// <summary>
        /// Update packages
        /// </summary>
        public async Task<UpdateResult> UpdateAsync(string? packageName = null)
        {
            var manifest = await LoadManifestAsync();
            var updates = new List<PackageInfo>();
            
            if (packageName != null)
            {
                // Update specific package
                if (!manifest.Dependencies.ContainsKey(packageName))
                {
                    return new UpdateResult
                    {
                        Success = false,
                        Error = $"Package '{packageName}' is not installed"
                    };
                }

                var currentVersion = manifest.Dependencies[packageName];
                var latestPackage = await registry.GetLatestVersionAsync(packageName);
                
                if (latestPackage != null && IsNewerVersion(latestPackage.Version, currentVersion))
                {
                    var result = await InstallAsync(packageName, latestPackage.Version);
                    if (result.Success)
                    {
                        updates.Add(latestPackage);
                    }
                }
            }
            else
            {
                // Update all packages
                foreach (var dep in manifest.Dependencies)
                {
                    var latestPackage = await registry.GetLatestVersionAsync(dep.Key);
                    if (latestPackage != null && IsNewerVersion(latestPackage.Version, dep.Value))
                    {
                        Console.WriteLine($"Updating {dep.Key} from {dep.Value} to {latestPackage.Version}");
                        var result = await InstallAsync(dep.Key, latestPackage.Version);
                        if (result.Success)
                        {
                            updates.Add(latestPackage);
                        }
                    }
                }
            }
            
            return new UpdateResult
            {
                Success = true,
                UpdatedPackages = updates
            };
        }

        /// <summary>
        /// List installed packages
        /// </summary>
        public async Task<List<InstalledPackage>> ListInstalledAsync()
        {
            var manifest = await LoadManifestAsync();
            var installed = new List<InstalledPackage>();
            
            foreach (var dep in manifest.Dependencies)
            {
                var packageDir = Path.Combine(packagesDirectory, dep.Key);
                if (Directory.Exists(packageDir))
                {
                    var info = await LoadPackageInfoAsync(packageDir);
                    if (info != null)
                    {
                        installed.Add(new InstalledPackage
                        {
                            Name = dep.Key,
                            Version = dep.Value,
                            Info = info,
                            Size = GetDirectorySize(packageDir)
                        });
                    }
                }
            }
            
            return installed.OrderBy(p => p.Name).ToList();
        }

        /// <summary>
        /// Search for packages
        /// </summary>
        public async Task<List<PackageInfo>> SearchAsync(string query)
        {
            return await registry.SearchAsync(query);
        }

        /// <summary>
        /// Create a new package
        /// </summary>
        public async Task<bool> CreatePackageAsync(string directory, PackageMetadata metadata)
        {
            var packageFile = Path.Combine(directory, "package.ouro.json");
            
            // Validate metadata
            if (string.IsNullOrEmpty(metadata.Name))
            {
                Console.WriteLine("Error: Package name is required");
                return false;
            }
            
            if (string.IsNullOrEmpty(metadata.Version))
            {
                Console.WriteLine("Error: Package version is required");
                return false;
            }
            
            // Create package.ouro.json
            var packageJson = JsonSerializer.Serialize(metadata, new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
            
            await File.WriteAllTextAsync(packageFile, packageJson);
            
            Console.WriteLine($"Created package.ouro.json for {metadata.Name}@{metadata.Version}");
            return true;
        }

        /// <summary>
        /// Publish a package
        /// </summary>
        public async Task<bool> PublishAsync(string directory)
        {
            var packageFile = Path.Combine(directory, "package.ouro.json");
            if (!File.Exists(packageFile))
            {
                Console.WriteLine("Error: package.ouro.json not found");
                return false;
            }
            
            var metadata = await LoadPackageMetadataAsync(packageFile);
            if (metadata == null)
            {
                Console.WriteLine("Error: Invalid package.ouro.json");
                return false;
            }
            
            Console.WriteLine($"Publishing {metadata.Name}@{metadata.Version}...");
            
            // Create package archive
            var archivePath = Path.Combine(Path.GetTempPath(), $"{metadata.Name}-{metadata.Version}.ouro.zip");
            await CreatePackageArchiveAsync(directory, archivePath, metadata);
            
            // Calculate checksum
            var checksum = await CalculateChecksumAsync(archivePath);
            
            // Upload to registry
            var success = await registry.PublishAsync(archivePath, metadata, checksum);
            
            // Clean up
            File.Delete(archivePath);
            
            if (success)
            {
                Console.WriteLine($"Successfully published {metadata.Name}@{metadata.Version}");
            }
            else
            {
                Console.WriteLine("Failed to publish package");
            }
            
            return success;
        }

        private async Task DownloadPackageAsync(PackageInfo package)
        {
            Console.WriteLine($"Downloading {package.Name}@{package.Version}...");
            
            var response = await httpClient.GetAsync(package.DownloadUrl);
            response.EnsureSuccessStatusCode();
            
            var content = await response.Content.ReadAsByteArrayAsync();
            
            // Verify checksum
            var checksum = CalculateChecksum(content);
            if (checksum != package.Checksum)
            {
                throw new InvalidOperationException("Package checksum verification failed");
            }
            
            // Save to cache
            await cache.StoreAsync(package, content);
        }

        private async Task ExtractPackageAsync(PackageInfo package)
        {
            var packageData = await cache.GetAsync(package);
            var targetDir = Path.Combine(packagesDirectory, package.Name);
            
            // Remove existing installation
            if (Directory.Exists(targetDir))
            {
                Directory.Delete(targetDir, recursive: true);
            }
            
            // Extract archive
            using var stream = new MemoryStream(packageData);
            using var archive = new ZipArchive(stream, ZipArchiveMode.Read);
            archive.ExtractToDirectory(targetDir);
            
            Console.WriteLine($"Extracted {package.Name} to {targetDir}");
        }

        private async Task RunInstallScriptsAsync(PackageInfo package)
        {
            var packageDir = Path.Combine(packagesDirectory, package.Name);
            var scriptsDir = Path.Combine(packageDir, "scripts");
            
            if (!Directory.Exists(scriptsDir))
                return;
                
            var installScript = Path.Combine(scriptsDir, "install.ouro");
            if (File.Exists(installScript))
            {
                Console.WriteLine("Running install script...");
                // TODO: Execute Ouroboros script
                await Task.CompletedTask;
            }
        }

        private async Task<bool> IsInstalledAsync(PackageInfo package)
        {
            var manifest = await LoadManifestAsync();
            return manifest.Dependencies.ContainsKey(package.Name) &&
                   manifest.Dependencies[package.Name] == package.Version;
        }

        private async Task UpdateManifestAsync(PackageInfo package)
        {
            var manifest = await LoadManifestAsync();
            manifest.Dependencies[package.Name] = package.Version;
            await SaveManifestAsync(manifest);
        }

        private async Task<ProjectManifest> LoadManifestAsync()
        {
            var manifestPath = Path.Combine(Directory.GetCurrentDirectory(), "project.ouro.json");
            
            if (!File.Exists(manifestPath))
            {
                return new ProjectManifest
                {
                    Name = Path.GetFileName(Directory.GetCurrentDirectory()),
                    Version = "0.1.0",
                    Dependencies = new Dictionary<string, string>()
                };
            }
            
            var json = await File.ReadAllTextAsync(manifestPath);
            return JsonSerializer.Deserialize<ProjectManifest>(json, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            }) ?? new ProjectManifest();
        }

        private async Task SaveManifestAsync(ProjectManifest manifest)
        {
            var manifestPath = Path.Combine(Directory.GetCurrentDirectory(), "project.ouro.json");
            var json = JsonSerializer.Serialize(manifest, new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
            await File.WriteAllTextAsync(manifestPath, json);
        }

        private async Task<PackageMetadata?> LoadPackageMetadataAsync(string path)
        {
            try
            {
                var json = await File.ReadAllTextAsync(path);
                return JsonSerializer.Deserialize<PackageMetadata>(json, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });
            }
            catch
            {
                return null;
            }
        }

        private async Task<PackageInfo?> LoadPackageInfoAsync(string directory)
        {
            var packageFile = Path.Combine(directory, "package.ouro.json");
            if (!File.Exists(packageFile))
                return null;
                
            var metadata = await LoadPackageMetadataAsync(packageFile);
            if (metadata == null)
                return null;
                
            return new PackageInfo
            {
                Name = metadata.Name,
                Version = metadata.Version,
                Description = metadata.Description,
                Author = metadata.Author,
                License = metadata.License,
                Dependencies = metadata.Dependencies ?? new Dictionary<string, string>()
            };
        }

        private List<string> GetDependents(string packageName, ProjectManifest manifest)
        {
            // In a real implementation, this would check all installed packages
            // to see which ones depend on the package being uninstalled
            return new List<string>();
        }

        private bool IsNewerVersion(string version1, string version2)
        {
            try
            {
                var v1 = Version.Parse(version1);
                var v2 = Version.Parse(version2);
                return v1 > v2;
            }
            catch
            {
                return string.Compare(version1, version2, StringComparison.Ordinal) > 0;
            }
        }

        private long GetDirectorySize(string directory)
        {
            return Directory.GetFiles(directory, "*", SearchOption.AllDirectories)
                .Sum(file => new FileInfo(file).Length);
        }

        private async Task CreatePackageArchiveAsync(string sourceDir, string archivePath, PackageMetadata metadata)
        {
            // Create .ouroignore aware archive
            var ignorePatterns = await LoadIgnorePatternsAsync(sourceDir);
            
            using var archive = ZipFile.Open(archivePath, ZipArchiveMode.Create);
            
            foreach (var file in GetFilesToPackage(sourceDir, ignorePatterns))
            {
                var relativePath = Path.GetRelativePath(sourceDir, file);
                archive.CreateEntryFromFile(file, relativePath);
            }
        }

        private async Task<List<string>> LoadIgnorePatternsAsync(string directory)
        {
            var patterns = new List<string> { ".git", "node_modules", "bin", "obj", ".vs" };
            var ignoreFile = Path.Combine(directory, ".ouroignore");
            
            if (File.Exists(ignoreFile))
            {
                var lines = await File.ReadAllLinesAsync(ignoreFile);
                patterns.AddRange(lines.Where(l => !string.IsNullOrWhiteSpace(l) && !l.StartsWith('#')));
            }
            
            return patterns;
        }

        private IEnumerable<string> GetFilesToPackage(string directory, List<string> ignorePatterns)
        {
            foreach (var file in Directory.GetFiles(directory, "*", SearchOption.AllDirectories))
            {
                var relativePath = Path.GetRelativePath(directory, file);
                if (!ignorePatterns.Any(p => relativePath.Contains(p)))
                {
                    yield return file;
                }
            }
        }

        private async Task<string> CalculateChecksumAsync(string filePath)
        {
            using var stream = File.OpenRead(filePath);
            using var sha256 = SHA256.Create();
            var hash = await sha256.ComputeHashAsync(stream);
            return Convert.ToBase64String(hash);
        }

        private string CalculateChecksum(byte[] data)
        {
            using var sha256 = SHA256.Create();
            var hash = sha256.ComputeHash(data);
            return Convert.ToBase64String(hash);
        }
    }

    /// <summary>
    /// Package registry interface
    /// </summary>
    public class PackageRegistry
    {
        private readonly string registryUrl;
        private readonly HttpClient httpClient;

        public PackageRegistry(string registryUrl = "https://packages.ouroboros-lang.org")
        {
            this.registryUrl = registryUrl;
            this.httpClient = new HttpClient();
        }

        public async Task<PackageInfo?> GetPackageAsync(string name, string? version = null)
        {
            var url = version != null 
                ? $"{registryUrl}/api/packages/{name}/{version}"
                : $"{registryUrl}/api/packages/{name}/latest";
                
            try
            {
                var response = await httpClient.GetAsync(url);
                if (!response.IsSuccessStatusCode)
                    return null;
                    
                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<PackageInfo>(json, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });
            }
            catch
            {
                return null;
            }
        }

        public async Task<PackageInfo?> GetLatestVersionAsync(string name)
        {
            return await GetPackageAsync(name);
        }

        public async Task<List<PackageInfo>> SearchAsync(string query)
        {
            try
            {
                var response = await httpClient.GetAsync($"{registryUrl}/api/search?q={Uri.EscapeDataString(query)}");
                if (!response.IsSuccessStatusCode)
                    return new List<PackageInfo>();
                    
                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<List<PackageInfo>>(json, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                }) ?? new List<PackageInfo>();
            }
            catch
            {
                return new List<PackageInfo>();
            }
        }

        public async Task<bool> PublishAsync(string archivePath, PackageMetadata metadata, string checksum)
        {
            try
            {
                using var form = new MultipartFormDataContent();
                using var fileStream = File.OpenRead(archivePath);
                
                form.Add(new StreamContent(fileStream), "package", Path.GetFileName(archivePath));
                form.Add(new StringContent(JsonSerializer.Serialize(metadata)), "metadata");
                form.Add(new StringContent(checksum), "checksum");
                
                var response = await httpClient.PostAsync($"{registryUrl}/api/publish", form);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }
    }

    /// <summary>
    /// Package cache
    /// </summary>
    public class PackageCache
    {
        private readonly string cacheDirectory;

        public PackageCache(string cacheDirectory)
        {
            this.cacheDirectory = cacheDirectory;
            Directory.CreateDirectory(cacheDirectory);
        }

        public async Task<bool> ExistsAsync(PackageInfo package)
        {
            var path = GetCachePath(package);
            return File.Exists(path);
        }

        public async Task StoreAsync(PackageInfo package, byte[] data)
        {
            var path = GetCachePath(package);
            await File.WriteAllBytesAsync(path, data);
        }

        public async Task<byte[]> GetAsync(PackageInfo package)
        {
            var path = GetCachePath(package);
            return await File.ReadAllBytesAsync(path);
        }

        private string GetCachePath(PackageInfo package)
        {
            return Path.Combine(cacheDirectory, $"{package.Name}-{package.Version}.ouro.zip");
        }
    }

    /// <summary>
    /// Dependency resolver
    /// </summary>
    public class DependencyResolver
    {
        private readonly PackageRegistry registry = new();

        public async Task<List<PackageInfo>> ResolveAsync(PackageInfo package)
        {
            var resolved = new Dictionary<string, PackageInfo>();
            var toResolve = new Queue<PackageInfo>();
            
            toResolve.Enqueue(package);
            
            while (toResolve.Count > 0)
            {
                var current = toResolve.Dequeue();
                
                foreach (var dep in current.Dependencies)
                {
                    if (!resolved.ContainsKey(dep.Key))
                    {
                        var depPackage = await registry.GetPackageAsync(dep.Key, dep.Value);
                        if (depPackage != null)
                        {
                            resolved[dep.Key] = depPackage;
                            toResolve.Enqueue(depPackage);
                        }
                        else
                        {
                            throw new InvalidOperationException($"Cannot resolve dependency: {dep.Key}@{dep.Value}");
                        }
                    }
                }
            }
            
            return resolved.Values.ToList();
        }
    }

    // Data models
    public class PackageInfo
    {
        public string Name { get; set; } = "";
        public string Version { get; set; } = "";
        public string Description { get; set; } = "";
        public string Author { get; set; } = "";
        public string License { get; set; } = "";
        public string DownloadUrl { get; set; } = "";
        public string Checksum { get; set; } = "";
        public Dictionary<string, string> Dependencies { get; set; } = new();
    }

    public class PackageMetadata
    {
        public string Name { get; set; } = "";
        public string Version { get; set; } = "";
        public string Description { get; set; } = "";
        public string Author { get; set; } = "";
        public string License { get; set; } = "";
        public string Homepage { get; set; } = "";
        public string Repository { get; set; } = "";
        public List<string> Keywords { get; set; } = new();
        public Dictionary<string, string>? Dependencies { get; set; }
        public Dictionary<string, string>? DevDependencies { get; set; }
        public Dictionary<string, string>? Scripts { get; set; }
    }

    public class ProjectManifest
    {
        public string Name { get; set; } = "";
        public string Version { get; set; } = "";
        public Dictionary<string, string> Dependencies { get; set; } = new();
    }

    public class InstallResult
    {
        public bool Success { get; set; }
        public string? Error { get; set; }
        public PackageInfo? InstalledPackage { get; set; }
        public List<PackageInfo>? InstalledDependencies { get; set; }
    }

    public class UpdateResult
    {
        public bool Success { get; set; }
        public string? Error { get; set; }
        public List<PackageInfo> UpdatedPackages { get; set; } = new();
    }

    public class InstalledPackage
    {
        public string Name { get; set; } = "";
        public string Version { get; set; } = "";
        public PackageInfo Info { get; set; } = new();
        public long Size { get; set; }
    }
} 