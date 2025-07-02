using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Ouro.Core.Lexer;
using Ouro.Core.Parser;
using Ouro.Core.Compiler;

namespace Ouro.LSP
{
    /// <summary>
    /// Ouroboros Language Server implementation
    /// </summary>
    public class LanguageServer
    {
        private readonly Stream input;
        private readonly Stream output;
        private readonly Dictionary<string, DocumentState> documents = new();
        private readonly CompletionProvider completionProvider;
        private readonly DiagnosticsProvider diagnosticsProvider;
        private readonly HoverProvider hoverProvider;
        private readonly DefinitionProvider definitionProvider;
        private readonly CancellationTokenSource cancellationSource = new();
        private bool initialized = false;
        private ServerCapabilities capabilities;

        public LanguageServer(Stream input, Stream output)
        {
            this.input = input;
            this.output = output;
            this.completionProvider = new CompletionProvider();
            this.diagnosticsProvider = new DiagnosticsProvider();
            this.hoverProvider = new HoverProvider();
            this.definitionProvider = new DefinitionProvider();
            this.capabilities = CreateServerCapabilities();
        }

        /// <summary>
        /// Start the language server
        /// </summary>
        public async Task RunAsync()
        {
            var reader = new MessageReader(input);
            var writer = new MessageWriter(output);

            while (!cancellationSource.Token.IsCancellationRequested)
            {
                try
                {
                    var message = await reader.ReadMessageAsync();
                    if (message == null)
                        break;

                    var response = await ProcessMessageAsync(message);
                    if (response != null)
                    {
                        await writer.WriteMessageAsync(response);
                    }
                }
                catch (Exception ex)
                {
                    LogError($"Error processing message: {ex}");
                }
            }
        }

        private async Task<JsonRpcMessage?> ProcessMessageAsync(JsonRpcMessage message)
        {
            if (message.Method == null)
            {
                // It's a response
                return null;
            }

            switch (message.Method)
            {
                case "initialize":
                    return HandleInitialize(message);
                    
                case "initialized":
                    HandleInitialized();
                    return null;
                    
                case "shutdown":
                    return HandleShutdown(message);
                    
                case "exit":
                    HandleExit();
                    return null;
                    
                case "textDocument/didOpen":
                    HandleDidOpenTextDocument(message);
                    return null;
                    
                case "textDocument/didChange":
                    HandleDidChangeTextDocument(message);
                    return null;
                    
                case "textDocument/didClose":
                    HandleDidCloseTextDocument(message);
                    return null;
                    
                case "textDocument/completion":
                    return await HandleCompletionAsync(message);
                    
                case "textDocument/hover":
                    return await HandleHoverAsync(message);
                    
                case "textDocument/definition":
                    return await HandleDefinitionAsync(message);
                    
                case "textDocument/formatting":
                    return await HandleFormattingAsync(message);
                    
                default:
                    // Method not supported
                    return new JsonRpcMessage
                    {
                        Id = message.Id,
                        Error = new JsonRpcError
                        {
                            Code = -32601,
                            Message = "Method not found"
                        }
                    };
            }
        }

        private JsonRpcMessage HandleInitialize(JsonRpcMessage message)
        {
            initialized = true;
            
            return new JsonRpcMessage
            {
                Id = message.Id,
                Result = new InitializeResult
                {
                    Capabilities = capabilities
                }
            };
        }

        private void HandleInitialized()
        {
            LogInfo("Language server initialized");
        }

        private JsonRpcMessage HandleShutdown(JsonRpcMessage message)
        {
            // Prepare for shutdown
            return new JsonRpcMessage
            {
                Id = message.Id,
                Result = null
            };
        }

        private void HandleExit()
        {
            cancellationSource.Cancel();
        }

        private void HandleDidOpenTextDocument(JsonRpcMessage message)
        {
            var parameters = message.Params as DidOpenTextDocumentParams;
            if (parameters == null) return;

            var uri = parameters.TextDocument.Uri;
            documents[uri] = new DocumentState
            {
                Uri = uri,
                Content = parameters.TextDocument.Text,
                Version = parameters.TextDocument.Version
            };

            // Trigger diagnostics
            Task.Run(() => PublishDiagnosticsAsync(uri));
        }

        private void HandleDidChangeTextDocument(JsonRpcMessage message)
        {
            var parameters = message.Params as DidChangeTextDocumentParams;
            if (parameters == null) return;

            var uri = parameters.TextDocument.Uri;
            if (!documents.ContainsKey(uri)) return;

            // Apply changes
            foreach (var change in parameters.ContentChanges)
            {
                documents[uri].Content = change.Text;
                documents[uri].Version = parameters.TextDocument.Version;
            }

            // Trigger diagnostics
            Task.Run(() => PublishDiagnosticsAsync(uri));
        }

        private void HandleDidCloseTextDocument(JsonRpcMessage message)
        {
            var parameters = message.Params as DidCloseTextDocumentParams;
            if (parameters == null) return;

            documents.Remove(parameters.TextDocument.Uri);
        }

