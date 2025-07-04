using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Ouro.Core.AST;
using Ouro.Core.Parser;
using Ouro.Core.Lexer;

namespace Ouro.Tools.DocGen
{
    /// <summary>
    /// Documentation generator for Ouroboros code
    /// </summary>
    public class DocumentationGenerator
    {
        private readonly DocGeneratorOptions options;
        private readonly Dictionary<string, DocumentedItem> documentedItems = new();
        private readonly List<string> errors = new();

        public DocumentationGenerator(DocGeneratorOptions options)
        {
            this.options = options;
        }

        /// <summary>
        /// Generate documentation for source files
        /// </summary>
        public async Task GenerateAsync()
        {
            Console.WriteLine($"Generating documentation for {options.SourcePaths.Count} source paths...");

            // Collect all source files
            var sourceFiles = CollectSourceFiles();
            Console.WriteLine($"Found {sourceFiles.Count} source files");

            // Parse and extract documentation
            foreach (var file in sourceFiles)
            {
                await ProcessSourceFileAsync(file);
            }

            // Generate output
            switch (options.OutputFormat)
            {
                case OutputFormat.Html:
                    await GenerateHtmlAsync();
                    break;
                case OutputFormat.Markdown:
                    await GenerateMarkdownAsync();
                    break;
                case OutputFormat.Json:
                    await GenerateJsonAsync();
                    break;
            }

            // Report errors
            if (errors.Any())
            {
                Console.WriteLine($"\n{errors.Count} errors occurred:");
                foreach (var error in errors)
                {
                    Console.WriteLine($"  - {error}");
                }
            }

            Console.WriteLine("\nDocumentation generation complete!");
        }

        private List<string> CollectSourceFiles()
        {
            var files = new List<string>();

            foreach (var path in options.SourcePaths)
            {
                if (File.Exists(path))
                {
                    files.Add(path);
                }
                else if (Directory.Exists(path))
                {
                    var pattern = options.IncludePattern ?? "*.ouro";
                    files.AddRange(Directory.GetFiles(path, pattern, SearchOption.AllDirectories));
                }
            }

            // Apply exclusions
            if (options.ExcludePatterns.Any())
            {
                files = files.Where(f => !options.ExcludePatterns.Any(p => f.Contains(p))).ToList();
            }

            return files;
        }

        private async Task ProcessSourceFileAsync(string filePath)
        {
            try
            {
                var source = await File.ReadAllTextAsync(filePath);
                var lexer = new Lexer(source, filePath);
                var tokens = lexer.ScanTokens();
                
                // Skip error checking for now - would need to check token errors
                
                var parser = new Parser(tokens);
                var ast = parser.Parse();
                
                // Skip error checking for now - would need to check parse errors

                // Extract documentation from AST
                var extractor = new DocumentationExtractor(filePath);
                extractor.ExtractFromAst(ast);
                
                foreach (var item in extractor.Items)
                {
                    documentedItems[item.FullName] = item;
                }
            }
            catch (Exception ex)
            {
                errors.Add($"Error processing {filePath}: {ex.Message}");
            }
        }

        private async Task GenerateHtmlAsync()
        {
            var outputDir = options.OutputPath ?? "docs";
            Directory.CreateDirectory(outputDir);

            // Generate index
            var indexHtml = GenerateHtmlIndex();
            await File.WriteAllTextAsync(Path.Combine(outputDir, "index.html"), indexHtml);

            // Generate pages for each namespace
            var namespaces = documentedItems.Values
                .GroupBy(i => i.Namespace)
                .OrderBy(g => g.Key);

            foreach (var ns in namespaces)
            {
                var nsDir = Path.Combine(outputDir, ns.Key.Replace('.', '/'));
                Directory.CreateDirectory(nsDir);

                var nsHtml = GenerateNamespaceHtml(ns.Key, ns.ToList());
                await File.WriteAllTextAsync(Path.Combine(nsDir, "index.html"), nsHtml);

                // Generate individual item pages
                foreach (var item in ns)
                {
                    var itemHtml = GenerateItemHtml(item);
                    await File.WriteAllTextAsync(Path.Combine(nsDir, $"{item.Name}.html"), itemHtml);
                }
            }

            // Copy CSS and JavaScript
            await GenerateAssetsAsync(outputDir);
        }

        private string GenerateHtmlIndex()
        {
            var html = new StringBuilder();
            
            html.AppendLine("<!DOCTYPE html>");
            html.AppendLine("<html lang=\"en\">");
            html.AppendLine("<head>");
            html.AppendLine("    <meta charset=\"UTF-8\">");
            html.AppendLine("    <meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\">");
            html.AppendLine($"    <title>{options.ProjectName} Documentation</title>");
            html.AppendLine("    <link rel=\"stylesheet\" href=\"assets/style.css\">");
            html.AppendLine("</head>");
            html.AppendLine("<body>");
            html.AppendLine("    <header>");
            html.AppendLine($"        <h1>{options.ProjectName} Documentation</h1>");
            html.AppendLine("    </header>");
            html.AppendLine("    <nav>");
            html.AppendLine("        <h2>Namespaces</h2>");
            html.AppendLine("        <ul>");

            var namespaces = documentedItems.Values
                .Select(i => i.Namespace)
                .Distinct()
                .OrderBy(n => n);

            foreach (var ns in namespaces)
            {
                var nsPath = ns.Replace('.', '/');
                html.AppendLine($"            <li><a href=\"{nsPath}/index.html\">{ns}</a></li>");
            }

            html.AppendLine("        </ul>");
            html.AppendLine("    </nav>");
            html.AppendLine("    <main>");
            html.AppendLine($"        <h2>Welcome to {options.ProjectName} Documentation</h2>");
            html.AppendLine($"        <p>This documentation was generated on {DateTime.Now:yyyy-MM-dd HH:mm:ss}</p>");
            
            // Statistics
            var stats = GenerateStatistics();
            html.AppendLine("        <h3>Statistics</h3>");
            html.AppendLine("        <ul>");
            foreach (var stat in stats)
            {
                html.AppendLine($"            <li>{stat.Key}: {stat.Value}</li>");
            }
            html.AppendLine("        </ul>");
            
            html.AppendLine("    </main>");
            html.AppendLine("</body>");
            html.AppendLine("</html>");
            
            return html.ToString();
        }

