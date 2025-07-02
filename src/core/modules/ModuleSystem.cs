using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Ouro.Core.AST;
using Ouro.Core.Compiler;

namespace Ouro.Core.Modules
{
    /// <summary>
    /// Module system for Ouro
    /// </summary>
    public class ModuleSystem
    {
        private readonly Dictionary<string, LoadedModule> loadedModules = new();
        private readonly List<string> searchPaths = new();
        private readonly ModuleResolver resolver;
        private readonly ModuleCache cache;

        public ModuleSystem()
        {
            resolver = new ModuleResolver();
            cache = new ModuleCache();
            
            // Add default search paths
            searchPaths.Add(Environment.CurrentDirectory);
            searchPaths.Add(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "stdlib"));
            
            var envPath = Environment.GetEnvironmentVariable("OURO_PATH");
            if (!string.IsNullOrEmpty(envPath))
            {
                searchPaths.AddRange(envPath.Split(Path.PathSeparator));
            }
        }

        /// <summary>
        /// Import a module
        /// </summary>
        public async Task<Module> ImportModuleAsync(string modulePath, ImportOptions? options = null)
        {
            options ??= new ImportOptions();
            
            // Check if already loaded
            var canonicalPath = resolver.ResolveModulePath(modulePath, searchPaths);
            if (loadedModules.TryGetValue(canonicalPath, out var loaded))
            {
                return loaded.Module;
            }

            // Check cache
            if (cache.TryGetModule(canonicalPath, out var cached))
            {
                loadedModules[canonicalPath] = cached;
                return cached.Module;
            }

            // Load and compile module
            var module = await LoadAndCompileModuleAsync(canonicalPath, options);
            
            // Process dependencies
            await ProcessDependenciesAsync(module);
            
            // Cache and register
            var loadedModule = new LoadedModule
            {
                Path = canonicalPath,
                Module = module,
                LoadTime = DateTime.UtcNow
            };
            
            loadedModules[canonicalPath] = loadedModule;
            cache.AddModule(canonicalPath, loadedModule);
            
            return module;
        }

        /// <summary>
        /// Export symbols from a module
        /// </summary>
        public void ExportSymbols(Module module, ExportDeclaration export)
        {
            foreach (var symbol in export.Symbols)
            {
                if (export.IsDefault)
                {
                    module.DefaultExport = symbol;
                }
                else
                {
                    module.Exports[symbol.Name] = symbol;
                }
            }
        }

        /// <summary>
        /// Resolve import statements
        /// </summary>
        public async Task<ImportResolution> ResolveImportAsync(ImportDeclaration import)
        {
            var module = await ImportModuleAsync(import.ModulePath);
            var resolution = new ImportResolution
            {
                Module = module,
                Import = import
            };

            if (import.IsWildcard)
            {
                // import * as name from 'module'
                resolution.ImportedSymbols = module.Exports;
            }
            else if (import.Symbols.Any())
            {
                // import { a, b, c } from 'module'
                resolution.ImportedSymbols = new Dictionary<string, Symbol>();
                foreach (var symbolName in import.Symbols)
                {
                    if (module.Exports.TryGetValue(symbolName.SourceName, out var symbol))
                    {
                        resolution.ImportedSymbols[symbolName.LocalName] = symbol;
                    }
                    else
                    {
                        throw new ModuleException($"Module '{module.Name}' does not export '{symbolName.SourceName}'");
                    }
                }
            }
            else if (import.IsDefault)
            {
                // import name from 'module'
                if (module.DefaultExport == null)
                {
                    throw new ModuleException($"Module '{module.Name}' has no default export");
                }
                resolution.DefaultImport = module.DefaultExport;
            }

            return resolution;
        }

        private async Task<Module> LoadAndCompileModuleAsync(string path, ImportOptions options)
        {
            var source = await File.ReadAllTextAsync(path);
            var lexer = new Lexer.Lexer(source, path);
            var tokens = lexer.ScanTokens();
            
            var parser = new Parser.Parser(tokens);
            var ast = parser.Parse();
            
            var module = new Module
            {
                Name = Path.GetFileNameWithoutExtension(path),
                Path = path,
                AST = ast,
                Exports = new Dictionary<string, Symbol>()
            };

            // Extract exports - need to handle this differently since Program structure is different
            // For now, we'll skip automatic export extraction
            
            return module;
        }

        private async Task ProcessDependenciesAsync(Module module)
        {
            // Process import declarations from the AST
            // Note: This would need to be integrated with the actual parser
            // to properly extract import declarations from the AST
            await Task.CompletedTask;
        }

        /// <summary>
        /// Add a search path for modules
        /// </summary>
        public void AddSearchPath(string path)
        {
            if (!searchPaths.Contains(path))
            {
                searchPaths.Add(path);
            }
        }