        private async Task<JsonRpcMessage> HandleCompletionAsync(JsonRpcMessage message)
        {
            var parameters = message.Params as CompletionParams;
            if (parameters == null)
            {
                return CreateErrorResponse(message.Id, "Invalid parameters");
            }

            var uri = parameters.TextDocument.Uri;
            if (!documents.TryGetValue(uri, out var document))
            {
                return CreateErrorResponse(message.Id, "Document not found");
            }

            var completions = await completionProvider.GetCompletionsAsync(
                document.Content,
                parameters.Position,
                document.SyntaxTree);

            return new JsonRpcMessage
            {
                Id = message.Id,
                Result = new CompletionList
                {
                    IsIncomplete = false,
                    Items = completions
                }
            };
        }

        private async Task<JsonRpcMessage> HandleHoverAsync(JsonRpcMessage message)
        {
            var parameters = message.Params as HoverParams;
            if (parameters == null)
            {
                return CreateErrorResponse(message.Id, "Invalid parameters");
            }

            var uri = parameters.TextDocument.Uri;
            if (!documents.TryGetValue(uri, out var document))
            {
                return CreateErrorResponse(message.Id, "Document not found");
            }

            var hover = await hoverProvider.GetHoverAsync(
                document.Content,
                parameters.Position,
                document.SyntaxTree);

            return new JsonRpcMessage
            {
                Id = message.Id,
                Result = hover
            };
        }

        private async Task<JsonRpcMessage> HandleDefinitionAsync(JsonRpcMessage message)
        {
            var parameters = message.Params as DefinitionParams;
            if (parameters == null)
            {
                return CreateErrorResponse(message.Id, "Invalid parameters");
            }

            var uri = parameters.TextDocument.Uri;
            if (!documents.TryGetValue(uri, out var document))
            {
                return CreateErrorResponse(message.Id, "Document not found");
            }

            var locations = await definitionProvider.GetDefinitionAsync(
                document.Content,
                parameters.Position,
                document.SyntaxTree);

            return new JsonRpcMessage
            {
                Id = message.Id,
                Result = locations
            };
        }

        private async Task<JsonRpcMessage> HandleFormattingAsync(JsonRpcMessage message)
        {
            var parameters = message.Params as DocumentFormattingParams;
            if (parameters == null)
            {
                return CreateErrorResponse(message.Id, "Invalid parameters");
            }

            var uri = parameters.TextDocument.Uri;
            if (!documents.TryGetValue(uri, out var document))
            {
                return CreateErrorResponse(message.Id, "Document not found");
            }

            var edits = await FormatDocumentAsync(document);

            return new JsonRpcMessage
            {
                Id = message.Id,
                Result = edits
            };
        }

        private async Task PublishDiagnosticsAsync(string uri)
        {
            if (!documents.TryGetValue(uri, out var document))
                return;

            var diagnostics = await diagnosticsProvider.GetDiagnosticsAsync(document);

            var notification = new JsonRpcMessage
            {
                Method = "textDocument/publishDiagnostics",
                Params = new PublishDiagnosticsParams
                {
                    Uri = uri,
                    Diagnostics = diagnostics
                }
            };

            var writer = new MessageWriter(output);
            await writer.WriteMessageAsync(notification);
        }

        private async Task<List<TextEdit>> FormatDocumentAsync(DocumentState document)
        {
            // Simple formatter - would be more sophisticated in production
            var edits = new List<TextEdit>();
            
            // For now, just ensure consistent indentation
            var lines = document.Content.Split('\n');
            var formattedLines = new List<string>();
            int indentLevel = 0;
            
            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                
                // Decrease indent for closing braces
                if (trimmed.StartsWith("}"))
                    indentLevel = Math.Max(0, indentLevel - 1);
                
                // Add formatted line
                formattedLines.Add(new string(' ', indentLevel * 4) + trimmed);
                
                // Increase indent for opening braces
                if (trimmed.EndsWith("{"))
                    indentLevel++;
            }
            
            var formatted = string.Join("\n", formattedLines);
            
            if (formatted != document.Content)
            {
                edits.Add(new TextEdit
                {
                    Range = new Range
                    {
                        Start = new Position { Line = 0, Character = 0 },
                        End = new Position { Line = lines.Length - 1, Character = lines[^1].Length }
                    },
                    NewText = formatted
                });
            }
            
            return edits;
        }

        private ServerCapabilities CreateServerCapabilities()
        {
            return new ServerCapabilities
            {
                TextDocumentSync = new TextDocumentSyncOptions
                {
                    OpenClose = true,
                    Change = TextDocumentSyncKind.Full
                },
                CompletionProvider = new CompletionOptions
                {
                    ResolveProvider = false,
                    TriggerCharacters = new[] { ".", ":" }
                },
                HoverProvider = true,
                DefinitionProvider = true,
                DocumentFormattingProvider = true
            };
        }

        private JsonRpcMessage CreateErrorResponse(object? id, string message)
        {
            return new JsonRpcMessage
            {
                Id = id,
                Error = new JsonRpcError
                {
                    Code = -32603,
                    Message = message
                }
            };
        }

        private void LogInfo(string message)
        {
            Console.Error.WriteLine($"[INFO] {message}");
        }

        private void LogError(string message)
        {
            Console.Error.WriteLine($"[ERROR] {message}");
        }
    }

    /// <summary>
    /// Document state tracking
    /// </summary>
    internal class DocumentState
    {
        public string Uri { get; set; } = "";
        public string Content { get; set; } = "";
        public int Version { get; set; }
        public Core.AST.Program? SyntaxTree { get; set; }
        public List<Diagnostic> Diagnostics { get; set; } = new();
    }
} 