        private string GenerateNamespaceHtml(string namespaceName, List<DocumentedItem> items)
        {
            var html = new StringBuilder();
            
            html.AppendLine("<!DOCTYPE html>");
            html.AppendLine("<html lang=\"en\">");
            html.AppendLine("<head>");
            html.AppendLine("    <meta charset=\"UTF-8\">");
            html.AppendLine($"    <title>{namespaceName} - {options.ProjectName}</title>");
            html.AppendLine("    <link rel=\"stylesheet\" href=\"../assets/style.css\">");
            html.AppendLine("</head>");
            html.AppendLine("<body>");
            html.AppendLine($"    <h1>Namespace: {namespaceName}</h1>");
            
            // Group by type
            var groups = items.GroupBy(i => i.ItemType).OrderBy(g => g.Key);
            
            foreach (var group in groups)
            {
                html.AppendLine($"    <h2>{group.Key}s</h2>");
                html.AppendLine("    <ul>");
                
                foreach (var item in group.OrderBy(i => i.Name))
                {
                    html.AppendLine($"        <li>");
                    html.AppendLine($"            <a href=\"{item.Name}.html\">{item.Name}</a>");
                    if (!string.IsNullOrEmpty(item.Summary))
                    {
                        html.AppendLine($"            <p>{item.Summary}</p>");
                    }
                    html.AppendLine($"        </li>");
                }
                
                html.AppendLine("    </ul>");
            }
            
            html.AppendLine("</body>");
            html.AppendLine("</html>");
            
            return html.ToString();
        }