        /// <summary>
        /// Clear module cache
        /// </summary>
        public void ClearCache()
        {
            cache.Clear();
            loadedModules.Clear();
        }
    }

    /// <summary>
    /// Module representation
    /// </summary>
    public class Module
    {
        public string Name { get; set; } = "";
        public string Path { get; set; } = "";
        public AST.Program AST { get; set; } = new(new List<Statement>());
        public Dictionary<string, Symbol> Exports { get; set; } = new();
        public Symbol? DefaultExport { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Loaded module information
    /// </summary>
    public class LoadedModule
    {
        public string Path { get; set; } = "";
        public Module Module { get; set; } = new();
        public DateTime LoadTime { get; set; }
    }

    /// <summary>
    /// Symbol exported from a module
    /// </summary>
    public class Symbol
    {
        public string Name { get; set; } = "";
        public SymbolKind Kind { get; set; }
        public object? Value { get; set; }
        public TypeNode? Type { get; set; }
        public AstNode? Declaration { get; set; }
    }

    /// <summary>
    /// Symbol kind
    /// </summary>
    public enum SymbolKind
    {
        Variable,
        Function,
        Class,
        Interface,
        Enum,
        Type,
        Namespace,
        Constant
    }

    /// <summary>
    /// Import options
    /// </summary>
    public class ImportOptions
    {
        public bool Lazy { get; set; } = false;
        public bool Cache { get; set; } = true;
        public bool ResolveExtensions { get; set; } = true;
        public string[] Extensions { get; set; } = { ".ouro", ".ou" };
    }

    /// <summary>
    /// Import resolution result
    /// </summary>
    public class ImportResolution
    {
        public Module Module { get; set; } = new();
        public ImportDeclaration Import { get; set; } = new();
        public Dictionary<string, Symbol> ImportedSymbols { get; set; } = new();
        public Symbol? DefaultImport { get; set; }
    }

    /// <summary>
    /// Module resolver
    /// </summary>
    public class ModuleResolver
    {
        /// <summary>
        /// Resolve module path
        /// </summary>
        public string ResolveModulePath(string modulePath, List<string> searchPaths)
        {
            // Absolute path
            if (Path.IsPathRooted(modulePath))
            {
                if (File.Exists(modulePath))
                    return Path.GetFullPath(modulePath);
                    
                // Try with extensions
                var extensions = new[] { ".ouro", ".ou" };
                foreach (var ext in extensions)
                {
                    var pathWithExt = modulePath + ext;
                    if (File.Exists(pathWithExt))
                        return Path.GetFullPath(pathWithExt);
                }
                
                throw new ModuleException($"Module not found: {modulePath}");
            }

            // Relative path starting with ./ or ../
            if (modulePath.StartsWith("./") || modulePath.StartsWith("../"))
            {
                var currentDir = Directory.GetCurrentDirectory();
                var resolved = Path.Combine(currentDir, modulePath);
                
                if (File.Exists(resolved))
                    return Path.GetFullPath(resolved);
                    
                // Try with extensions
                var extensions = new[] { ".ouro", ".ou" };
                foreach (var ext in extensions)
                {
                    var pathWithExt = resolved + ext;
                    if (File.Exists(pathWithExt))
                        return Path.GetFullPath(pathWithExt);
                }
            }

            // Search in paths
            foreach (var searchPath in searchPaths)
            {
                var candidate = Path.Combine(searchPath, modulePath);
                
                // Try exact path
                if (File.Exists(candidate))
                    return Path.GetFullPath(candidate);
                    
                // Try with extensions
                var extensions = new[] { ".ouro", ".ou" };
                foreach (var ext in extensions)
                {
                    var pathWithExt = candidate + ext;
                    if (File.Exists(pathWithExt))
                        return Path.GetFullPath(pathWithExt);
                }
                
                // Try as directory with index file
                if (Directory.Exists(candidate))
                {
                    var indexPath = Path.Combine(candidate, "index.ouro");
                    if (File.Exists(indexPath))
                        return Path.GetFullPath(indexPath);
                        
                    indexPath = Path.Combine(candidate, "index.ou");
                    if (File.Exists(indexPath))
                        return Path.GetFullPath(indexPath);
                }
            }

            throw new ModuleException($"Cannot resolve module: {modulePath}");
        }
    }

    /// <summary>
    /// Module cache
    /// </summary>
    public class ModuleCache
    {
        private readonly Dictionary<string, LoadedModule> cache = new();

        public bool TryGetModule(string path, out LoadedModule module)
        {
            return cache.TryGetValue(path, out module);
        }

        public void AddModule(string path, LoadedModule module)
        {
            cache[path] = module;
        }

        public void Clear()
        {
            cache.Clear();
        }
    }

    /// <summary>
    /// Module exception
    /// </summary>
    public class ModuleException : Exception
    {
        public ModuleException(string message) : base(message) { }
        public ModuleException(string message, Exception inner) : base(message, inner) { }
    }

    /// <summary>
    /// Import declaration AST node
    /// </summary>
    public class ImportDeclaration : Statement
    {
        public string ModulePath { get; set; } = "";
        public List<ImportSymbol> Symbols { get; set; } = new();
        public bool IsWildcard { get; set; }
        public bool IsDefault { get; set; }
        public string? Alias { get; set; }

        public override T Accept<T>(IAstVisitor<T> visitor)
        {
            // For now, return default value
            return default(T)!;
        }
    }

    /// <summary>
    /// Import symbol
    /// </summary>
    public class ImportSymbol
    {
        public string SourceName { get; set; } = "";
        public string LocalName { get; set; } = "";
    }

    /// <summary>
    /// Export declaration AST node
    /// </summary>
    public class ExportDeclaration : Statement
    {
        public List<Symbol> Symbols { get; set; } = new();
        public bool IsDefault { get; set; }
        public AstNode? Declaration { get; set; }

        public override T Accept<T>(IAstVisitor<T> visitor)
        {
            // For now, return default value
            return default(T)!;
        }
    }
} 