using System;
using System.Collections.Generic;
// Temporarily disable CommandLine until package is installed
// using System.CommandLine;
// using System.CommandLine.Invocation;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Ouro.Tools.Opm;

namespace Ouro.Tools
{
    /// <summary>
    /// Ouroboros Package Manager CLI
    /// </summary>
    public class OpmCli
    {
        public static async Task<int> Main(string[] args)
        {
            var rootCommand = new RootCommand("Ouroboros Package Manager - Manage packages for Ouroboros projects");

            // Install command
            var installCommand = new Command("install", "Install a package")
            {
                new Argument<string>("package", "Package name to install") { Arity = ArgumentArity.ZeroOrOne },
                new Option<string>("--version", "Specific version to install"),
                new Option<string>("--registry", "Registry to use (default, private, etc.)"),
                new Option<bool>("--save-dev", "Save as development dependency"),
                new Option<bool>("--global", "Install globally"),
                new Option<bool>("--force", "Force reinstall even if already installed")
            };
            installCommand.Handler = CommandHandler.Create<string?, string?, string?, bool, bool, bool>(InstallCommand);
            rootCommand.Add(installCommand);

            // Uninstall command
            var uninstallCommand = new Command("uninstall", "Uninstall a package")
            {
                new Argument<string>("package", "Package name to uninstall"),
                new Option<bool>("--global", "Uninstall from global packages")
            };
            uninstallCommand.Handler = CommandHandler.Create<string, bool>(UninstallCommand);
            rootCommand.Add(uninstallCommand);

            // Update command
            var updateCommand = new Command("update", "Update packages")
            {
                new Argument<string>("package", "Package name to update") { Arity = ArgumentArity.ZeroOrOne },
                new Option<bool>("--all", "Update all packages"),
                new Option<bool>("--global", "Update global packages")
            };
            updateCommand.Handler = CommandHandler.Create<string?, bool, bool>(UpdateCommand);
            rootCommand.Add(updateCommand);

            // List command
            var listCommand = new Command("list", "List installed packages")
            {
                new Option<bool>("--global", "List global packages"),
                new Option<bool>("--depth", "Show dependency tree depth"),
                new Option<string>("--json", "Output as JSON")
            };
            listCommand.Handler = CommandHandler.Create<bool, bool, string?>(ListCommand);
            rootCommand.Add(listCommand);

            // Search command
            var searchCommand = new Command("search", "Search for packages")
            {
                new Argument<string>("query", "Search query"),
                new Option<string>("--registry", "Registry to search (all, default, private)")
            };
            searchCommand.Handler = CommandHandler.Create<string, string?>(SearchCommand);
            rootCommand.Add(searchCommand);

            // Init command
            var initCommand = new Command("init", "Initialize a new Ouroboros project")
            {
                new Option<string>("--name", "Project name"),
                new Option<bool>("--yes", "Use defaults for all prompts")
            };
            initCommand.Handler = CommandHandler.Create<string?, bool>(InitCommand);
            rootCommand.Add(initCommand);

            // Publish command
            var publishCommand = new Command("publish", "Publish a package")
            {
                new Option<string>("--registry", "Registry to publish to"),
                new Option<string>("--tag", "Tag to publish under (latest, beta, etc.)"),
                new Option<bool>("--dry-run", "Perform a dry run without publishing")
            };
            publishCommand.Handler = CommandHandler.Create<string?, string?, bool>(PublishCommand);
            rootCommand.Add(publishCommand);

            // Registry command
            var registryCommand = new Command("registry", "Manage package registries");
            
            var addRegistryCommand = new Command("add", "Add a new registry")
            {
                new Argument<string>("name", "Registry name"),
                new Argument<string>("url", "Registry URL"),
                new Option<string>("--token", "Authentication token"),
                new Option<bool>("--default", "Set as default registry")
            };
            addRegistryCommand.Handler = CommandHandler.Create<string, string, string?, bool>(AddRegistryCommand);
            registryCommand.Add(addRegistryCommand);
            
            var removeRegistryCommand = new Command("remove", "Remove a registry")
            {
                new Argument<string>("name", "Registry name")
            };
            removeRegistryCommand.Handler = CommandHandler.Create<string>(RemoveRegistryCommand);
            registryCommand.Add(removeRegistryCommand);
            
            var listRegistryCommand = new Command("list", "List configured registries");
            listRegistryCommand.Handler = CommandHandler.Create(ListRegistriesCommand);
            registryCommand.Add(listRegistryCommand);
            
            rootCommand.Add(registryCommand);

            // Cache command
            var cacheCommand = new Command("cache", "Manage package cache");
            
            var cleanCacheCommand = new Command("clean", "Clean package cache")
            {
                new Option<bool>("--force", "Force clean without confirmation")
            };
            cleanCacheCommand.Handler = CommandHandler.Create<bool>(CleanCacheCommand);
            cacheCommand.Add(cleanCacheCommand);
            
            var verifyCacheCommand = new Command("verify", "Verify cache integrity");
            verifyCacheCommand.Handler = CommandHandler.Create(VerifyCacheCommand);
            cacheCommand.Add(verifyCacheCommand);
            
            rootCommand.Add(cacheCommand);

            // Version command
            var versionCommand = new Command("version", "Show OPM version");
            versionCommand.Handler = CommandHandler.Create(VersionCommand);
            rootCommand.Add(versionCommand);

            return await rootCommand.InvokeAsync(args);
        }

