using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Ouro.Tools.Opm
{
    /// <summary>
    /// Private package registry support
    /// </summary>
    public class PrivateRegistry : PackageRegistry
    {
        private readonly string authToken;
        private readonly bool requiresAuth;
        private readonly Dictionary<string, string> customHeaders;

        public PrivateRegistry(string registryUrl, string? authToken = null, Dictionary<string, string>? customHeaders = null) 
            : base(registryUrl)
        {
            this.authToken = authToken ?? "";
            this.requiresAuth = !string.IsNullOrEmpty(authToken);
            this.customHeaders = customHeaders ?? new Dictionary<string, string>();
            
            if (requiresAuth)
            {
                ConfigureAuthentication();
            }
        }

        private void ConfigureAuthentication()
        {
            httpClient.DefaultRequestHeaders.Authorization = 
                new AuthenticationHeaderValue("Bearer", authToken);
                
            foreach (var header in customHeaders)
            {
                httpClient.DefaultRequestHeaders.Add(header.Key, header.Value);
            }
        }

        public override async Task<PackageInfo?> GetPackageAsync(string name, string? version = null)
        {
            if (!await ValidateAccessAsync())
            {
                throw new UnauthorizedAccessException("Invalid authentication for private registry");
            }
            
            return await base.GetPackageAsync(name, version);
        }

        public override async Task<bool> PublishAsync(string archivePath, PackageMetadata metadata, string checksum)
        {
            if (!await ValidateAccessAsync())
            {
                throw new UnauthorizedAccessException("Invalid authentication for private registry");
            }
            
            // Add additional metadata for private packages
            metadata.Private = true;
            metadata.Registry = registryUrl;
            
            return await base.PublishAsync(archivePath, metadata, checksum);
        }

        private async Task<bool> ValidateAccessAsync()
        {
            if (!requiresAuth)
                return true;
                
            try
            {
                var response = await httpClient.GetAsync($"{registryUrl}/api/auth/validate");
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        public async Task<List<string>> GetAuthorizedScopesAsync()
        {
            try
            {
                var response = await httpClient.GetAsync($"{registryUrl}/api/auth/scopes");
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    return JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();
                }
            }
            catch
            {
                // Ignore errors
            }
            
            return new List<string>();
        }
    }

    /// <summary>
    /// Registry configuration for multiple registries
    /// </summary>
    public class RegistryConfiguration
    {
        public List<RegistryConfig> Registries { get; set; } = new();
        public string DefaultRegistry { get; set; } = "https://packages.ouroboros-lang.org";
        
        public static async Task<RegistryConfiguration> LoadAsync()
        {
            var configPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                ".ouroboros",
                "registries.json"
            );
            
            if (!File.Exists(configPath))
            {
                return new RegistryConfiguration();
            }
            
            var json = await File.ReadAllTextAsync(configPath);
            return JsonSerializer.Deserialize<RegistryConfiguration>(json, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            }) ?? new RegistryConfiguration();
        }
        
        public async Task SaveAsync()
        {
            var configDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                ".ouroboros"
            );
            
            Directory.CreateDirectory(configDir);
            
            var configPath = Path.Combine(configDir, "registries.json");
            var json = JsonSerializer.Serialize(this, new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
            
            await File.WriteAllTextAsync(configPath, json);
        }
        
        public RegistryConfig? GetRegistry(string name)
        {
            return Registries.FirstOrDefault(r => r.Name == name);
        }
        
        public void AddRegistry(RegistryConfig registry)
        {
            RemoveRegistry(registry.Name);
            Registries.Add(registry);
        }
        
        public void RemoveRegistry(string name)
        {
            Registries.RemoveAll(r => r.Name == name);
        }
    }

    public class RegistryConfig
    {
        public string Name { get; set; } = "";
        public string Url { get; set; } = "";
        public string? AuthToken { get; set; }
        public Dictionary<string, string>? Headers { get; set; }
        public List<string>? Scopes { get; set; }
        public bool IsDefault { get; set; }
    }

    /// <summary>
    /// Enhanced package manager with multi-registry support
    /// </summary>
    public class MultiRegistryPackageManager : PackageManager
    {
        private readonly Dictionary<string, PackageRegistry> registries = new();
        private readonly RegistryConfiguration configuration;
        
        public MultiRegistryPackageManager(string packagesDirectory, RegistryConfiguration? configuration = null) 
            : base(packagesDirectory)
        {
            this.configuration = configuration ?? new RegistryConfiguration();
            InitializeRegistries();
        }
        
        private void InitializeRegistries()
        {
            // Add default registry
            registries["default"] = new PackageRegistry(configuration.DefaultRegistry);
            
            // Add configured registries
            foreach (var config in configuration.Registries)
            {
                if (string.IsNullOrEmpty(config.AuthToken))
                {
                    registries[config.Name] = new PackageRegistry(config.Url);
                }
                else
                {
                    registries[config.Name] = new PrivateRegistry(config.Url, config.AuthToken, config.Headers);
                }
            }
        }
        
        public async Task<InstallResult> InstallFromRegistryAsync(string packageName, string registryName, string? version = null)
        {
            if (!registries.ContainsKey(registryName))
            {
                return new InstallResult
                {
                    Success = false,
                    Error = $"Unknown registry: {registryName}"
                };
            }
            
            var registry = registries[registryName];
            var package = await registry.GetPackageAsync(packageName, version);
            
            if (package == null)
            {
                return new InstallResult
                {
                    Success = false,
                    Error = $"Package '{packageName}' not found in registry '{registryName}'"
                };
            }
            
            // Continue with normal installation
            return await base.InstallAsync(packageName, version);
        }
        
        public async Task<List<PackageInfo>> SearchAllRegistriesAsync(string query)
        {
            var allResults = new List<PackageInfo>();
            
            foreach (var registry in registries.Values)
            {
                try
                {
                    var results = await registry.SearchAsync(query);
                    allResults.AddRange(results);
                }
                catch
                {
                    // Continue with other registries
                }
            }
            
            // Remove duplicates, keeping the one from the preferred registry
            var uniqueResults = allResults
                .GroupBy(p => p.Name)
                .Select(g => g.First())
                .ToList();
                
            return uniqueResults;
        }
    }
} 