        private string GenerateItemHtml(DocumentedItem item)
        {
            var html = new StringBuilder();
            
            html.AppendLine("<!DOCTYPE html>");
            html.AppendLine("<html lang=\"en\">");
            html.AppendLine("<head>");
            html.AppendLine("    <meta charset=\"UTF-8\">");
            html.AppendLine($"    <title>{item.Name} - {options.ProjectName}</title>");
            html.AppendLine("    <link rel=\"stylesheet\" href=\"../assets/style.css\">");
            html.AppendLine("    <link rel=\"stylesheet\" href=\"../assets/highlight.css\">");
            html.AppendLine("</head>");
            html.AppendLine("<body>");
            html.AppendLine($"    <h1>{item.ItemType}: {item.Name}</h1>");
            
            if (!string.IsNullOrEmpty(item.Summary))
            {
                html.AppendLine($"    <div class=\"summary\">{item.Summary}</div>");
            }
            
            if (!string.IsNullOrEmpty(item.Remarks))
            {
                html.AppendLine("    <h2>Remarks</h2>");
                html.AppendLine($"    <div class=\"remarks\">{item.Remarks}</div>");
            }
            
            // Signature
            if (!string.IsNullOrEmpty(item.Signature))
            {
                html.AppendLine("    <h2>Signature</h2>");
                html.AppendLine($"    <pre><code class=\"language-ouroboros\">{item.Signature}</code></pre>");
            }
            
            // Parameters
            if (item.Parameters.Any())
            {
                html.AppendLine("    <h2>Parameters</h2>");
                html.AppendLine("    <dl>");
                foreach (var param in item.Parameters)
                {
                    html.AppendLine($"        <dt>{param.Name} : {param.Type}</dt>");
                    html.AppendLine($"        <dd>{param.Description}</dd>");
                }
                html.AppendLine("    </dl>");
            }
            
            // Return value
            if (item.ReturnValue != null)
            {
                html.AppendLine("    <h2>Returns</h2>");
                html.AppendLine($"    <p>{item.ReturnValue.Type}: {item.ReturnValue.Description}</p>");
            }
            
            // Examples
            if (item.Examples.Any())
            {
                html.AppendLine("    <h2>Examples</h2>");
                foreach (var example in item.Examples)
                {
                    html.AppendLine($"    <h3>{example.Title}</h3>");
                    html.AppendLine($"    <pre><code class=\"language-ouroboros\">{example.Code}</code></pre>");
                }
            }
            
            // Generate JavaScript for code highlighting
            html.AppendLine(@"
<script>
// Syntax highlighting for Ouroboros code blocks
document.addEventListener('DOMContentLoaded', function() {
    // Find all code blocks
    const codeBlocks = document.querySelectorAll('pre code');
    
    codeBlocks.forEach(block => {
        // Apply basic syntax highlighting
        let code = block.innerHTML;
        
        // Keywords
        const keywords = ['function', 'class', 'interface', 'struct', 'enum', 'if', 'else', 'while', 'for', 
                         'return', 'break', 'continue', 'switch', 'case', 'default', 'new', 'typeof', 
                         'var', 'let', 'const', 'public', 'private', 'protected', 'static', 'virtual',
                         'override', 'abstract', 'sealed', 'async', 'await', 'try', 'catch', 'finally',
                         'throw', 'import', 'export', 'namespace', 'using'];
        
        keywords.forEach(keyword => {
            const regex = new RegExp('\\b' + keyword + '\\b', 'g');
            code = code.replace(regex, '<span class=""keyword"">' + keyword + '</span>');
        });
        
        // Types
        const types = ['int', 'float', 'double', 'string', 'bool', 'void', 'any', 'object'];
        types.forEach(type => {
            const regex = new RegExp('\\b' + type + '\\b', 'g');
            code = code.replace(regex, '<span class=""type"">' + type + '</span>');
        });
        
        // Strings (simple regex, doesn't handle escapes properly)
        code = code.replace(/(""[^""]*"")/g, '<span class=""string"">$1</span>');
        code = code.replace(/('[^']*')/g, '<span class=""string"">$1</span>');
        
        // Comments
        code = code.replace(/(\/\/[^\n]*)/g, '<span class=""comment"">$1</span>');
        code = code.replace(/(\/\*[\s\S]*?\*\/)/g, '<span class=""comment"">$1</span>');
        
        // Numbers
        code = code.replace(/\b(\d+(\.\d+)?)\b/g, '<span class=""number"">$1</span>');
        
        // Update the block
        block.innerHTML = code;
    });
    
    // Add line numbers
    document.querySelectorAll('pre.line-numbers code').forEach(block => {
        const lines = block.innerHTML.split('\n');
        const numberedLines = lines.map((line, index) => 
            `<span class=""line-number"">${index + 1}</span>${line}`
        ).join('\n');
        block.innerHTML = numberedLines;
    });
});
</script>");
            
            html.AppendLine("</body>");
            html.AppendLine("</html>");
            
            return html.ToString();
        }

        private async Task GenerateMarkdownAsync()
        {
            var outputDir = options.OutputPath ?? "docs";
            Directory.CreateDirectory(outputDir);

            // Generate README
            var readme = GenerateMarkdownIndex();
            await File.WriteAllTextAsync(Path.Combine(outputDir, "README.md"), readme);

            // Generate namespace files
            var namespaces = documentedItems.Values
                .GroupBy(i => i.Namespace)
                .OrderBy(g => g.Key);

            foreach (var ns in namespaces)
            {
                var nsMarkdown = GenerateNamespaceMarkdown(ns.Key, ns.ToList());
                await File.WriteAllTextAsync(Path.Combine(outputDir, $"{ns.Key}.md"), nsMarkdown);
            }
        }

        private string GenerateMarkdownIndex()
        {
            var md = new StringBuilder();
            
            md.AppendLine($"# {options.ProjectName} Documentation");
            md.AppendLine();
            md.AppendLine($"Generated on {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            md.AppendLine();
            
            md.AppendLine("## Namespaces");
            md.AppendLine();
            
            var namespaces = documentedItems.Values
                .Select(i => i.Namespace)
                .Distinct()
                .OrderBy(n => n);

            foreach (var ns in namespaces)
            {
                var itemCount = documentedItems.Values.Count(i => i.Namespace == ns);
                md.AppendLine($"- [{ns}]({ns}.md) ({itemCount} items)");
            }
            
            md.AppendLine();
            md.AppendLine("## Statistics");
            md.AppendLine();
            
            var stats = GenerateStatistics();
            foreach (var stat in stats)
            {
                md.AppendLine($"- {stat.Key}: {stat.Value}");
            }
            
            return md.ToString();
        }

        private string GenerateNamespaceMarkdown(string namespaceName, List<DocumentedItem> items)
        {
            var md = new StringBuilder();
            
            md.AppendLine($"# Namespace: {namespaceName}");
            md.AppendLine();
            
            var groups = items.GroupBy(i => i.ItemType).OrderBy(g => g.Key);
            
            foreach (var group in groups)
            {
                md.AppendLine($"## {group.Key}s");
                md.AppendLine();
                
                foreach (var item in group.OrderBy(i => i.Name))
                {
                    md.AppendLine($"### {item.Name}");
                    md.AppendLine();
                    
                    if (!string.IsNullOrEmpty(item.Summary))
                    {
                        md.AppendLine(item.Summary);
                        md.AppendLine();
                    }
                    
                    if (!string.IsNullOrEmpty(item.Signature))
                    {
                        md.AppendLine("```ouroboros");
                        md.AppendLine(item.Signature);
                        md.AppendLine("```");
                        md.AppendLine();
                    }
                    
                    if (item.Parameters.Any())
                    {
                        md.AppendLine("**Parameters:**");
                        md.AppendLine();
                        foreach (var param in item.Parameters)
                        {
                            md.AppendLine($"- `{param.Name}` ({param.Type}): {param.Description}");
                        }
                        md.AppendLine();
                    }
                    
                    if (item.ReturnValue != null)
                    {
                        md.AppendLine($"**Returns:** {item.ReturnValue.Type} - {item.ReturnValue.Description}");
                        md.AppendLine();
                    }
                }
            }
            
            return md.ToString();
        }

        private async Task GenerateJsonAsync()
        {
            var outputPath = options.OutputPath ?? "docs/api.json";
            var directory = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var json = System.Text.Json.JsonSerializer.Serialize(documentedItems.Values, new System.Text.Json.JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
            });

            await File.WriteAllTextAsync(outputPath, json);
        }

        private async Task GenerateAssetsAsync(string outputDir)
        {
            var assetsDir = Path.Combine(outputDir, "assets");
            Directory.CreateDirectory(assetsDir);

            // Generate CSS
            var css = @"
body {
    font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif;
    line-height: 1.6;
    color: #333;
    max-width: 1200px;
    margin: 0 auto;
    padding: 20px;
}

h1, h2, h3 {
    color: #2c3e50;
}

pre {
    background: #f4f4f4;
    border: 1px solid #ddd;
    padding: 10px;
    overflow-x: auto;
}

code {
    background: #f4f4f4;
    padding: 2px 4px;
    border-radius: 3px;
}

.summary {
    font-style: italic;
    color: #666;
    margin: 10px 0;
}

.remarks {
    background: #f9f9f9;
    border-left: 4px solid #2196F3;
    padding: 10px;
    margin: 10px 0;
}

nav {
    background: #f0f0f0;
    padding: 15px;
    border-radius: 5px;
    margin-bottom: 20px;
}

nav ul {
    list-style: none;
    padding: 0;
}

nav li {
    margin: 5px 0;
}

a {
    color: #2196F3;
    text-decoration: none;
}

a:hover {
    text-decoration: underline;
}

dl dt {
    font-weight: bold;
    margin-top: 10px;
}

dl dd {
    margin-left: 20px;
    margin-bottom: 10px;
}
";
            await File.WriteAllTextAsync(Path.Combine(assetsDir, "style.css"), css);

            // Generate JavaScript for code highlighting
            var js = @"
// Ouroboros syntax highlighting
document.addEventListener('DOMContentLoaded', function() {
    highlightAllCode();
    console.log('Documentation loaded with syntax highlighting');
});

function highlightAllCode() {
    document.querySelectorAll('pre code').forEach(block => {
        highlightOuroborosCode(block);
    });
}

function highlightOuroborosCode(element) {
    let code = element.textContent;
    
    // Keywords
    code = code.replace(/\b(class|interface|struct|enum|function|if|else|while|for|return|break|continue|switch|case|default|new|typeof|var|let|const|public|private|protected|static|virtual|override|abstract|sealed|async|await|try|catch|finally|throw|import|export|namespace|using|@low|@medium|@high|@asm)\b/g, '<span class=""keyword"">$1</span>');
    
    // Types
    code = code.replace(/\b(int|float|double|string|bool|void|any|object|vector|matrix|quaternion|byte|short|long|decimal)\b/g, '<span class=""type"">$1</span>');
    
    // Strings
    code = code.replace(/(""[^""]*"")/g, '<span class=""string"">$1</span>');
    code = code.replace(/('[^']*')/g, '<span class=""string"">$1</span>');
    
    // Comments
    code = code.replace(/(\/\/[^\n]*)/g, '<span class=""comment"">$1</span>');
    code = code.replace(/(\/\*[\s\S]*?\*\/)/g, '<span class=""comment"">$1</span>');
    
    // Numbers
    code = code.replace(/\b(\d+(\.\d+)?)\b/g, '<span class=""number"">$1</span>');
    
    element.innerHTML = code;
}

// Add CSS for syntax highlighting
var style = document.createElement('style');
style.textContent = `
    .keyword { color: #0000ff; font-weight: bold; }
    .type { color: #2b91af; }
    .string { color: #a31515; }
    .comment { color: #008000; font-style: italic; }
    .number { color: #098658; }
`;
document.head.appendChild(style);
";
            await File.WriteAllTextAsync(Path.Combine(assetsDir, "script.js"), js);
        }

        private Dictionary<string, int> GenerateStatistics()
        {
            var stats = new Dictionary<string, int>
            {
                ["Total Items"] = documentedItems.Count,
                ["Namespaces"] = documentedItems.Values.Select(i => i.Namespace).Distinct().Count(),
                ["Classes"] = documentedItems.Values.Count(i => i.ItemType == ItemType.Class),
                ["Interfaces"] = documentedItems.Values.Count(i => i.ItemType == ItemType.Interface),
                ["Functions"] = documentedItems.Values.Count(i => i.ItemType == ItemType.Function),
                ["Enums"] = documentedItems.Values.Count(i => i.ItemType == ItemType.Enum),
                ["Properties"] = documentedItems.Values.Count(i => i.ItemType == ItemType.Property)
            };

            return stats;
        }
    }

    /// <summary>
    /// Documentation generator options
    /// </summary>
    public class DocGeneratorOptions
    {
        public List<string> SourcePaths { get; set; } = new();
        public string? OutputPath { get; set; }
        public OutputFormat OutputFormat { get; set; } = OutputFormat.Html;
        public string ProjectName { get; set; } = "Ouroboros";
        public string? IncludePattern { get; set; }
        public List<string> ExcludePatterns { get; set; } = new();
        public bool IncludePrivate { get; set; } = false;
        public bool IncludeInternal { get; set; } = false;
    }

    /// <summary>
    /// Output format
    /// </summary>
    public enum OutputFormat
    {
        Html,
        Markdown,
        Json
    }

    /// <summary>
    /// Documented item
    /// </summary>
    public class DocumentedItem
    {
        public string Name { get; set; } = "";
        public string FullName { get; set; } = "";
        public string Namespace { get; set; } = "";
        public ItemType ItemType { get; set; }
        public string Summary { get; set; } = "";
        public string Remarks { get; set; } = "";
        public string Signature { get; set; } = "";
        public List<Parameter> Parameters { get; set; } = new();
        public ReturnValue? ReturnValue { get; set; }
        public List<Example> Examples { get; set; } = new();
        public string SourceFile { get; set; } = "";
        public int SourceLine { get; set; }
    }

    /// <summary>
    /// Item type
    /// </summary>
    public enum ItemType
    {
        Class,
        Interface,
        Struct,
        Enum,
        Function,
        Property,
        Field,
        Event,
        Delegate,
        Namespace
    }

    /// <summary>
    /// Parameter documentation
    /// </summary>
    public class Parameter
    {
        public string Name { get; set; } = "";
        public string Type { get; set; } = "";
        public string Description { get; set; } = "";
    }

    /// <summary>
    /// Return value documentation
    /// </summary>
    public class ReturnValue
    {
        public string Type { get; set; } = "";
        public string Description { get; set; } = "";
    }

    /// <summary>
    /// Example documentation
    /// </summary>
    public class Example
    {
        public string Title { get; set; } = "";
        public string Code { get; set; } = "";
    }

    /// <summary>
    /// Documentation extractor from AST
    /// </summary>
    internal class DocumentationExtractor : IAstVisitor<object?>
    {
        private readonly string sourceFile;
        private readonly List<DocumentedItem> items = new();
        private string currentNamespace = "";

        public List<DocumentedItem> Items => items;

        public DocumentationExtractor(string sourceFile)
        {
            this.sourceFile = sourceFile;
        }

        public void ExtractFromAst(Core.AST.Program ast)
        {
            Visit(ast);
        }

        private object? Visit(AstNode node)
        {
            return node.Accept(this);
        }

        public object? VisitProgram(Core.AST.Program program)
        {
            foreach (var statement in program.Statements)
            {
                Visit(statement);
            }
            return null;
        }

        public object? VisitNamespaceDeclaration(NamespaceDeclaration decl)
        {
            var previousNamespace = currentNamespace;
            currentNamespace = decl.Name;
            
            foreach (var member in decl.Members)
            {
                Visit(member);
            }
            
            currentNamespace = previousNamespace;
            return null;
        }

        public object? VisitClassDeclaration(ClassDeclaration decl)
        {
            var item = new DocumentedItem
            {
                Name = decl.Name,
                FullName = string.IsNullOrEmpty(currentNamespace) ? decl.Name : $"{currentNamespace}.{decl.Name}",
                Namespace = currentNamespace,
                ItemType = ItemType.Class,
                SourceFile = sourceFile,
                SourceLine = decl.Line,
                Signature = BuildClassSignature(decl)
            };
            
            items.Add(item);
            
            // Process members
            foreach (var member in decl.Members)
            {
                Visit(member);
            }
            
            return null;
        }

        public object? VisitFunctionDeclaration(FunctionDeclaration decl)
        {
            var item = new DocumentedItem
            {
                Name = decl.Name,
                FullName = string.IsNullOrEmpty(currentNamespace) ? decl.Name : $"{currentNamespace}.{decl.Name}",
                Namespace = currentNamespace,
                ItemType = ItemType.Function,
                SourceFile = sourceFile,
                SourceLine = decl.Line,
                Signature = BuildFunctionSignature(decl)
            };
            
            // Extract parameters
            foreach (var param in decl.Parameters)
            {
                item.Parameters.Add(new Parameter
                {
                    Name = param.Name,
                    Type = param.Type.Name
                });
            }
            
            // Extract return type
            if (decl.ReturnType.Name != "void")
            {
                item.ReturnValue = new ReturnValue
                {
                    Type = decl.ReturnType.Name
                };
            }
            
            items.Add(item);
            return null;
        }

        private string BuildClassSignature(ClassDeclaration decl)
        {
            var sb = new StringBuilder();
            
            foreach (var modifier in decl.Modifiers)
            {
                sb.Append(modifier.ToString().ToLower()).Append(" ");
            }
            
            sb.Append("class ").Append(decl.Name);
            
            if (decl.TypeParameters.Any())
            {
                sb.Append("<");
                sb.Append(string.Join(", ", decl.TypeParameters.Select(tp => tp.Name)));
                sb.Append(">");
            }
            
            if (decl.BaseClass != null)
            {
                sb.Append(" : ").Append(decl.BaseClass.Name);
            }
            
            if (decl.Interfaces.Any())
            {
                if (decl.BaseClass == null)
                    sb.Append(" : ");
                else
                    sb.Append(", ");
                sb.Append(string.Join(", ", decl.Interfaces.Select(i => i.Name)));
            }
            
            return sb.ToString();
        }

        private string BuildFunctionSignature(FunctionDeclaration decl)
        {
            var sb = new StringBuilder();
            
            foreach (var modifier in decl.Modifiers)
            {
                sb.Append(modifier.ToString().ToLower()).Append(" ");
            }
            
            if (decl.IsAsync)
                sb.Append("async ");
            
            sb.Append(decl.ReturnType.Name).Append(" ");
            sb.Append(decl.Name);
            
            if (decl.TypeParameters.Any())
            {
                sb.Append("<");
                sb.Append(string.Join(", ", decl.TypeParameters.Select(tp => tp.Name)));
                sb.Append(">");
            }
            
            sb.Append("(");
            sb.Append(string.Join(", ", decl.Parameters.Select(p => $"{p.Type.Name} {p.Name}")));
            sb.Append(")");
            
            return sb.ToString();
        }

        // Implement other visitor methods...
        public object? VisitBinaryExpression(BinaryExpression expr) => null;
        public object? VisitUnaryExpression(UnaryExpression expr) => null;
        public object? VisitLiteralExpression(LiteralExpression expr) => null;
        public object? VisitIdentifierExpression(IdentifierExpression expr) => null;
        public object? VisitGenericIdentifierExpression(GenericIdentifierExpression expr) => null;
        public object? VisitAssignmentExpression(AssignmentExpression expr) => null;
        public object? VisitCallExpression(CallExpression expr) => null;
        public object? VisitMemberExpression(MemberExpression expr) => null;
        public object? VisitArrayExpression(ArrayExpression expr) => null;
        public object? VisitLambdaExpression(LambdaExpression expr) => null;
        public object? VisitConditionalExpression(ConditionalExpression expr) => null;
        public object? VisitNewExpression(NewExpression expr) => null;
        public object? VisitThisExpression(ThisExpression expr) => null;
        public object? VisitBaseExpression(BaseExpression expr) => null;
        public object? VisitTypeofExpression(TypeofExpression expr) => null;
        public object? VisitSizeofExpression(SizeofExpression expr) => null;
        public object? VisitNameofExpression(NameofExpression expr) => null;
        public object? VisitInterpolatedStringExpression(InterpolatedStringExpression expr) => null;
        public object? VisitMathExpression(MathExpression expr) => null;
        public object? VisitVectorExpression(VectorExpression expr) => null;
        public object? VisitMatrixExpression(MatrixExpression expr) => null;
        public object? VisitQuaternionExpression(QuaternionExpression expr) => null;
        public object? VisitIsExpression(IsExpression expr) => null;
        public object? VisitCastExpression(CastExpression expr) => null;
        public object? VisitMatchExpression(MatchExpression expr) => null;
        public object? VisitThrowExpression(ThrowExpression expr) => null;
        public object? VisitMatchArm(MatchArm arm) => null;
        public object? VisitStructLiteral(StructLiteral expr) => null;
        public object? VisitBlockStatement(BlockStatement stmt) => null;
        public object? VisitExpressionStatement(ExpressionStatement stmt) => null;
        public object? VisitVariableDeclaration(VariableDeclaration stmt) => null;
        public object? VisitIfStatement(IfStatement stmt) => null;
        public object? VisitWhileStatement(WhileStatement stmt) => null;
        public object? VisitForStatement(ForStatement stmt) => null;
        public object? VisitForEachStatement(ForEachStatement stmt) => null;
        public object? VisitRepeatStatement(RepeatStatement stmt) => null;
        public object? VisitIterateStatement(IterateStatement stmt) => null;
        public object? VisitParallelForStatement(ParallelForStatement stmt) => null;
        public object? VisitDoWhileStatement(DoWhileStatement stmt) => null;
        public object? VisitSwitchStatement(SwitchStatement stmt) => null;
        public object? VisitReturnStatement(ReturnStatement stmt) => null;
        public object? VisitBreakStatement(BreakStatement stmt) => null;
        public object? VisitContinueStatement(ContinueStatement stmt) => null;
        public object? VisitThrowStatement(ThrowStatement stmt) => null;
        public object? VisitTryStatement(TryStatement stmt) => null;
        public object? VisitUsingStatement(UsingStatement stmt) => null;
        public object? VisitLockStatement(LockStatement stmt) => null;
        public object? VisitUnsafeStatement(UnsafeStatement stmt) => null;
        public object? VisitFixedStatement(FixedStatement stmt) => null;
        public object? VisitYieldStatement(YieldStatement stmt) => null;
        public object? VisitMatchStatement(MatchStatement stmt) => null;
        public object? VisitAssemblyStatement(AssemblyStatement stmt) => null;
        public object? VisitInterfaceDeclaration(InterfaceDeclaration decl) => null;
        public object? VisitStructDeclaration(StructDeclaration decl) => null;
        public object? VisitEnumDeclaration(EnumDeclaration decl) => null;
        public object? VisitPropertyDeclaration(PropertyDeclaration decl) => null;
        public object? VisitFieldDeclaration(FieldDeclaration decl) => null;
        public object? VisitImportDeclaration(ImportDeclaration decl) => null;
        public object? VisitTypeAliasDeclaration(TypeAliasDeclaration decl) => null;
        public object? VisitComponentDeclaration(ComponentDeclaration decl) => null;
        public object? VisitSystemDeclaration(SystemDeclaration decl) => null;
        public object? VisitEntityDeclaration(EntityDeclaration decl) => null;
        public object? VisitDomainDeclaration(DomainDeclaration decl) => null;
        public object? VisitMacroDeclaration(MacroDeclaration decl) => null;
        public object? VisitTraitDeclaration(TraitDeclaration decl) => null;
        public object? VisitImplementDeclaration(ImplementDeclaration decl) => null;

        object? IAstVisitor<object?>.VisitBinaryExpression(BinaryExpression expr)
        {
            throw new NotImplementedException();
        }

        object? IAstVisitor<object?>.VisitUnaryExpression(UnaryExpression expr)
        {
            throw new NotImplementedException();
        }

        object? IAstVisitor<object?>.VisitLiteralExpression(LiteralExpression expr)
        {
            throw new NotImplementedException();
        }

        object? IAstVisitor<object?>.VisitIdentifierExpression(IdentifierExpression expr)
        {
            throw new NotImplementedException();
        }

        object? IAstVisitor<object?>.VisitGenericIdentifierExpression(GenericIdentifierExpression expr)
        {
            throw new NotImplementedException();
        }

        object? IAstVisitor<object?>.VisitAssignmentExpression(AssignmentExpression expr)
        {
            throw new NotImplementedException();
        }

        object? IAstVisitor<object?>.VisitCallExpression(CallExpression expr)
        {
            throw new NotImplementedException();
        }

        object? IAstVisitor<object?>.VisitMemberExpression(MemberExpression expr)
        {
            throw new NotImplementedException();
        }

        object? IAstVisitor<object?>.VisitArrayExpression(ArrayExpression expr)
        {
            throw new NotImplementedException();
        }

        object? IAstVisitor<object?>.VisitLambdaExpression(LambdaExpression expr)
        {
            throw new NotImplementedException();
        }

        object? IAstVisitor<object?>.VisitConditionalExpression(ConditionalExpression expr)
        {
            throw new NotImplementedException();
        }

        object? IAstVisitor<object?>.VisitNewExpression(NewExpression expr)
        {
            throw new NotImplementedException();
        }

        object? IAstVisitor<object?>.VisitThisExpression(ThisExpression expr)
        {
            throw new NotImplementedException();
        }

        object? IAstVisitor<object?>.VisitBaseExpression(BaseExpression expr)
        {
            throw new NotImplementedException();
        }

        object? IAstVisitor<object?>.VisitTypeofExpression(TypeofExpression expr)
        {
            throw new NotImplementedException();
        }

        object? IAstVisitor<object?>.VisitSizeofExpression(SizeofExpression expr)
        {
            throw new NotImplementedException();
        }

        object? IAstVisitor<object?>.VisitNameofExpression(NameofExpression expr)
        {
            throw new NotImplementedException();
        }

        object? IAstVisitor<object?>.VisitInterpolatedStringExpression(InterpolatedStringExpression expr)
        {
            throw new NotImplementedException();
        }

        object? IAstVisitor<object?>.VisitMathExpression(MathExpression expr)
        {
            throw new NotImplementedException();
        }

        object? IAstVisitor<object?>.VisitVectorExpression(VectorExpression expr)
        {
            throw new NotImplementedException();
        }

        object? IAstVisitor<object?>.VisitMatrixExpression(MatrixExpression expr)
        {
            throw new NotImplementedException();
        }

        object? IAstVisitor<object?>.VisitQuaternionExpression(QuaternionExpression expr)
        {
            throw new NotImplementedException();
        }

        object? IAstVisitor<object?>.VisitIsExpression(IsExpression expr)
        {
            throw new NotImplementedException();
        }

        object? IAstVisitor<object?>.VisitCastExpression(CastExpression expr)
        {
            throw new NotImplementedException();
        }

        object? IAstVisitor<object?>.VisitMatchExpression(MatchExpression expr)
        {
            throw new NotImplementedException();
        }

        object? IAstVisitor<object?>.VisitThrowExpression(ThrowExpression expr)
        {
            throw new NotImplementedException();
        }

        object? IAstVisitor<object?>.VisitMatchArm(MatchArm arm)
        {
            throw new NotImplementedException();
        }

        object? IAstVisitor<object?>.VisitStructLiteral(StructLiteral expr)
        {
            throw new NotImplementedException();
        }

        object? IAstVisitor<object?>.VisitIndexExpression(IndexExpression expr)
        {
            throw new NotImplementedException();
        }

        object? IAstVisitor<object?>.VisitTupleExpression(TupleExpression expr)
        {
            throw new NotImplementedException();
        }

        object? IAstVisitor<object?>.VisitSpreadExpression(SpreadExpression expr)
        {
            throw new NotImplementedException();
        }

        object? IAstVisitor<object?>.VisitRangeExpression(RangeExpression expr)
        {
            throw new NotImplementedException();
        }

        object? IAstVisitor<object?>.VisitBlockStatement(BlockStatement stmt)
        {
            throw new NotImplementedException();
        }

        object? IAstVisitor<object?>.VisitExpressionStatement(ExpressionStatement stmt)
        {
            throw new NotImplementedException();
        }

        object? IAstVisitor<object?>.VisitVariableDeclaration(VariableDeclaration stmt)
        {
            throw new NotImplementedException();
        }

        object? IAstVisitor<object?>.VisitIfStatement(IfStatement stmt)
        {
            throw new NotImplementedException();
        }

        object? IAstVisitor<object?>.VisitWhileStatement(WhileStatement stmt)
        {
            throw new NotImplementedException();
        }

        object? IAstVisitor<object?>.VisitForStatement(ForStatement stmt)
        {
            throw new NotImplementedException();
        }

        object? IAstVisitor<object?>.VisitForEachStatement(ForEachStatement stmt)
        {
            throw new NotImplementedException();
        }

        object? IAstVisitor<object?>.VisitRepeatStatement(RepeatStatement stmt)
        {
            throw new NotImplementedException();
        }

        object? IAstVisitor<object?>.VisitIterateStatement(IterateStatement stmt)
        {
            throw new NotImplementedException();
        }

        object? IAstVisitor<object?>.VisitParallelForStatement(ParallelForStatement stmt)
        {
            throw new NotImplementedException();
        }

        object? IAstVisitor<object?>.VisitDoWhileStatement(DoWhileStatement stmt)
        {
            throw new NotImplementedException();
        }

        object? IAstVisitor<object?>.VisitSwitchStatement(SwitchStatement stmt)
        {
            throw new NotImplementedException();
        }

        object? IAstVisitor<object?>.VisitReturnStatement(ReturnStatement stmt)
        {
            throw new NotImplementedException();
        }

        object? IAstVisitor<object?>.VisitBreakStatement(BreakStatement stmt)
        {
            throw new NotImplementedException();
        }

        object? IAstVisitor<object?>.VisitContinueStatement(ContinueStatement stmt)
        {
            throw new NotImplementedException();
        }

        object? IAstVisitor<object?>.VisitThrowStatement(ThrowStatement stmt)
        {
            throw new NotImplementedException();
        }

        object? IAstVisitor<object?>.VisitTryStatement(TryStatement stmt)
        {
            throw new NotImplementedException();
        }

        object? IAstVisitor<object?>.VisitUsingStatement(UsingStatement stmt)
        {
            throw new NotImplementedException();
        }

        object? IAstVisitor<object?>.VisitLockStatement(LockStatement stmt)
        {
            throw new NotImplementedException();
        }

        object? IAstVisitor<object?>.VisitUnsafeStatement(UnsafeStatement stmt)
        {
            throw new NotImplementedException();
        }

        object? IAstVisitor<object?>.VisitFixedStatement(FixedStatement stmt)
        {
            throw new NotImplementedException();
        }

        object? IAstVisitor<object?>.VisitYieldStatement(YieldStatement stmt)
        {
            throw new NotImplementedException();
        }

        object? IAstVisitor<object?>.VisitMatchStatement(MatchStatement stmt)
        {
            throw new NotImplementedException();
        }

        object? IAstVisitor<object?>.VisitAssemblyStatement(AssemblyStatement stmt)
        {
            throw new NotImplementedException();
        }

        object? IAstVisitor<object?>.VisitClassDeclaration(ClassDeclaration decl)
        {
            throw new NotImplementedException();
        }

        object? IAstVisitor<object?>.VisitInterfaceDeclaration(InterfaceDeclaration decl)
        {
            throw new NotImplementedException();
        }

        object? IAstVisitor<object?>.VisitStructDeclaration(StructDeclaration decl)
        {
            throw new NotImplementedException();
        }

        object? IAstVisitor<object?>.VisitEnumDeclaration(EnumDeclaration decl)
        {
            throw new NotImplementedException();
        }

        object? IAstVisitor<object?>.VisitFunctionDeclaration(FunctionDeclaration decl)
        {
            throw new NotImplementedException();
        }

        object? IAstVisitor<object?>.VisitPropertyDeclaration(PropertyDeclaration decl)
        {
            throw new NotImplementedException();
        }

        object? IAstVisitor<object?>.VisitFieldDeclaration(FieldDeclaration decl)
        {
            throw new NotImplementedException();
        }

        object? IAstVisitor<object?>.VisitNamespaceDeclaration(NamespaceDeclaration decl)
        {
            throw new NotImplementedException();
        }

        object? IAstVisitor<object?>.VisitImportDeclaration(ImportDeclaration decl)
        {
            throw new NotImplementedException();
        }

        object? IAstVisitor<object?>.VisitTypeAliasDeclaration(TypeAliasDeclaration decl)
        {
            throw new NotImplementedException();
        }

        object? IAstVisitor<object?>.VisitComponentDeclaration(ComponentDeclaration decl)
        {
            throw new NotImplementedException();
        }

        object? IAstVisitor<object?>.VisitSystemDeclaration(SystemDeclaration decl)
        {
            throw new NotImplementedException();
        }

        object? IAstVisitor<object?>.VisitEntityDeclaration(EntityDeclaration decl)
        {
            throw new NotImplementedException();
        }

        object? IAstVisitor<object?>.VisitDomainDeclaration(DomainDeclaration decl)
        {
            throw new NotImplementedException();
        }

        object? IAstVisitor<object?>.VisitMacroDeclaration(MacroDeclaration decl)
        {
            throw new NotImplementedException();
        }

        object? IAstVisitor<object?>.VisitTraitDeclaration(TraitDeclaration decl)
        {
            throw new NotImplementedException();
        }

        object? IAstVisitor<object?>.VisitImplementDeclaration(ImplementDeclaration decl)
        {
            throw new NotImplementedException();
        }

        object? IAstVisitor<object?>.VisitProgram(Core.AST.Program program)
        {
            throw new NotImplementedException();
        }
    }
} 