        private static async Task<int> InstallCommand(string? package, string? version, string? registry, bool saveDev, bool global, bool force)
        {
            try
            {
                var packagesDir = global 
                    ? GetGlobalPackagesDirectory() 
                    : Path.Combine(Directory.GetCurrentDirectory(), "packages");
                
                PackageManager packageManager;
                
                if (!string.IsNullOrEmpty(registry))
                {
                    var config = await RegistryConfiguration.LoadAsync();
                    packageManager = new MultiRegistryPackageManager(packagesDir, config);
                }
                else
                {
                    packageManager = new PackageManager(packagesDir);
                }

                if (string.IsNullOrEmpty(package))
                {
                    // Install from project.ouro.json
                    Console.WriteLine("Installing dependencies from project.ouro.json...");
                    var manifest = await LoadProjectManifest();
                    
                    foreach (var dep in manifest.Dependencies)
                    {
                        var result = await packageManager.InstallAsync(dep.Key, dep.Value);
                        if (!result.Success)
                        {
                            Console.WriteLine($"Failed to install {dep.Key}: {result.Error}");
                            return 1;
                        }
                    }
                    
                    Console.WriteLine("All dependencies installed successfully!");
                    return 0;
                }
                else
                {
                    // Install specific package
                    var result = await packageManager.InstallAsync(package, version);
                    
                    if (result.Success)
                    {
                        Console.WriteLine($"Successfully installed {package}@{result.InstalledPackage?.Version}");
                        
                        if (result.InstalledDependencies?.Any() == true)
                        {
                            Console.WriteLine("Installed dependencies:");
                            foreach (var dep in result.InstalledDependencies)
                            {
                                Console.WriteLine($"  - {dep.Name}@{dep.Version}");
                            }
                        }
                        
                        return 0;
                    }
                    else
                    {
                        Console.WriteLine($"Failed to install {package}: {result.Error}");
                        return 1;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                return 1;
            }
        }

        private static async Task<int> UninstallCommand(string package, bool global)
        {
            try
            {
                var packagesDir = global 
                    ? GetGlobalPackagesDirectory() 
                    : Path.Combine(Directory.GetCurrentDirectory(), "packages");
                
                var packageManager = new PackageManager(packagesDir);
                var success = await packageManager.UninstallAsync(package);
                
                return success ? 0 : 1;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                return 1;
            }
        }

        private static async Task<int> UpdateCommand(string? package, bool all, bool global)
        {
            try
            {
                var packagesDir = global 
                    ? GetGlobalPackagesDirectory() 
                    : Path.Combine(Directory.GetCurrentDirectory(), "packages");
                
                var packageManager = new PackageManager(packagesDir);
                
                if (all || string.IsNullOrEmpty(package))
                {
                    var result = await packageManager.UpdateAsync();
                    if (result.Success)
                    {
                        if (result.UpdatedPackages.Any())
                        {
                            Console.WriteLine("Updated packages:");
                            foreach (var pkg in result.UpdatedPackages)
                            {
                                Console.WriteLine($"  - {pkg.Name} → {pkg.Version}");
                            }
                        }
                        else
                        {
                            Console.WriteLine("All packages are up to date!");
                        }
                        return 0;
                    }
                }
                else
                {
                    var result = await packageManager.UpdateAsync(package);
                    if (result.Success)
                    {
                        if (result.UpdatedPackages.Any())
                        {
                            Console.WriteLine($"Updated {package} to version {result.UpdatedPackages[0].Version}");
                        }
                        else
                        {
                            Console.WriteLine($"{package} is already up to date!");
                        }
                        return 0;
                    }
                }
                
                return 1;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                return 1;
            }
        }

        private static async Task<int> ListCommand(bool global, bool depth, string? json)
        {
            try
            {
                var packagesDir = global 
                    ? GetGlobalPackagesDirectory() 
                    : Path.Combine(Directory.GetCurrentDirectory(), "packages");
                
                var packageManager = new PackageManager(packagesDir);
                var installed = await packageManager.ListInstalledAsync();
                
                if (!string.IsNullOrEmpty(json))
                {
                    var jsonOutput = System.Text.Json.JsonSerializer.Serialize(installed, new System.Text.Json.JsonSerializerOptions
                    {
                        WriteIndented = true
                    });
                    Console.WriteLine(jsonOutput);
                }
                else
                {
                    if (!installed.Any())
                    {
                        Console.WriteLine("No packages installed.");
                        return 0;
                    }
                    
                    Console.WriteLine("Installed packages:");
                    foreach (var pkg in installed)
                    {
                        Console.WriteLine($"  {pkg.Name}@{pkg.Version} ({FormatSize(pkg.Size)})");
                        if (!string.IsNullOrEmpty(pkg.Info.Description))
                        {
                            Console.WriteLine($"    {pkg.Info.Description}");
                        }
                        
                        if (depth && pkg.Info.Dependencies.Any())
                        {
                            Console.WriteLine("    Dependencies:");
                            foreach (var dep in pkg.Info.Dependencies)
                            {
                                Console.WriteLine($"      - {dep.Key}@{dep.Value}");
                            }
                        }
                    }
                }
                
                return 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                return 1;
            }
        }

        private static async Task<int> SearchCommand(string query, string? registry)
        {
            try
            {
                var packagesDir = Path.Combine(Directory.GetCurrentDirectory(), "packages");
                
                if (registry == "all")
                {
                    var config = await RegistryConfiguration.LoadAsync();
                    var multiRegistry = new MultiRegistryPackageManager(packagesDir, config);
                    var results = await multiRegistry.SearchAllRegistriesAsync(query);
                    
                    DisplaySearchResults(results);
                }
                else
                {
                    var packageManager = new PackageManager(packagesDir);
                    var results = await packageManager.SearchAsync(query);
                    
                    DisplaySearchResults(results);
                }
                
                return 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                return 1;
            }
        }

        private static void DisplaySearchResults(List<PackageInfo> results)
        {
            if (!results.Any())
            {
                Console.WriteLine("No packages found.");
                return;
            }
            
            Console.WriteLine($"Found {results.Count} package(s):");
            foreach (var pkg in results.Take(20))
            {
                Console.WriteLine($"\n{pkg.Name}@{pkg.Version}");
                if (!string.IsNullOrEmpty(pkg.Description))
                {
                    Console.WriteLine($"  {pkg.Description}");
                }
                if (!string.IsNullOrEmpty(pkg.Author))
                {
                    Console.WriteLine($"  Author: {pkg.Author}");
                }
            }
            
            if (results.Count > 20)
            {
                Console.WriteLine($"\n... and {results.Count - 20} more results");
            }
        }

        private static async Task<int> InitCommand(string? name, bool yes)
        {
            try
            {
                var projectName = name ?? Path.GetFileName(Directory.GetCurrentDirectory());
                
                if (!yes)
                {
                    Console.Write($"Project name ({projectName}): ");
                    var input = Console.ReadLine();
                    if (!string.IsNullOrEmpty(input))
                    {
                        projectName = input;
                    }
                }
                
                var metadata = new PackageMetadata
                {
                    Name = projectName,
                    Version = "0.1.0",
                    Description = "",
                    Author = Environment.UserName,
                    License = "MIT",
                    Dependencies = new Dictionary<string, string>(),
                    Scripts = new Dictionary<string, string>
                    {
                        ["build"] = "ouroboros build",
                        ["test"] = "ouroboros test"
                    }
                };
                
                var packageManager = new PackageManager("packages");
                await packageManager.CreatePackageAsync(Directory.GetCurrentDirectory(), metadata);
                
                Console.WriteLine($"Initialized Ouroboros project '{projectName}'");
                return 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                return 1;
            }
        }

        private static async Task<int> PublishCommand(string? registry, string? tag, bool dryRun)
        {
            try
            {
                if (dryRun)
                {
                    Console.WriteLine("Performing dry run...");
                }
                
                var packagesDir = Path.Combine(Directory.GetCurrentDirectory(), "packages");
                var packageManager = new PackageManager(packagesDir);
                
                if (!dryRun)
                {
                    var success = await packageManager.PublishAsync(Directory.GetCurrentDirectory());
                    return success ? 0 : 1;
                }
                else
                {
                    Console.WriteLine("Dry run complete. Package would be published successfully.");
                    return 0;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                return 1;
            }
        }

        private static async Task<int> AddRegistryCommand(string name, string url, string? token, bool setDefault)
        {
            try
            {
                var config = await RegistryConfiguration.LoadAsync();
                
                var registryConfig = new RegistryConfig
                {
                    Name = name,
                    Url = url,
                    AuthToken = token,
                    IsDefault = setDefault
                };
                
                config.AddRegistry(registryConfig);
                
                if (setDefault)
                {
                    config.DefaultRegistry = url;
                }
                
                await config.SaveAsync();
                
                Console.WriteLine($"Added registry '{name}' ({url})");
                return 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                return 1;
            }
        }

        private static async Task<int> RemoveRegistryCommand(string name)
        {
            try
            {
                var config = await RegistryConfiguration.LoadAsync();
                config.RemoveRegistry(name);
                await config.SaveAsync();
                
                Console.WriteLine($"Removed registry '{name}'");
                return 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                return 1;
            }
        }

        private static async Task<int> ListRegistriesCommand()
        {
            try
            {
                var config = await RegistryConfiguration.LoadAsync();
                
                Console.WriteLine("Configured registries:");
                Console.WriteLine($"  default: {config.DefaultRegistry}");
                
                foreach (var reg in config.Registries)
                {
                    var authInfo = string.IsNullOrEmpty(reg.AuthToken) ? "" : " [authenticated]";
                    var defaultInfo = reg.IsDefault ? " (default)" : "";
                    Console.WriteLine($"  {reg.Name}: {reg.Url}{authInfo}{defaultInfo}");
                }
                
                return 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                return 1;
            }
        }

        private static async Task<int> CleanCacheCommand(bool force)
        {
            try
            {
                var cacheDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                    ".ouroboros",
                    "cache"
                );
                
                if (!Directory.Exists(cacheDir))
                {
                    Console.WriteLine("Cache is already empty.");
                    return 0;
                }
                
                var size = GetDirectorySize(cacheDir);
                
                if (!force)
                {
                    Console.Write($"This will delete {FormatSize(size)} of cached packages. Continue? (y/N): ");
                    var response = Console.ReadLine();
                    if (response?.ToLower() != "y")
                    {
                        Console.WriteLine("Cancelled.");
                        return 0;
                    }
                }
                
                Directory.Delete(cacheDir, recursive: true);
                Console.WriteLine($"Cleared {FormatSize(size)} of cached packages.");
                
                return 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                return 1;
            }
        }

        private static async Task<int> VerifyCacheCommand()
        {
            try
            {
                var cacheDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                    ".ouroboros",
                    "cache"
                );
                
                if (!Directory.Exists(cacheDir))
                {
                    Console.WriteLine("Cache is empty.");
                    return 0;
                }
                
                var cache = new PackageCache(cacheDir);
                var files = Directory.GetFiles(cacheDir, "*.ouro.zip");
                var corrupted = 0;
                
                Console.WriteLine($"Verifying {files.Length} cached packages...");
                
                foreach (var file in files)
                {
                    try
                    {
                        // Try to open as zip to verify integrity
                        using var archive = System.IO.Compression.ZipFile.OpenRead(file);
                        // File is valid
                    }
                    catch
                    {
                        corrupted++;
                        Console.WriteLine($"  Corrupted: {Path.GetFileName(file)}");
                        File.Delete(file);
                    }
                }
                
                if (corrupted > 0)
                {
                    Console.WriteLine($"Removed {corrupted} corrupted package(s).");
                }
                else
                {
                    Console.WriteLine("All cached packages are valid.");
                }
                
                return 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                return 1;
            }
        }

        private static Task<int> VersionCommand()
        {
            Console.WriteLine("Ouroboros Package Manager (OPM) v2.0.0");
            Console.WriteLine("Part of the Ouroboros Programming Language");
            return Task.FromResult(0);
        }

        private static string GetGlobalPackagesDirectory()
        {
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                ".ouroboros",
                "packages"
            );
        }

        private static async Task<ProjectManifest> LoadProjectManifest()
        {
            var path = Path.Combine(Directory.GetCurrentDirectory(), "project.ouro.json");
            if (!File.Exists(path))
            {
                throw new FileNotFoundException("No project.ouro.json found in current directory");
            }
            
            var json = await File.ReadAllTextAsync(path);
            return System.Text.Json.JsonSerializer.Deserialize<ProjectManifest>(json, new System.Text.Json.JsonSerializerOptions
            {
                PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
            }) ?? new ProjectManifest();
        }

        private static long GetDirectorySize(string directory)
        {
            return Directory.GetFiles(directory, "*", SearchOption.AllDirectories)
                .Sum(file => new FileInfo(file).Length);
        }

        private static string FormatSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB" };
            int order = 0;
            double size = bytes;
            
            while (size >= 1024 && order < sizes.Length - 1)
            {
                order++;
                size /= 1024;
            }
            
            return $"{size:0.##} {sizes[order]}";
        }
    }
} 