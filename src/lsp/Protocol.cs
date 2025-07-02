using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Ouro.LSP
{
    /// <summary>
    /// JSON-RPC message
    /// </summary>
    public class JsonRpcMessage
    {
        [JsonPropertyName("jsonrpc")]
        public string JsonRpc { get; set; } = "2.0";

        [JsonPropertyName("id")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public object? Id { get; set; }

        [JsonPropertyName("method")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Method { get; set; }

        [JsonPropertyName("params")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public object? Params { get; set; }

        [JsonPropertyName("result")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public object? Result { get; set; }

        [JsonPropertyName("error")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public JsonRpcError? Error { get; set; }
    }

    /// <summary>
    /// JSON-RPC error
    /// </summary>
    public class JsonRpcError
    {
        [JsonPropertyName("code")]
        public int Code { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; } = "";

        [JsonPropertyName("data")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public object? Data { get; set; }
    }

    /// <summary>
    /// Initialize result
    /// </summary>
    public class InitializeResult
    {
        [JsonPropertyName("capabilities")]
        public ServerCapabilities Capabilities { get; set; } = new();
    }

    /// <summary>
    /// Server capabilities
    /// </summary>
    public class ServerCapabilities
    {
        [JsonPropertyName("textDocumentSync")]
        public TextDocumentSyncOptions? TextDocumentSync { get; set; }

        [JsonPropertyName("completionProvider")]
        public CompletionOptions? CompletionProvider { get; set; }

        [JsonPropertyName("hoverProvider")]
        public bool HoverProvider { get; set; }

        [JsonPropertyName("definitionProvider")]
        public bool DefinitionProvider { get; set; }

        [JsonPropertyName("documentFormattingProvider")]
        public bool DocumentFormattingProvider { get; set; }

        [JsonPropertyName("documentRangeFormattingProvider")]
        public bool DocumentRangeFormattingProvider { get; set; }

        [JsonPropertyName("documentSymbolProvider")]
        public bool DocumentSymbolProvider { get; set; }

        [JsonPropertyName("workspaceSymbolProvider")]
        public bool WorkspaceSymbolProvider { get; set; }

        [JsonPropertyName("codeActionProvider")]
        public bool CodeActionProvider { get; set; }

        [JsonPropertyName("renameProvider")]
        public bool RenameProvider { get; set; }
    }

    /// <summary>
    /// Text document sync options
    /// </summary>
    public class TextDocumentSyncOptions
    {
        [JsonPropertyName("openClose")]
        public bool OpenClose { get; set; }

        [JsonPropertyName("change")]
        public TextDocumentSyncKind Change { get; set; }
    }

    /// <summary>
    /// Text document sync kind
    /// </summary>
    public enum TextDocumentSyncKind
    {
        None = 0,
        Full = 1,
        Incremental = 2
    }

    /// <summary>
    /// Completion options
    /// </summary>
    public class CompletionOptions
    {
        [JsonPropertyName("resolveProvider")]
        public bool ResolveProvider { get; set; }

        [JsonPropertyName("triggerCharacters")]
        public string[]? TriggerCharacters { get; set; }
    }

    /// <summary>
    /// Position in a text document
    /// </summary>
    public class Position
    {
        [JsonPropertyName("line")]
        public int Line { get; set; }

        [JsonPropertyName("character")]
        public int Character { get; set; }
    }

    /// <summary>
    /// Range in a text document
    /// </summary>
    public class Range
    {
        [JsonPropertyName("start")]
        public Position Start { get; set; } = new();

        [JsonPropertyName("end")]
        public Position End { get; set; } = new();
    }

    /// <summary>
    /// Location in a text document
    /// </summary>
    public class Location
    {
        [JsonPropertyName("uri")]
        public string Uri { get; set; } = "";

        [JsonPropertyName("range")]
        public Range Range { get; set; } = new();
    }

    /// <summary>
    /// Text document identifier
    /// </summary>
    public class TextDocumentIdentifier
    {
        [JsonPropertyName("uri")]
        public string Uri { get; set; } = "";
    }

    /// <summary>
    /// Versioned text document identifier
    /// </summary>
    public class VersionedTextDocumentIdentifier : TextDocumentIdentifier
    {
        [JsonPropertyName("version")]
        public int Version { get; set; }
    }

    /// <summary>
    /// Text document item
    /// </summary>
    public class TextDocumentItem
    {
        [JsonPropertyName("uri")]
        public string Uri { get; set; } = "";

        [JsonPropertyName("languageId")]
        public string LanguageId { get; set; } = "";

        [JsonPropertyName("version")]
        public int Version { get; set; }

        [JsonPropertyName("text")]
        public string Text { get; set; } = "";
    }

    /// <summary>
    /// Text document position parameters
    /// </summary>
    public class TextDocumentPositionParams
    {
        [JsonPropertyName("textDocument")]
        public TextDocumentIdentifier TextDocument { get; set; } = new();

        [JsonPropertyName("position")]
        public Position Position { get; set; } = new();
    }

    /// <summary>
    /// Did open text document parameters
    /// </summary>
    public class DidOpenTextDocumentParams
    {
        [JsonPropertyName("textDocument")]
        public TextDocumentItem TextDocument { get; set; } = new();
    }

    /// <summary>
    /// Did change text document parameters
    /// </summary>
    public class DidChangeTextDocumentParams
    {
        [JsonPropertyName("textDocument")]
        public VersionedTextDocumentIdentifier TextDocument { get; set; } = new();

        [JsonPropertyName("contentChanges")]
        public List<TextDocumentContentChangeEvent> ContentChanges { get; set; } = new();
    }

    /// <summary>
    /// Text document content change event
    /// </summary>
    public class TextDocumentContentChangeEvent
    {
        [JsonPropertyName("range")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public Range? Range { get; set; }

        [JsonPropertyName("text")]
        public string Text { get; set; } = "";
    }

    /// <summary>
    /// Did close text document parameters
    /// </summary>
    public class DidCloseTextDocumentParams
    {
        [JsonPropertyName("textDocument")]
        public TextDocumentIdentifier TextDocument { get; set; } = new();
    }

    /// <summary>
    /// Completion parameters
    /// </summary>
    public class CompletionParams : TextDocumentPositionParams
    {
        [JsonPropertyName("context")]
        public CompletionContext? Context { get; set; }
    }

    /// <summary>
    /// Completion context
    /// </summary>
    public class CompletionContext
    {
        [JsonPropertyName("triggerKind")]
        public CompletionTriggerKind TriggerKind { get; set; }

        [JsonPropertyName("triggerCharacter")]
        public string? TriggerCharacter { get; set; }
    }

    /// <summary>
    /// Completion trigger kind
    /// </summary>
    public enum CompletionTriggerKind
    {
        Invoked = 1,
        TriggerCharacter = 2,
        TriggerForIncompleteCompletions = 3
    }

    /// <summary>
    /// Completion list
    /// </summary>
    public class CompletionList
    {
        [JsonPropertyName("isIncomplete")]
        public bool IsIncomplete { get; set; }

        [JsonPropertyName("items")]
        public List<CompletionItem> Items { get; set; } = new();
    }

    /// <summary>
    /// Completion item
    /// </summary>
    public class CompletionItem
    {
        [JsonPropertyName("label")]
        public string Label { get; set; } = "";

        [JsonPropertyName("kind")]
        public CompletionItemKind? Kind { get; set; }

        [JsonPropertyName("detail")]
        public string? Detail { get; set; }

        [JsonPropertyName("documentation")]
        public string? Documentation { get; set; }

        [JsonPropertyName("sortText")]
        public string? SortText { get; set; }

        [JsonPropertyName("filterText")]
        public string? FilterText { get; set; }

        [JsonPropertyName("insertText")]
        public string? InsertText { get; set; }

        [JsonPropertyName("insertTextFormat")]
        public InsertTextFormat? InsertTextFormat { get; set; }
    }

    /// <summary>
    /// Completion item kind
    /// </summary>
    public enum CompletionItemKind
    {
        Text = 1,
        Method = 2,
        Function = 3,
        Constructor = 4,
        Field = 5,
        Variable = 6,
        Class = 7,
        Interface = 8,
        Module = 9,
        Property = 10,
        Unit = 11,
        Value = 12,
        Enum = 13,
        Keyword = 14,
        Snippet = 15,
        Color = 16,
        File = 17,
        Reference = 18,
        Folder = 19,
        EnumMember = 20,
        Constant = 21,
        Struct = 22,
        Event = 23,
        Operator = 24,
        TypeParameter = 25
    }

    /// <summary>
    /// Insert text format
    /// </summary>
    public enum InsertTextFormat
    {
        PlainText = 1,
        Snippet = 2
    }

    /// <summary>
    /// Hover parameters
    /// </summary>
    public class HoverParams : TextDocumentPositionParams
    {
    }

    /// <summary>
    /// Hover result
    /// </summary>
    public class Hover
    {
        [JsonPropertyName("contents")]
        public MarkupContent Contents { get; set; } = new();

        [JsonPropertyName("range")]
        public Range? Range { get; set; }
    }

    /// <summary>
    /// Markup content
    /// </summary>
    public class MarkupContent
    {
        [JsonPropertyName("kind")]
        public string Kind { get; set; } = "markdown";

        [JsonPropertyName("value")]
        public string Value { get; set; } = "";
    }

    /// <summary>
    /// Definition parameters
    /// </summary>
    public class DefinitionParams : TextDocumentPositionParams
    {
    }

    /// <summary>
    /// Document formatting parameters
    /// </summary>
    public class DocumentFormattingParams
    {
        [JsonPropertyName("textDocument")]
        public TextDocumentIdentifier TextDocument { get; set; } = new();

        [JsonPropertyName("options")]
        public FormattingOptions Options { get; set; } = new();
    }

    /// <summary>
    /// Formatting options
    /// </summary>
    public class FormattingOptions
    {
        [JsonPropertyName("tabSize")]
        public int TabSize { get; set; } = 4;

        [JsonPropertyName("insertSpaces")]
        public bool InsertSpaces { get; set; } = true;
    }

    /// <summary>
    /// Text edit
    /// </summary>
    public class TextEdit
    {
        [JsonPropertyName("range")]
        public Range Range { get; set; } = new();

        [JsonPropertyName("newText")]
        public string NewText { get; set; } = "";
    }

    /// <summary>
    /// Publish diagnostics parameters
    /// </summary>
    public class PublishDiagnosticsParams
    {
        [JsonPropertyName("uri")]
        public string Uri { get; set; } = "";

        [JsonPropertyName("diagnostics")]
        public List<Diagnostic> Diagnostics { get; set; } = new();
    }

    /// <summary>
    /// Diagnostic
    /// </summary>
    public class Diagnostic
    {
        [JsonPropertyName("range")]
        public Range Range { get; set; } = new();

        [JsonPropertyName("severity")]
        public DiagnosticSeverity Severity { get; set; }

        [JsonPropertyName("code")]
        public string? Code { get; set; }

        [JsonPropertyName("source")]
        public string? Source { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; } = "";
    }

    /// <summary>
    /// Diagnostic severity
    /// </summary>
    public enum DiagnosticSeverity
    {
        Error = 1,
        Warning = 2,
        Information = 3,
        Hint = 4
    }
} 