using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Text.Json;
using System.IO.Compression;
using System.CommandLine;
using System.CommandLine.Invocation;
using Ouroboros.Tools.Opm;

namespace Ouroboros.PackageManager
{
    /// <summary>
    /// Ouroboros Package Manager (OPM)
    /// </summary>
    public class OuroborosPackageManager
    {
        private const string REGISTRY_URL = "https://packages.ouroboros-lang.org";
        private const string PACKAGES_DIR = ".ouro/packages";
        private const string MANIFEST_FILE = "ouro.json";
        private const string LOCK_FILE = "ouro.lock";
        
        private readonly HttpClient httpClient;
        private readonly string workingDirectory;
        private PackageManifest manifest;
        private PackageLock lockFile;
        
        public OuroborosPackageManager(string workingDirectory = null)
        {
            this.workingDirectory = workingDirectory ?? Directory.GetCurrentDirectory();
            this.httpClient = new HttpClient();
            
            LoadManifest();
            LoadLockFile();
        }
        
        /// <summary>
        /// Initialize a new Ouroboros project
        /// </summary>
        public async Task InitAsync(string projectName)
        {
            Console.WriteLine($"Initializing new Ouroboros project: {projectName}");
            
            // Create manifest
            manifest = new PackageManifest
            {
                Name = projectName,
                Version = "1.0.0",
                Description = "",
                Author = Environment.UserName,
                License = "MIT",
                Main = "main.ouro",
                Dependencies = new Dictionary<string, string>(),
                DevDependencies = new Dictionary<string, string>(),
                Scripts = new Dictionary<string, string>
                {
                    ["build"] = "ouro compile main.ouro",
                    ["run"] = "ouro run main.ouro",
                    ["test"] = "ouro test"
                }
            };
            
            // Save manifest
            SaveManifest();
            
            // Create main file
            var mainFile = Path.Combine(workingDirectory, "main.ouro");
            if (!File.Exists(mainFile))
            {
                await File.WriteAllTextAsync(mainFile, @"// Welcome to Ouroboros!

@high
print ""Hello, World!""
");
            }
            
            // Create .gitignore
            var gitignore = Path.Combine(workingDirectory, ".gitignore");
            if (!File.Exists(gitignore))
            {
                await File.WriteAllTextAsync(gitignore, @".ouro/
*.ob
*.exe
*.dll
");
            }
            
            Console.WriteLine("Project initialized successfully!");
            Console.WriteLine("Run 'opm install' to install dependencies.");
        }
        
        /// <summary>
        /// Install packages
        /// </summary>
        public async Task InstallAsync(string packageName = null, bool isDev = false)
        {
            if (packageName == null)
            {
                // Install all dependencies from manifest
                await InstallAllDependenciesAsync();
            }
            else
            {
                // Install specific package
                await InstallPackageAsync(packageName, isDev);
            }
        }
        
        /// <summary>
        /// Uninstall package
        /// </summary>
        public async Task UninstallAsync(string packageName)
        {
            Console.WriteLine($"Uninstalling {packageName}...");
            
            // Remove from manifest
            manifest.Dependencies.Remove(packageName);
            manifest.DevDependencies.Remove(packageName);
            SaveManifest();
            
            // Remove from lock file
            lockFile?.Packages.RemoveAll(p => p.Name == packageName);
            SaveLockFile();
            
            // Remove package directory
            var packageDir = Path.Combine(workingDirectory, PACKAGES_DIR, packageName);
            if (Directory.Exists(packageDir))
            {
                Directory.Delete(packageDir, true);
            }
            
            Console.WriteLine($"{packageName} uninstalled successfully!");
        }
        
        /// <summary>
        /// Update packages
        /// </summary>
        public async Task UpdateAsync(string packageName = null)
        {
            if (packageName == null)
            {
                // Update all packages
                Console.WriteLine("Updating all packages...");
                
                foreach (var dep in manifest.Dependencies.Keys.ToList())
                {
                    await UpdatePackageAsync(dep);
                }
                
                foreach (var dep in manifest.DevDependencies.Keys.ToList())
                {
                    await UpdatePackageAsync(dep);
                }
            }
            else
            {
                // Update specific package
                await UpdatePackageAsync(packageName);
            }
        }
        
        /// <summary>
        /// Search for packages
        /// </summary>
        public async Task SearchAsync(string query)
        {
            Console.WriteLine($"Searching for packages matching '{query}'...");
            
            try
            {
                var response = await httpClient.GetAsync($"{REGISTRY_URL}/api/search?q={query}");
                response.EnsureSuccessStatusCode();
                
                var json = await response.Content.ReadAsStringAsync();
                var results = JsonSerializer.Deserialize<List<PackageSearchResult>>(json);
                
                if (results.Count == 0)
                {
                    Console.WriteLine("No packages found.");
                    return;
                }
                
                Console.WriteLine($"\nFound {results.Count} packages:\n");
                
                foreach (var result in results)
                {
                    Console.WriteLine($"{result.Name} ({result.Version}) - {result.Description}");
                    Console.WriteLine($"  Author: {result.Author}");
                    Console.WriteLine($"  Downloads: {result.Downloads}");
                    Console.WriteLine();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error searching packages: {ex.Message}");
            }
        }
        
        /// <summary>
        /// List installed packages
        /// </summary>
        public void List()
        {
            Console.WriteLine("Installed packages:\n");
            
            if (manifest.Dependencies.Count > 0)
            {
                Console.WriteLine("Dependencies:");
                foreach (var dep in manifest.Dependencies)
                {
                    var installed = lockFile?.Packages.FirstOrDefault(p => p.Name == dep.Key);
                    Console.WriteLine($"  {dep.Key} {dep.Value} (installed: {installed?.Version ?? "not installed"})");
                }
                Console.WriteLine();
            }
            
            if (manifest.DevDependencies.Count > 0)
            {
                Console.WriteLine("Dev Dependencies:");
                foreach (var dep in manifest.DevDependencies)
                {
                    var installed = lockFile?.Packages.FirstOrDefault(p => p.Name == dep.Key);
                    Console.WriteLine($"  {dep.Key} {dep.Value} (installed: {installed?.Version ?? "not installed"})");
                }
            }
        }
        
        /// <summary>
        /// Run a script from manifest
        /// </summary>
        public async Task RunScriptAsync(string scriptName)
        {
            if (!manifest.Scripts.TryGetValue(scriptName, out var script))
            {
                Console.WriteLine($"Script '{scriptName}' not found in manifest.");
                return;
            }
            
            Console.WriteLine($"Running script '{scriptName}': {script}");
            
            // Execute script
            var process = new System.Diagnostics.Process
            {
                StartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/c {script}",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    WorkingDirectory = workingDirectory
                }
            };
            
            process.Start();
            
            var output = await process.StandardOutput.ReadToEndAsync();
            var error = await process.StandardError.ReadToEndAsync();
            
            await process.WaitForExitAsync();
            
            if (!string.IsNullOrEmpty(output))
                Console.WriteLine(output);
                
            if (!string.IsNullOrEmpty(error))
                Console.WriteLine(error);
        }
        
        /// <summary>
        /// Publish package to registry
        /// </summary>
        public async Task PublishAsync()
        {
            Console.WriteLine("Publishing package...");
            
            // Validate manifest
            if (string.IsNullOrEmpty(manifest.Name))
            {
                Console.WriteLine("Error: Package name is required.");
                return;
            }
            
            if (string.IsNullOrEmpty(manifest.Version))
            {
                Console.WriteLine("Error: Package version is required.");
                return;
            }
            
            // Create package archive
            var packageFile = $"{manifest.Name}-{manifest.Version}.ouro-pkg";
            
            // Create package archive
            CreatePackageArchive(packageFile);
            
            // Upload to registry
            try
            {
                using var content = new MultipartFormDataContent();
                content.Add(new StreamContent(File.OpenRead(packageFile)), "package", packageFile);
                
                var response = await httpClient.PostAsync($"{REGISTRY_URL}/api/publish", content);
                response.EnsureSuccessStatusCode();
                
                Console.WriteLine($"Package {manifest.Name}@{manifest.Version} published successfully!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error publishing package: {ex.Message}");
            }
        }
        
        #region Private Methods
        
        private void LoadManifest()
        {
            var manifestPath = Path.Combine(workingDirectory, MANIFEST_FILE);
            if (File.Exists(manifestPath))
            {
                var json = File.ReadAllText(manifestPath);
                manifest = JsonSerializer.Deserialize<PackageManifest>(json);
            }
        }
        
        private void SaveManifest()
        {
            var manifestPath = Path.Combine(workingDirectory, MANIFEST_FILE);
            var options = new JsonSerializerOptions { WriteIndented = true };
            var json = JsonSerializer.Serialize(manifest, options);
            File.WriteAllText(manifestPath, json);
        }
        
        private void LoadLockFile()
        {
            var lockPath = Path.Combine(workingDirectory, LOCK_FILE);
            if (File.Exists(lockPath))
            {
                var json = File.ReadAllText(lockPath);
                lockFile = JsonSerializer.Deserialize<PackageLock>(json);
            }
            else
            {
                lockFile = new PackageLock { Packages = new List<PackageLockEntry>() };
            }
        }
        
        private void SaveLockFile()
        {
            var lockPath = Path.Combine(workingDirectory, LOCK_FILE);
            var options = new JsonSerializerOptions { WriteIndented = true };
            var json = JsonSerializer.Serialize(lockFile, options);
            File.WriteAllText(lockPath, json);
        }
        
        private async Task InstallAllDependenciesAsync()
        {
            Console.WriteLine("Installing dependencies...");
            
            // Install regular dependencies
            foreach (var dep in manifest.Dependencies)
            {
                await InstallPackageVersionAsync(dep.Key, dep.Value, false);
            }
            
            // Install dev dependencies
            foreach (var dep in manifest.DevDependencies)
            {
                await InstallPackageVersionAsync(dep.Key, dep.Value, true);
            }
            
            SaveLockFile();
            Console.WriteLine("All dependencies installed successfully!");
        }
        
        private async Task InstallPackageAsync(string packageSpec, bool isDev)
        {
            // Parse package spec (name@version)
            var parts = packageSpec.Split('@');
            var packageName = parts[0];
            var version = parts.Length > 1 ? parts[1] : "latest";
            
            // Add to manifest
            if (isDev)
                manifest.DevDependencies[packageName] = version;
            else
                manifest.Dependencies[packageName] = version;
                
            SaveManifest();
            
            // Install package
            await InstallPackageVersionAsync(packageName, version, isDev);
            SaveLockFile();
        }
        
        private async Task InstallPackageVersionAsync(string packageName, string version, bool isDev)
        {
            Console.WriteLine($"Installing {packageName}@{version}...");
            
            try
            {
                // Resolve version
                if (version == "latest")
                {
                    version = await ResolveLatestVersionAsync(packageName);
                }
                
                // Check if already installed
                var existing = lockFile.Packages.FirstOrDefault(p => p.Name == packageName);
                if (existing != null && existing.Version == version)
                {
                    Console.WriteLine($"{packageName}@{version} is already installed.");
                    return;
                }
                
                // Download package
                var packageData = await DownloadPackageAsync(packageName, version);
                
                // Extract to packages directory
                var packageDir = Path.Combine(workingDirectory, PACKAGES_DIR, packageName);
                Directory.CreateDirectory(packageDir);
                
                // Extract package archive
                ExtractPackageArchive(packageData, packageDir);
                
                // Update lock file
                if (existing != null)
                {
                    existing.Version = version;
                    existing.IsDev = isDev;
                }
                else
                {
                    lockFile.Packages.Add(new PackageLockEntry
                    {
                        Name = packageName,
                        Version = version,
                        IsDev = isDev
                    });
                }
                
                Console.WriteLine($"{packageName}@{version} installed successfully!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error installing {packageName}: {ex.Message}");
            }
        }
        
        private async Task UpdatePackageAsync(string packageName)
        {
            Console.WriteLine($"Updating {packageName}...");
            
            var currentVersion = lockFile?.Packages.FirstOrDefault(p => p.Name == packageName)?.Version;
            var latestVersion = await ResolveLatestVersionAsync(packageName);
            
            if (currentVersion == latestVersion)
            {
                Console.WriteLine($"{packageName} is already up to date.");
                return;
            }
            
            var isDev = manifest.DevDependencies.ContainsKey(packageName);
            await InstallPackageVersionAsync(packageName, latestVersion, isDev);
            
            // Update manifest
            if (isDev)
                manifest.DevDependencies[packageName] = latestVersion;
            else
                manifest.Dependencies[packageName] = latestVersion;
                
            SaveManifest();
            SaveLockFile();
        }
        
        private async Task<string> ResolveLatestVersionAsync(string packageName)
        {
            var response = await httpClient.GetAsync($"{REGISTRY_URL}/api/packages/{packageName}/latest");
            response.EnsureSuccessStatusCode();
            
            var json = await response.Content.ReadAsStringAsync();
            var info = JsonSerializer.Deserialize<PackageInfo>(json);
            
            return info.Version;
        }
        
        private async Task<byte[]> DownloadPackageAsync(string packageName, string version)
        {
            var response = await httpClient.GetAsync($"{REGISTRY_URL}/api/packages/{packageName}/{version}/download");
            response.EnsureSuccessStatusCode();
            
            return await response.Content.ReadAsByteArrayAsync();
        }
        
        private void CreatePackageArchive(string packageFile)
        {
            // Create a ZIP archive containing the package files
            using (var zipArchive = ZipFile.Open(packageFile, ZipArchiveMode.Create))
            {
                // Add manifest file
                zipArchive.CreateEntryFromFile(
                    Path.Combine(workingDirectory, MANIFEST_FILE),
                    MANIFEST_FILE
                );
                
                // Add source files (excluding node_modules, .ouro, etc.)
                var filesToInclude = Directory.GetFiles(workingDirectory, "*.*", SearchOption.AllDirectories)
                    .Where(file => !file.Contains(Path.DirectorySeparatorChar + ".ouro" + Path.DirectorySeparatorChar) &&
                                   !file.Contains(Path.DirectorySeparatorChar + "ouro_modules" + Path.DirectorySeparatorChar) &&
                                   !file.Contains(Path.DirectorySeparatorChar + ".git" + Path.DirectorySeparatorChar) &&
                                   !file.EndsWith(".ouro-pkg") &&
                                   !file.EndsWith("ouro.lock"))
                    .ToList();
                
                foreach (var file in filesToInclude)
                {
                    var relativePath = Path.GetRelativePath(workingDirectory, file);
                    zipArchive.CreateEntryFromFile(file, relativePath);
                }
            }
        }
        
        private void ExtractPackageArchive(byte[] packageData, string targetDirectory)
        {
            // Save package data to temporary file
            var tempFile = Path.GetTempFileName();
            try
            {
                File.WriteAllBytes(tempFile, packageData);
                
                // Extract ZIP archive
                ZipFile.ExtractToDirectory(tempFile, targetDirectory, overwriteFiles: true);
            }
            finally
            {
                // Clean up temporary file
                if (File.Exists(tempFile))
                {
                    File.Delete(tempFile);
                }
            }
        }
        
        #endregion
    }
    
    #region Data Models
    
    public class PackageManifest
    {
        public string Name { get; set; }
        public string Version { get; set; }
        public string Description { get; set; }
        public string Author { get; set; }
        public string License { get; set; }
        public string Main { get; set; }
        public Dictionary<string, string> Dependencies { get; set; }
        public Dictionary<string, string> DevDependencies { get; set; }
        public Dictionary<string, string> Scripts { get; set; }
    }
    
    public class PackageLock
    {
        public List<PackageLockEntry> Packages { get; set; }
    }
    
    public class PackageLockEntry
    {
        public string Name { get; set; }
        public string Version { get; set; }
        public bool IsDev { get; set; }
    }
    
    public class PackageSearchResult
    {
        public string Name { get; set; }
        public string Version { get; set; }
        public string Description { get; set; }
        public string Author { get; set; }
        public int Downloads { get; set; }
    }
    
    public class PackageInfo
    {
        public string Name { get; set; }
        public string Version { get; set; }
    }
    
    #endregion
    
    /// <summary>
    /// Command line interface for OPM
    /// </summary>
    public class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("Ouroboros Package Manager (OPM) v1.0.0\n");
            
            if (args.Length == 0)
            {
                ShowHelp();
                return;
            }
            
            var opm = new OuroborosPackageManager();
            var command = args[0].ToLower();
            
            try
            {
                switch (command)
                {
                    case "init":
                        var projectName = args.Length > 1 ? args[1] : Path.GetFileName(Directory.GetCurrentDirectory());
                        await opm.InitAsync(projectName);
                        break;
                        
                    case "install":
                    case "i":
                        if (args.Length > 1)
                        {
                            var isDev = args.Contains("--dev") || args.Contains("-D");
                            await opm.InstallAsync(args[1], isDev);
                        }
                        else
                        {
                            await opm.InstallAsync();
                        }
                        break;
                        
                    case "uninstall":
                    case "remove":
                    case "rm":
                        if (args.Length > 1)
                        {
                            await opm.UninstallAsync(args[1]);
                        }
                        else
                        {
                            Console.WriteLine("Please specify a package to uninstall.");
                        }
                        break;
                        
                    case "update":
                    case "upgrade":
                        if (args.Length > 1)
                        {
                            await opm.UpdateAsync(args[1]);
                        }
                        else
                        {
                            await opm.UpdateAsync();
                        }
                        break;
                        
                    case "search":
                        if (args.Length > 1)
                        {
                            await opm.SearchAsync(args[1]);
                        }
                        else
                        {
                            Console.WriteLine("Please specify a search query.");
                        }
                        break;
                        
                    case "list":
                    case "ls":
                        opm.List();
                        break;
                        
                    case "run":
                        if (args.Length > 1)
                        {
                            await opm.RunScriptAsync(args[1]);
                        }
                        else
                        {
                            Console.WriteLine("Please specify a script to run.");
                        }
                        break;
                        
                    case "publish":
                        await opm.PublishAsync();
                        break;
                        
                    case "help":
                    case "--help":
                    case "-h":
                        ShowHelp();
                        break;
                        
                    default:
                        Console.WriteLine($"Unknown command: {command}");
                        ShowHelp();
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                Environment.Exit(1);
            }
        }
        
        static void ShowHelp()
        {
            Console.WriteLine(@"Usage: opm <command> [options]

Commands:
  init [name]              Initialize a new Ouroboros project
  install [package]        Install a package (alias: i)
    --dev, -D             Install as dev dependency
  uninstall <package>      Uninstall a package (aliases: remove, rm)
  update [package]         Update package(s) (alias: upgrade)
  search <query>           Search for packages
  list                     List installed packages (alias: ls)
  run <script>             Run a script from ouro.json
  publish                  Publish package to registry
  help                     Show this help message

Examples:
  opm init my-project      Create a new project
  opm install              Install all dependencies
  opm install http         Install the 'http' package
  opm install test --dev   Install 'test' as dev dependency
  opm search math          Search for math packages
  opm run build            Run the 'build' script
");
        }
    }
}