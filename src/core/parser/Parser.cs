using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Globalization;
using Ouro.Tokens;
using Ouro.Core.AST;

namespace Ouro.Core.Parser
{
    /// <summary>
    /// Recursive descent parser for the Ouro language
    /// Supports all three syntax levels: High, Medium, and Low
    /// </summary>
    public class Parser
    {
        private readonly List<Token> _tokens;
        private int _current = 0;
        private SyntaxLevel _currentSyntaxLevel = SyntaxLevel.Medium;
        private List<string> _currentGenericTypeParameters = new List<string>();

        public Parser(List<Token> tokens)
        {
            _tokens = tokens;
            
            // Write debug info to file
            var debugFile = "parser_debug.txt";
            try
            {
                var debugInfo = new System.Text.StringBuilder();
                debugInfo.AppendLine($"Parser constructor: Received {tokens.Count} tokens");
                
                if (tokens.Count > 0)
                {
                    debugInfo.AppendLine($"First 20 tokens:");
                    for (int i = 0; i < Math.Min(20, tokens.Count); i++)
                    {
                        var t = tokens[i];
                        debugInfo.AppendLine($"  Token {i}: Line={t.Line} Col={t.Column} Type={t.Type} Lexeme='{t.Lexeme}'");
                    }
                }
                
                System.IO.File.WriteAllText(debugFile, debugInfo.ToString());
            }
            catch (System.Exception ex)
            {
                // Log the error but don't fail the parsing process
                // Warning: Failed to write debug file
            }
        }

        public Ouro.Core.AST.Program Parse()
        {
            var statements = new List<Statement>();
            var errorCount = 0;
            const int maxErrors = 50; // Prevent infinite error loops

            while (!IsAtEnd() && errorCount < maxErrors)
            {
                try
                {
                    // Check for C# interop method signatures (but not native Ouroboros functions with C#-style syntax)
                    // Only treat as C# interop if explicitly marked or in interop context
                    if (IsCSharpMethodSignature() && !IsNativeOuroborosFunctionWithCSharpSyntax())
                    {
                        // Detected C# interop method signature, parsing mixed format
                        var methodBody = ParseCSharpMethodWithOuroborosBody();
                        statements.Add(methodBody);
                        continue;
                    }
                    
                    // Check if this is definitely a declaration token that should never be a statement
                    var current = Current();
                    if (current.Type == TokenType.Namespace || current.Type == TokenType.Using || 
                        current.Type == TokenType.Import || current.Type == TokenType.Class || 
                        current.Type == TokenType.Interface || current.Type == TokenType.Struct || 
                        current.Type == TokenType.Enum || current.Type == TokenType.Function ||
                        current.Type == TokenType.HighLevel || current.Type == TokenType.MediumLevel || 
                        current.Type == TokenType.LowLevel || current.Type == TokenType.Assembly || current.Type == TokenType.SpirvAssembly)
                    {
                        // These are always declarations, never statements
                        var declaration = ParseDeclaration();
                        statements.Add(declaration);
                    }
                    else
                    {
                        // Try to parse as declaration first
                        var savedPosition = _current;
                        try
                        {
                            var declaration = ParseDeclaration();
                            statements.Add(declaration);
                        }
                        catch (ParseException)
                        {
                            // If declaration parsing fails, try as statement  
                            _current = savedPosition;
                            var stmt = ParseStatement();
                            statements.Add(stmt);
                        }
                    }
                }
                catch (ParseException error)
                {
                    errorCount++;
                    Console.WriteLine($"Parse error at line {error.Token?.Line ?? Current().Line}: {error.Message}");
                    RecordError(error);
                    Synchronize();
                    // Continue parsing instead of throwing
                    // This allows the parser to continue after encountering errors
                }
                catch (Exception ex)
                {
                    errorCount++;
                    // Unexpected error - wrap and continue
                    Console.WriteLine($"Unexpected error at line {Current().Line}: {ex.Message}");
                    RecordError(new ParseException($"Unexpected error: {ex.Message}", Current()));
                    Synchronize();
                }
            }

            if (errorCount >= maxErrors)
            {
                throw new ParseException($"Too many parse errors (>{maxErrors}). Aborting parse.", Current());
            }

            return new Ouro.Core.AST.Program(statements);
        }
        
        private readonly List<ParseException> _parseErrors = new List<ParseException>();
        
        private void RecordError(ParseException error)
        {
            _parseErrors.Add(error);
        }
        
        public bool HadError => _parseErrors.Count > 0;
        public IReadOnlyList<ParseException> Errors => _parseErrors.AsReadOnly();

        #region Mixed C#/Ouroboros Format Support

        private bool IsNativeOuroborosFunctionWithCSharpSyntax()
        {
            // For now, assume that C#-style syntax in Ouroboros context should be parsed as native
            // This method distinguishes native Ouroboros functions using C# syntax from true C# interop
            // In the future, this could check for explicit interop markers or context
            return true; // Default to treating C#-style syntax as native Ouroboros
        }

        private bool IsCSharpMethodSignature()
        {
            // Look ahead to detect C# method signatures like:
            // public static void TestMethodName()
            // private int MethodName(params)
            // public static ReturnType MethodName<T>(params)
            
            var lookAhead = _current;
            
            // Check for access modifiers (public, private, protected, internal)
            if (lookAhead < _tokens.Count && 
                (_tokens[lookAhead].Type == TokenType.Public || 
                 _tokens[lookAhead].Type == TokenType.Private ||
                 _tokens[lookAhead].Type == TokenType.Protected ||
                 _tokens[lookAhead].Type == TokenType.Internal))
            {
                lookAhead++;
                
                // Check for static keyword (optional)
                if (lookAhead < _tokens.Count && _tokens[lookAhead].Type == TokenType.Static)
                {
                    lookAhead++;
                }
                
                // Check for return type (void, identifier, or known types)
                if (lookAhead < _tokens.Count && 
                    (_tokens[lookAhead].Type == TokenType.Void ||
                     _tokens[lookAhead].Type == TokenType.Identifier ||
                     IsKnownTypeName(_tokens[lookAhead])))
                {
                    lookAhead++;
                    
                    // Check for method name (identifier)
                    if (lookAhead < _tokens.Count && _tokens[lookAhead].Type == TokenType.Identifier)
                    {
                        lookAhead++;
                        
                        // Check for generic type parameters (optional)
                        if (lookAhead < _tokens.Count && _tokens[lookAhead].Type == TokenType.Less)
                        {
                            // Skip generic parameters
                            var depth = 1;
                            lookAhead++;
                            while (lookAhead < _tokens.Count && depth > 0)
                            {
                                if (_tokens[lookAhead].Type == TokenType.Less) depth++;
                                if (_tokens[lookAhead].Type == TokenType.Greater) depth--;
                                lookAhead++;
                            }
                        }
                        
                        // Check for parameter list
                        if (lookAhead < _tokens.Count && _tokens[lookAhead].Type == TokenType.LeftParen)
                        {
                                                    // IsCSharpMethodSignature detected
                        return true;
                        }
                    }
                }
            }
            
            return false;
        }

        private Statement ParseCSharpMethodWithOuroborosBody()
        {
            // ParseCSharpMethodWithOuroborosBody starting
            
            // Skip the C# method signature by consuming tokens until we reach the opening brace
            while (!Check(TokenType.LeftBrace) && !IsAtEnd())
            {
                // Skipping C# signature token
                Advance();
            }
            
            if (IsAtEnd())
            {
                throw Error(Current(), "Expected method body after C# method signature.");
            }
            
            // Consume the opening brace
            Consume(TokenType.LeftBrace, "Expected '{' to start method body.");
            Console.WriteLine($"DEBUG: Entered method body, parsing Ouro code");
            
            // Parse the method body as Ouro statements
            var statements = new List<Statement>();
            while (!Check(TokenType.RightBrace) && !IsAtEnd())
            {
                try
                {
                    // Parse each statement in the method body as Ouro code
                    var statement = ParseStatement();
                    statements.Add(statement);
                    Console.WriteLine($"DEBUG: Parsed Ouroboros statement in C# method body");
                }
                catch (ParseException ex)
                {
                    Console.WriteLine($"DEBUG: Error parsing statement in C# method body: {ex.Message}");
                    // If we can't parse as Ouroboros, try skipping to next statement
                    Synchronize();
                }
            }
            
            // Consume the closing brace
            if (!IsAtEnd())
            {
                Consume(TokenType.RightBrace, "Expected '}' to close method body.");
                Console.WriteLine($"DEBUG: Completed parsing C# method with Ouroboros body");
            }
            
            // Return a block statement containing all the Ouroboros statements
            return new BlockStatement(statements);
        }

        private bool IsUsingDomainBlock()
        {
            // Look ahead to see if this is: using DomainName { ... }
            // rather than: using System.Collections;
            
            var lookAhead = _current;
            
            if (lookAhead < _tokens.Count && _tokens[lookAhead].Type == TokenType.Using)
            {
                lookAhead++;
                
                // Check for domain name (identifier)
                if (lookAhead < _tokens.Count && _tokens[lookAhead].Type == TokenType.Identifier)
                {
                    lookAhead++;
                    
                    // Check for opening brace (domain block) vs semicolon/dot (import)
                    if (lookAhead < _tokens.Count && _tokens[lookAhead].Type == TokenType.LeftBrace)
                    {
                        Console.WriteLine($"DEBUG: IsUsingDomainBlock detected at line {Current().Line}");
                        return true;
                    }
                }
            }
            
            return false;
        }

        private Statement ParseUsingDomainBlock()
        {
            Console.WriteLine($"DEBUG: ParseUsingDomainBlock starting");
            
            // Parse: using DomainName { ... }
            Consume(TokenType.Using, "Expected 'using' keyword.");
            var domainName = Consume(TokenType.Identifier, "Expected domain name after 'using'.");
            Consume(TokenType.LeftBrace, "Expected '{' after domain name.");
            Console.WriteLine($"DEBUG: Entering domain block for: {domainName.Lexeme}");
            
            // Parse the block body as Ouroboros statements
            var statements = new List<Statement>();
            while (!Check(TokenType.RightBrace) && !IsAtEnd())
            {
                try
                {
                    // Parse each statement in the domain block
                    var statement = ParseStatement();
                    statements.Add(statement);
                    Console.WriteLine($"DEBUG: Parsed statement in domain block");
                }
                catch (ParseException ex)
                {
                    Console.WriteLine($"DEBUG: Error parsing statement in domain block: {ex.Message}");
                    // Try to recover by synchronizing
                    Synchronize();
                }
            }
            
            // Consume the closing brace
            if (!IsAtEnd())
            {
                Consume(TokenType.RightBrace, "Expected '}' to close domain block.");
                Console.WriteLine($"DEBUG: Completed parsing domain block for: {domainName.Lexeme}");
            }
            
            // For now, return a block statement with the domain name as a comment
            // In a full implementation, this would be a specialized UsingDomainStatement
            return new BlockStatement(statements);
        }

        #endregion

        #region Declaration Parsing

        private Statement ParseDeclaration()
        {
            try
            {
                // Skip any attributes before the declaration
                SkipAttributes();
                
                // Check for syntax level markers
                SyntaxLevel? scopedSyntaxLevel = null;
                if (Match(TokenType.HighLevel, TokenType.MediumLevel, TokenType.LowLevel, TokenType.Assembly, TokenType.SpirvAssembly))
                {
                    scopedSyntaxLevel = Previous().Type switch
                    {
                        TokenType.HighLevel => SyntaxLevel.High,
                        TokenType.MediumLevel => SyntaxLevel.Medium,
                        TokenType.LowLevel => SyntaxLevel.Low,
                        TokenType.Assembly => SyntaxLevel.Assembly,
                        TokenType.SpirvAssembly => SyntaxLevel.Assembly, // SPIR-V is also assembly-level
                        _ => SyntaxLevel.Medium
                    };

                    // For function declarations, we'll apply the syntax level in ParseFunctionDeclaration
                    // For other declarations, apply it immediately
                    if (!Check(TokenType.Function))
                    {
                        var previousLevel = _currentSyntaxLevel;
                        _currentSyntaxLevel = scopedSyntaxLevel.Value;
                        try
                        {
                            var declaration = ParseDeclaration();
                            return declaration;
                        }
                        finally
                        {
                            _currentSyntaxLevel = previousLevel;
                        }
                    }
                }

                // Handle unsafe statements at declaration level
                if (Match(TokenType.Unsafe)) return ParseUnsafeStatement();

                // Collect modifiers
                var modifiers = ParseModifiers();

                // Parse declarations based on keyword
                Console.WriteLine($"DEBUG: ParseDeclaration - checking declarations for {Current().Type} '{Current().Lexeme}'");
                if (Match(TokenType.Class)) return ParseClass(modifiers);
                if (Match(TokenType.Interface)) return ParseInterface(modifiers);
                if (Match(TokenType.Struct)) return ParseStruct(modifiers);
                if (Match(TokenType.UnionKeyword)) return ParseUnion(modifiers);
                if (Match(TokenType.Enum)) return ParseEnum(modifiers);
                Console.WriteLine($"DEBUG: ParseDeclaration - About to check for Domain, current token: {Current().Type}");
                if (Check(TokenType.Domain))
                {
                    Console.WriteLine($"DEBUG: ParseDeclaration - Found Domain token, calling ParseDomain");
                    Match(TokenType.Domain);
                    return ParseDomain(modifiers);
                }
                Console.WriteLine($"DEBUG: ParseDeclaration - Domain check failed, current token: {Current().Type}");
                
                // Check for 'module' keyword
                if (Check(TokenType.Module))
                {
                    Console.WriteLine($"DEBUG: ParseDeclaration - Found Module token, calling ParseModule");
                    Match(TokenType.Module);
                    return ParseModule(modifiers);
                }
                
                if (Match(TokenType.Function)) 
                {
                    return ParseFunctionDeclaration(modifiers, scopedSyntaxLevel);
                }

                // Check for const declarations (const token may have been consumed as a modifier)
                if (Check(TokenType.Const))
                {
                    return ParseConstDeclaration();
                }
                else if (modifiers.Contains(Modifier.Const) && Check(TokenType.Identifier))
                {
                    // Const was consumed as a modifier, parse const field declaration
                    var name = Advance(); // consume identifier
                    Consume(TokenType.Assign, "Expected '=' after constant name.");
                    var initializer = ParseExpression();
                    Consume(TokenType.Semicolon, "Expected ';' after constant declaration.");
                    
                    // Create a field declaration with const modifier
                    var constType = new TypeNode("const");
                    return new FieldDeclaration(name, constType, initializer, modifiers);
                }
                
                // Check for trait declarations (treated as identifiers since no TokenType.Trait exists)
                if (Check(TokenType.Identifier) && Current().Lexeme == "trait")
                {
                    Console.WriteLine($"DEBUG: Detected trait declaration in class member parsing");
                    return ParseTraitDeclaration();
                }
                
                // Check for implement blocks (trait implementations)
                if (Check(TokenType.Identifier) && Current().Lexeme == "implement")
                {
                    Console.WriteLine($"DEBUG: Detected implement block in class member parsing");
                    return ParseImplementDeclaration();
                }
                
                // Check for destructor declarations
                if (Check(TokenType.Identifier) && Current().Lexeme == "destructor")
                {
                    Console.WriteLine($"DEBUG: Detected destructor declaration in class member parsing");
                    return ParseDestructorDeclaration();
                }
                
                // Check for extern blocks (for foreign function interfaces)
                if (Check(TokenType.Identifier) && Current().Lexeme == "extern")
                {
                    Console.WriteLine($"DEBUG: Detected extern block declaration");
                    return ParseExternBlock();
                }

                                // Continue with rest of parsing...
                if (Match(TokenType.Namespace)) return ParseNamespace();
                if (Match(TokenType.Using, TokenType.Import)) return ParseUsing();

                if (Match(TokenType.Alias)) return ParseTypeAlias();
                if (Match(TokenType.Type)) return ParseTypeAlias(); // Support both 'alias' and 'type' keywords

                // Data-oriented declarations
                if (Match(TokenType.Component)) return ParseComponent(modifiers);
                if (Match(TokenType.System)) return ParseSystem(modifiers);
                if (Match(TokenType.Entity)) return ParseEntity(modifiers);

                // Check for operator overloading
                if (Match(TokenType.Identifier) && Previous().Lexeme == "operator")
                {
                    return ParseOperatorOverload(modifiers);
                }

                // Check for Rust-style variable declarations: IDENTIFIER: Type = value;
                Console.WriteLine($"DEBUG: ParseDeclaration - About to check Rust-style variable declaration");
                Console.WriteLine($"DEBUG: Current token: {Current().Type} '{Current().Lexeme}'");
                Console.WriteLine($"DEBUG: Next token: {PeekNext()?.Type} '{PeekNext()?.Lexeme}'");
                
                // Check two patterns:
                // 1. Current token is identifier, next is colon: IDENTIFIER: Type = value;
                // 2. Current token is colon, previous was identifier: means we already consumed identifier
                if ((Check(TokenType.Identifier) && PeekNext()?.Type == TokenType.Colon) ||
                    (Check(TokenType.Colon) && PeekPrevious()?.Type == TokenType.Identifier))
                {
                    Console.WriteLine($"DEBUG: ParseDeclaration - Found Rust-style variable declaration");
                    return ParseRustStyleVariableDeclaration(modifiers);
                }
                Console.WriteLine($"DEBUG: ParseDeclaration - Rust-style check failed");

                // Check for type declaration followed by identifier (field or method)
                if (PeekType() != null)
                {
                    var type = ParseType();
                    var name = ConsumeIdentifierOrGreekLetter("Expected name after type.");

                    if (Match(TokenType.LeftParen))
                    {
                        // It's a method declaration
                        // Backtrack and parse as function
                        _current--;
                        return ParseFunctionWithSyntaxLevel(type, name, modifiers, scopedSyntaxLevel);
                    }
                    else
                    {
                        // It's a field declaration
                        return ParseField(type, name, modifiers);
                    }
                }

                // Check for assembly blocks that appear in function bodies within declarations
                if (Check(TokenType.LeftBrace))
                {
                    Console.WriteLine($"DEBUG: ParseDeclaration - Found assembly block starting with {{ , parsing as statement");
                    // This is likely an assembly block - parse it as a statement and wrap in a declaration
                    var assemblyStatement = ParseAssemblyStatement();
                    
                    // For now, wrap the assembly statement in an expression statement
                    // In a full implementation, you might want a specific AssemblyDeclaration type
                    return new ExpressionStatement(new LiteralExpression(
                        new Token(TokenType.StringLiteral, "assembly_block", "assembly_block", 
                                assemblyStatement.Token.Line, assemblyStatement.Token.Column, 
                                assemblyStatement.Token.StartPosition, assemblyStatement.Token.EndPosition,
                                assemblyStatement.Token.FileName, assemblyStatement.Token.SyntaxLevel)));
                }

                throw Error(Current(), "Expected declaration.");
            }
            catch (ParseException ex)
            {
                Console.WriteLine($"Error parsing declaration: {ex.Message}");
                throw;
            }
        }

        private ClassDeclaration ParseClass(List<Modifier> modifiers)
        {
            var name = Consume(TokenType.Identifier, "Expected class name");
            var typeParameters = new List<TypeParameter>();
            
            if (Match(TokenType.Less))
            {
                typeParameters = ParseTypeParameters();
                Consume(TokenType.Greater, "Expected '>' after type parameters");
            }

            var baseTypes = new List<TypeNode>();
            if (Match(TokenType.Colon))
            {
                do
                {
                    baseTypes.Add(ParseType());
                } while (Match(TokenType.Comma));
            }

            Consume(TokenType.LeftBrace, "Expected '{' before class body");

            var members = new List<Declaration>();
            while (!Check(TokenType.RightBrace) && !IsAtEnd())
            {
                try
                {
                    var decl = ParseDeclaration();
                    if (decl is Declaration d)
                    {
                        members.Add(d);
                    }
                    else
                    {
                        throw Error(Current(), "Expected class member declaration");
                    }
                }
                catch (ParseException)
                {
                    // Skip to next member
                    while (!IsAtEnd() && !Check(TokenType.RightBrace))
                    {
                        if (Match(TokenType.Semicolon)) break;
                        if (Check(TokenType.Public) || Check(TokenType.Private) || 
                            Check(TokenType.Protected) || Check(TokenType.Internal) ||
                            Check(TokenType.Static) || Check(TokenType.Virtual) ||
                            Check(TokenType.Override) || Check(TokenType.Abstract))
                        {
                            break;
                        }
                        Advance();
                    }
                }
            }

            Consume(TokenType.RightBrace, "Expected '}' after class body");

            var baseClass = baseTypes.Count > 0 ? baseTypes[0] : null;
            var interfaces = baseTypes.Count > 1 ? baseTypes.Skip(1).ToList() : new List<TypeNode>();
            
            return new ClassDeclaration(Previous(), name, baseClass, interfaces, members, typeParameters, modifiers);
        }

        private InterfaceDeclaration ParseInterface(List<Modifier> modifiers)
        {
            var name = Consume(TokenType.Identifier, "Expected interface name.");
            var typeParameters = ParseTypeParameters();

            var baseInterfaces = new List<TypeNode>();
            if (Match(TokenType.Colon))
            {
                do
                {
                    baseInterfaces.Add(ParseType());
                } while (Match(TokenType.Comma));
            }

            Consume(TokenType.LeftBrace, "Expected '{' before interface body.");

            var members = new List<Declaration>();
            while (!Check(TokenType.RightBrace) && !IsAtEnd())
            {
                // Check for compact field syntax: x, y, z: type;
                if (IsCompactFieldDeclaration())
                {
                    members.AddRange(ParseCompactFieldDeclaration());
                }
                else
                {
                    members.Add((Declaration)ParseDeclaration());
                }
            }

            Consume(TokenType.RightBrace, "Expected '}' after interface body.");

            return new InterfaceDeclaration(Previous(), name, baseInterfaces, members, typeParameters, modifiers);
        }

        private StructDeclaration ParseStruct(List<Modifier> modifiers)
        {
            var name = Consume(TokenType.Identifier, "Expected struct name.");
            var typeParameters = ParseTypeParameters();

            var interfaces = new List<TypeNode>();
            if (Match(TokenType.Colon))
            {
                do
                {
                    interfaces.Add(ParseType());
                } while (Match(TokenType.Comma));
            }

            Consume(TokenType.LeftBrace, "Expected '{' before struct body.");

            var members = new List<Declaration>();
            
            // Store current syntax level and temporarily switch to Medium for struct body parsing
            // This ensures field parsing doesn't get routed through low-level statement parsing
            var savedSyntaxLevel = _currentSyntaxLevel;
            _currentSyntaxLevel = SyntaxLevel.Medium;
            
            while (!Check(TokenType.RightBrace) && !IsAtEnd())
            {
                // Check for compact field syntax: x, y, z: type;
                if (IsCompactFieldDeclaration())
                {
                    Console.WriteLine($"DEBUG: ParseStruct - Calling ParseCompactFieldDeclaration()");
                    members.AddRange(ParseCompactFieldDeclaration());
                    Console.WriteLine($"DEBUG: ParseStruct - ParseCompactFieldDeclaration() completed successfully");
                }
                else
                {
                    Console.WriteLine($"DEBUG: ParseStruct - Calling ParseDeclaration() for {Current().Type} '{Current().Lexeme}'");
                    members.Add((Declaration)ParseDeclaration());
                    Console.WriteLine($"DEBUG: ParseStruct - ParseDeclaration() completed");
                }
            }
            
            // Restore the original syntax level
            _currentSyntaxLevel = savedSyntaxLevel;

            Consume(TokenType.RightBrace, "Expected '}' after struct body.");

            return new StructDeclaration(Previous(), name, interfaces, members, typeParameters, modifiers);
        }

        private StructDeclaration ParseUnion(List<Modifier> modifiers)
        {
            var name = Consume(TokenType.Identifier, "Expected union name.");
            var typeParameters = ParseTypeParameters();

            var interfaces = new List<TypeNode>();
            if (Match(TokenType.Colon))
            {
                do
                {
                    interfaces.Add(ParseType());
                } while (Match(TokenType.Comma));
            }

            Consume(TokenType.LeftBrace, "Expected '{' before union body.");

            var members = new List<Declaration>();

            // Store current syntax level and temporarily switch to Medium for union body parsing
            // This ensures field parsing doesn't get routed through low-level statement parsing
            var savedSyntaxLevel = _currentSyntaxLevel;
            _currentSyntaxLevel = SyntaxLevel.Medium;

            while (!Check(TokenType.RightBrace) && !IsAtEnd())
            {
                // Check for compact field syntax: x, y, z: type;
                if (IsCompactFieldDeclaration())
                {
                    members.AddRange(ParseCompactFieldDeclaration());
                }
                // Check for function declarations within union
                else if (Match(TokenType.Function))
                {
                    members.Add(ParseFunctionDeclaration(new List<Modifier>(), null));
                }
                else
                {
                    members.Add((Declaration)ParseDeclaration());
                }
            }

            // Restore the original syntax level
            _currentSyntaxLevel = savedSyntaxLevel;

            Consume(TokenType.RightBrace, "Expected '}' after union body.");

            // For now, we'll represent unions as structs since they have similar structure
            // In a real implementation, you'd want a separate UnionDeclaration AST node
            return new StructDeclaration(Previous(), name, interfaces, members, typeParameters, modifiers);
        }

        private bool IsCompactFieldDeclaration()
        {
            // Check: identifier or certain keywords followed by comma or colon suggests compact field
            // Allow keywords like 'length', 'width', 'area', 'pin' etc. to be used as field names
            if (Check(TokenType.Identifier) || Check(TokenType.Length) || Check(TokenType.Width) || 
                Check(TokenType.Area) || Check(TokenType.Pin))
            {
                var current = Current();
                var next = PeekNext();
                Console.WriteLine($"DEBUG: IsCompactFieldDeclaration - Current: {current?.Lexeme} ({current?.Type}), Next: {next?.Lexeme} ({next?.Type})");
                
                if (next?.Type == TokenType.Comma || next?.Type == TokenType.Colon)
                {
                    Console.WriteLine($"DEBUG: IsCompactFieldDeclaration - MATCH! Returning true");
                    return true;
                }
            }
            
            Console.WriteLine($"DEBUG: IsCompactFieldDeclaration - NO MATCH, returning false");
            return false;
        }
        
        private List<Declaration> ParseCompactFieldDeclaration()
        {
            var declarations = new List<Declaration>();
            var fieldNames = new List<Token>();
            
            // Parse comma-separated field names: x, y, z OR single field name: magic, length, etc.
            // Accept identifiers and certain keywords as field names
            fieldNames.Add(ConsumeFieldName("Expected field name"));
            
            while (Match(TokenType.Comma))
            {
                fieldNames.Add(ConsumeFieldName("Expected field name after comma"));
            }
            
            // Parse type after colon: : float OR : u32
            Consume(TokenType.Colon, "Expected ':' after field names");
            var fieldType = ParseType();
            
            // Consume semicolon that terminates the compact field declaration
            Consume(TokenType.Semicolon, "Expected ';' after compact field declaration");
            
            // Create individual field declarations for each name
            foreach (var fieldName in fieldNames)
            {
                var field = new FieldDeclaration(fieldName, fieldType, null, new List<Modifier>());
                declarations.Add(field);
            }
            
            return declarations;
        }
        
        private Token ConsumeFieldName(string message)
        {
            // Accept identifiers and certain keywords as field names
            if (Check(TokenType.Identifier) || Check(TokenType.Length) || Check(TokenType.Width) || 
                Check(TokenType.Area) || Check(TokenType.Numbers) || Check(TokenType.Pin))
            {
                return Advance();
            }
            
            throw Error(Current(), message);
        }

        private EnumDeclaration ParseEnum(List<Modifier> modifiers)
        {
            var name = Consume(TokenType.Identifier, "Expected enum name.");

            TypeNode underlyingType = null;
            if (Match(TokenType.Colon))
            {
                underlyingType = ParseType();
            }

            Consume(TokenType.LeftBrace, "Expected '{' before enum body.");

            var members = new List<EnumMember>();
            do
            {
                var memberName = Consume(TokenType.Identifier, "Expected enum member name.");
                Expression value = null;

                if (Match(TokenType.Assign))
                {
                    value = ParseExpression();
                }

                members.Add(new EnumMember(memberName.Lexeme, value));
            } while (Match(TokenType.Comma) && !Check(TokenType.RightBrace));

            Consume(TokenType.RightBrace, "Expected '}' after enum body.");

            return new EnumDeclaration(Previous(), name, underlyingType, members, modifiers);
        }

        private DomainDeclaration ParseDomain(List<Modifier> modifiers)
        {
            Console.WriteLine($"DEBUG: ParseDomain() called");
            var name = Consume(TokenType.Identifier, "Expected domain name.");
            Console.WriteLine($"DEBUG: ParseDomain() - domain name: {name.Lexeme}");
            
            Console.WriteLine($"DEBUG: ParseDomain() - expecting left brace, current token: {Current().Type} '{Current().Lexeme}'");
            Consume(TokenType.LeftBrace, "Expected '{' before domain body.");
            Console.WriteLine($"DEBUG: ParseDomain() - found left brace, starting body parsing");

            var members = new List<Statement>();
            while (!Check(TokenType.RightBrace) && !IsAtEnd())
            {
                Console.WriteLine($"DEBUG: ParseDomain - parsing member at {Current().Type} '{Current().Lexeme}' line {Current().Line}");
                // Parse domain members (operator bindings, constants, etc.)
                if (IsDomainOperatorMapping())
                {
                    Console.WriteLine($"DEBUG: ParseDomain - detected operator mapping");
                    // Parse operator mapping: "× means cross_product for Vector3;"
                    var mapping = ParseDomainOperatorMapping();
                    members.Add(mapping);
                }
                else if (Check(TokenType.Const))
                {
                    Console.WriteLine($"DEBUG: ParseDomain - detected const declaration");
                    // Parse constant declarations within domains
                    var constDecl = ParseConstDeclaration();
                    members.Add(constDecl);
                }
                else
                {
                    Console.WriteLine($"DEBUG: ParseDomain - parsing as regular statement");
                    // Parse regular statements
                    var member = ParseStatement();
                    members.Add(member);
                }
            }

            Console.WriteLine($"DEBUG: ParseDomain() - expecting right brace, current token: {Current().Type} '{Current().Lexeme}'");
            Consume(TokenType.RightBrace, "Expected '}' after domain body.");
            
            Console.WriteLine($"DEBUG: ParseDomain() - completed successfully with {members.Count} members");
            return new DomainDeclaration(Previous(), name, members, modifiers);
        }

        private NamespaceDeclaration ParseModule(List<Modifier> modifiers)
        {
            Console.WriteLine($"DEBUG: ParseModule() called");
            var name = Consume(TokenType.Identifier, "Expected module name.");
            Console.WriteLine($"DEBUG: ParseModule() - module name: {name.Lexeme}");
            
            Consume(TokenType.LeftBrace, "Expected '{' before module body.");
            Console.WriteLine($"DEBUG: ParseModule() - found left brace, starting body parsing");

            var members = new List<Declaration>();
            while (!Check(TokenType.RightBrace) && !IsAtEnd())
            {
                Console.WriteLine($"DEBUG: ParseModule - parsing member at {Current().Type} '{Current().Lexeme}' line {Current().Line}");
                try
                {
                    var member = (Declaration)ParseDeclaration();
                    members.Add(member);
                }
                catch (ParseException ex)
                {
                    Console.WriteLine($"DEBUG: ParseModule - error parsing member: {ex.Message}");
                    throw;
                }
            }

            Console.WriteLine($"DEBUG: ParseModule() - expecting right brace, current token: {Current().Type} '{Current().Lexeme}'");
            Consume(TokenType.RightBrace, "Expected '}' after module body.");
            
            Console.WriteLine($"DEBUG: ParseModule() - completed successfully with {members.Count} members");
            // Use NamespaceDeclaration to represent modules since they have similar structure
            // Convert Declaration list to Statement list since modules can contain both
            var statements = new List<Statement>();
            foreach (var member in members)
            {
                statements.Add(member);
            }
            return new NamespaceDeclaration(Previous(), name.Lexeme, statements);
        }

        private FieldDeclaration ParseRustStyleVariableDeclaration(List<Modifier> modifiers)
        {
            Console.WriteLine($"DEBUG: ParseRustStyleVariableDeclaration() called");
            Console.WriteLine($"DEBUG: Current token: {Current().Type} '{Current().Lexeme}'");
            
            Token name;
            
            // Handle two cases:
            // 1. Current token is identifier: IDENTIFIER: Type = value;
            // 2. Current token is colon (identifier already consumed): : Type = value;
            if (Check(TokenType.Identifier))
            {
                // Normal case - identifier is current token
                name = Advance();
                Console.WriteLine($"DEBUG: ParseRustStyleVariableDeclaration() - parsed variable name: {name.Lexeme}");
                Consume(TokenType.Colon, "Expected ':' after variable name.");
            }
            else if (Check(TokenType.Colon))
            {
                // Alternative case - identifier was already consumed, get it from previous token
                var previousToken = PeekPrevious();
                if (previousToken != null && previousToken.Type == TokenType.Identifier)
                {
                    name = previousToken;
                    Console.WriteLine($"DEBUG: ParseRustStyleVariableDeclaration() - using previously consumed variable name: {name.Lexeme}");
                    Advance(); // consume the colon
                }
                else
                {
                    throw Error(Current(), "Expected variable name before ':'.");
                }
            }
            else
            {
                throw Error(Current(), "Expected variable name or ':' in Rust-style variable declaration.");
            }
            
            var type = ParseType();
            Console.WriteLine($"DEBUG: ParseRustStyleVariableDeclaration() - type: {type.Name}");
            
            Expression initializer = null;
            if (Match(TokenType.Assign))
            {
                Console.WriteLine($"DEBUG: ParseRustStyleVariableDeclaration() - parsing initializer");
                initializer = ParseExpression();
            }
            
            Consume(TokenType.Semicolon, "Expected ';' after variable declaration.");
            
            Console.WriteLine($"DEBUG: ParseRustStyleVariableDeclaration() - completed: {type.Name} {name.Lexeme}");
            
            // For module-level declarations (like static variables), return a FieldDeclaration 
            // instead of VariableDeclaration since modules expect Declaration types
            return new FieldDeclaration(name, type, initializer, modifiers);
        }

        private FieldDeclaration ParseConstDeclaration()
        {
            // Parse: const identifier = expression;
            Consume(TokenType.Const, "Expected 'const' keyword.");
            
            // Handle mathematical Unicode identifiers like ε₀, μ₀, etc.
            // Some Unicode characters might be tokenized as multiple tokens, so we need to handle this
            Token name;
            
            // Handle Greek letters and math symbols that may be followed by subscripts/superscripts
            if (IsGreekLetterOrMathSymbol(Current().Type))
            {
                var nameBuilder = new StringBuilder();
                var startToken = Current();
                
                // Start with the Greek letter or math symbol
                nameBuilder.Append(Advance().Lexeme);
                
                // Continue collecting subscripts, superscripts, or other identifier parts
                while (!Check(TokenType.Assign) && !IsAtEnd() && !Check(TokenType.Semicolon) && !Check(TokenType.RightBrace))
                {
                    var currentToken = Current();
                    
                    // If we hit a clear non-identifier token (except subscripts/superscripts), stop
                    if (currentToken.Type == TokenType.Means || currentToken.Type == TokenType.For ||
                        currentToken.Type == TokenType.Const || currentToken.Type == TokenType.LeftBrace)
                        break;
                        
                    // Check if this looks like a subscript/superscript or continuation of identifier
                    var nextLexeme = currentToken.Lexeme;
                    if (nextLexeme.Length > 0)
                    {
                        var firstChar = nextLexeme[0];
                        // Include subscripts (₀₁₂₃...), superscripts, and other identifier chars
                        if (char.IsLetterOrDigit(firstChar) || 
                            (firstChar >= '\u2080' && firstChar <= '\u208E') || // Subscripts
                            (firstChar >= '\u2070' && firstChar <= '\u209F') || // Superscripts  
                            firstChar == '_' ||
                            char.GetUnicodeCategory(firstChar) == UnicodeCategory.NonSpacingMark ||
                            char.GetUnicodeCategory(firstChar) == UnicodeCategory.ModifierSymbol)
                        {
                            nameBuilder.Append(Advance().Lexeme);
                        }
                        else
                        {
                            break;
                        }
                    }
                    else
                    {
                        break;
                    }
                }
                
                // Create a synthetic identifier token with the combined name
                name = new Token(TokenType.Identifier, nameBuilder.ToString(), nameBuilder.ToString(),
                    startToken.Line, startToken.Column, startToken.StartPosition, startToken.EndPosition,
                    startToken.FileName, startToken.SyntaxLevel);
            }
            else if (Check(TokenType.Identifier))
            {
                name = Advance();
            }
            else
            {
                // For other complex Unicode identifiers that may be split across tokens,
                // combine tokens until we hit the '=' sign
                var nameBuilder = new StringBuilder();
                var startToken = Current();
                
                while (!Check(TokenType.Assign) && !IsAtEnd() && !Check(TokenType.Semicolon) && !Check(TokenType.RightBrace))
                {
                    var token = Advance();
                    nameBuilder.Append(token.Lexeme);
                    
                    // Stop if we've collected a reasonable identifier and the next is '='
                    if (nameBuilder.Length > 0 && Check(TokenType.Assign))
                        break;
                }
                
                // Create a synthetic identifier token
                name = new Token(TokenType.Identifier, nameBuilder.ToString(), nameBuilder.ToString(),
                    startToken.Line, startToken.Column, startToken.StartPosition, startToken.EndPosition,
                    startToken.FileName, startToken.SyntaxLevel);
            }
            
            Consume(TokenType.Assign, "Expected '=' after constant name.");
            var initializer = ParseExpression();
            Consume(TokenType.Semicolon, "Expected ';' after constant declaration.");
            
            // Create a field declaration with const modifier
            // Infer type from the initializer expression
            var constType = InferTypeFromExpression(initializer);
            var modifiers = new List<Modifier> { Modifier.Const };
            return new FieldDeclaration(name, constType, initializer, modifiers);
        }

        private MacroDeclaration ParseMacroDeclaration()
        {
            // Parse: macro name(parameters...) { body }
            var macroToken = Consume(TokenType.Macro, "Expected 'macro' keyword.");
            var nameToken = ConsumeIdentifier("Expected macro name.");
            
            // Parse parameter list
            Consume(TokenType.LeftParen, "Expected '(' after macro name.");
            var parameters = new List<Parameter>();
            
            if (!Check(TokenType.RightParen))
            {
                do
                {
                    var paramName = ConsumeIdentifier("Expected parameter name.");
                    
                    // Check for variadic parameter syntax: args...
                    var modifier = ParameterModifier.None;
                    if (Match(TokenType.Spread))
                    {
                        modifier = ParameterModifier.Params;
                    }
                    
                    // Check for default value (but not for variadic parameters)
                    Expression? defaultValue = null;
                    if (modifier != ParameterModifier.Params && Match(TokenType.Assign))
                    {
                        defaultValue = ParseAssignment();
                    }
                    
                    // Macro parameters are untyped, so use "var" as type
                    parameters.Add(new Parameter(new TypeNode("var"), paramName.Lexeme, defaultValue, modifier));
                }
                while (Match(TokenType.Comma));
            }
            
            Consume(TokenType.RightParen, "Expected ')' after macro parameters.");
            
            // Parse macro body
            var body = ParseBlock();
            
            return new MacroDeclaration(macroToken, nameToken, parameters, body);
        }

        private TraitDeclaration ParseTraitDeclaration()
        {
            // Parse: trait Name<T> { members... }
            var traitToken = Consume(TokenType.Identifier, "Expected 'trait' keyword."); // Since trait is an identifier
            var nameToken = ConsumeIdentifier("Expected trait name.");
            
            // Parse optional type parameters
            var typeParameters = new List<TypeParameter>();
            if (Match(TokenType.Less))
            {
                do
                {
                    var paramName = ConsumeIdentifier("Expected type parameter name.");
                    var constraints = new List<TypeNode>();
                    
                    // Parse type parameter constraints if present (e.g., T: Numeric)
                    if (Match(TokenType.Colon))
                    {
                        do
                        {
                            var constraint = ParseType();
                            constraints.Add(constraint);
                        }
                        while (Match(TokenType.Plus)); // Support multiple constraints with +
                    }
                    
                    typeParameters.Add(new TypeParameter(paramName.Lexeme, constraints));
                }
                while (Match(TokenType.Comma));
                
                Consume(TokenType.Greater, "Expected '>' after type parameters.");
            }
            
            // Parse trait body
            Consume(TokenType.LeftBrace, "Expected '{' before trait body.");
            
            var members = new List<Declaration>();
            while (!Check(TokenType.RightBrace) && !IsAtEnd())
            {
                // Parse trait members: method signatures, associated types, etc.
                try
                {
                    // Parse method signature (no body in traits)
                    if (Check(TokenType.Function))
                    {
                        Advance(); // consume 'function'
                        var methodName = ConsumeIdentifier("Expected method name.");
                        Consume(TokenType.LeftParen, "Expected '(' after method name.");
                        var parameters = ParseParameters();
                        
                        // Parse return type
                        TypeNode returnType = new TypeNode("void");
                        if (Match(TokenType.Colon))
                        {
                            returnType = ParseType();
                        }
                        
                        Consume(TokenType.Semicolon, "Expected ';' after trait method signature.");
                        
                        // Create a function declaration without body for trait
                        var methodDecl = new FunctionDeclaration(
                            methodName,
                            returnType,
                            parameters,
                            null, // No body in trait
                            new List<TypeParameter>(),
                            false,
                            new List<Modifier> { Modifier.Abstract }
                        );
                        members.Add(methodDecl);
                    }
                    else
                    {
                        // Skip unknown trait member
                        while (!Check(TokenType.Semicolon) && !Check(TokenType.RightBrace) && !IsAtEnd())
                        {
                            Advance();
                        }
                        if (Check(TokenType.Semicolon)) Advance();
                    }
                }
                catch (ParseException)
                {
                    // Skip to next trait member on error
                    while (!Check(TokenType.Semicolon) && !Check(TokenType.RightBrace) && !IsAtEnd())
                    {
                        Advance();
                    }
                    if (Check(TokenType.Semicolon)) Advance();
                }
            }
            
            Consume(TokenType.RightBrace, "Expected '}' after trait body.");
            
            return new TraitDeclaration(traitToken, nameToken, typeParameters, members);
        }

        private ImplementDeclaration ParseImplementDeclaration()
        {
            // Parse: implement TraitName<T> for TargetType { members... }
            // or: implement TraitName<T> { members... } (for direct trait implementation)
            Console.WriteLine($"DEBUG: ParseImplementDeclaration - Starting at line {Current().Line}");
            var implementToken = Consume(TokenType.Identifier, "Expected 'implement' keyword."); // Since implement is an identifier
            
            // Parse the trait type (e.g., Numeric<i32>)
            var traitType = ParseType();
            
            // Check for 'for' keyword (trait implementation for a specific type)
            TypeNode? targetType = null;
            if (Check(TokenType.Identifier) && Current().Lexeme == "for")
            {
                Advance(); // consume 'for'
                targetType = ParseType();
            }
            
            // Parse the implementation body
            Consume(TokenType.LeftBrace, "Expected '{' before implement body.");
            
            var members = new List<Declaration>();
            while (!Check(TokenType.RightBrace) && !IsAtEnd())
            {
                // Parse trait implementation members: operators and methods
                try
                {
                    var modifiers = ParseModifiers();
                    
                    if (Check(TokenType.Identifier) && Current().Lexeme == "operator")
                    {
                        // Parse operator implementation
                        modifiers.Add(Modifier.Operator);
                        Advance(); // consume "operator"
                        var operatorDecl = ParseOperatorOverload(modifiers);
                        members.Add(operatorDecl);
                    }
                    else if (Check(TokenType.Function))
                    {
                        // Parse method implementation
                        var methodDecl = ParseFunctionDeclaration(modifiers);
                        members.Add(methodDecl);
                    }
                    else
                    {
                        // Skip unknown implementation member  
                        // Handle nested braces properly for function bodies
                        int braceDepth = 0;
                        while (!IsAtEnd())
                        {
                            if (Check(TokenType.LeftBrace))
                            {
                                braceDepth++;
                                Advance();
                            }
                            else if (Check(TokenType.RightBrace))
                            {
                                if (braceDepth == 0)
                                {
                                    // This is the end of the implement block
                                    break;
                                }
                                else
                                {
                                    braceDepth--;
                                    Advance();
                                }
                            }
                            else if (Check(TokenType.Semicolon) && braceDepth == 0)
                            {
                                Advance(); // consume semicolon
                                break; // End of this member
                            }
                            else
                            {
                                Advance();
                            }
                        }
                    }
                }
                catch (ParseException)
                {
                    // Skip to next member on error
                    int braceDepth = 0;
                    while (!IsAtEnd())
                    {
                        if (Check(TokenType.LeftBrace))
                        {
                            braceDepth++;
                            Advance();
                        }
                        else if (Check(TokenType.RightBrace))
                        {
                            if (braceDepth == 0) break;
                            else
                            {
                                braceDepth--;
                                Advance();
                            }
                        }
                        else if (Check(TokenType.Semicolon) && braceDepth == 0)
                        {
                            Advance();
                            break;
                        }
                        else
                        {
                            Advance();
                        }
                    }
                }
            }
            
            Consume(TokenType.RightBrace, "Expected '}' after implement body.");
            
            Console.WriteLine($"DEBUG: ParseImplementDeclaration - Finished at line {Current().Line}");
            return new ImplementDeclaration(implementToken, traitType, targetType, members);
        }

        private FunctionDeclaration ParseDestructorDeclaration()
        {
            // Parse: destructor { body }
            Console.WriteLine($"DEBUG: ParseDestructorDeclaration - Starting at line {Current().Line}");
            var destructorToken = Consume(TokenType.Identifier, "Expected 'destructor' keyword."); // Since destructor is an identifier
            
            // Destructors have no parameters and no return type
            var parameters = new List<Parameter>();
            var returnType = new TypeNode("void");
            
            // Parse the destructor body
            var body = ParseBlock();
            
            Console.WriteLine($"DEBUG: ParseDestructorDeclaration - Finished at line {Current().Line}");
            
            // Create a function declaration with a special destructor name
            return new FunctionDeclaration(
                new Token(TokenType.Identifier, "~destructor", null, destructorToken.Line, destructorToken.Column, 0, 0, "", _currentSyntaxLevel),
                returnType,
                parameters,
                body,
                modifiers: new List<Modifier> { Modifier.Public }
            );
        }

        private NamespaceDeclaration ParseExternBlock()
        {
            // Parse: extern { function declarations... }
            Console.WriteLine($"DEBUG: ParseExternBlock - Starting at line {Current().Line}");
            var externToken = Consume(TokenType.Identifier, "Expected 'extern' keyword."); // Since extern is an identifier
            
            Consume(TokenType.LeftBrace, "Expected '{' after extern.");
            
            var members = new List<Statement>();
            while (!Check(TokenType.RightBrace) && !IsAtEnd())
            {
                // Skip attributes that might be on extern function declarations
                SkipAttributes();
                
                if (Check(TokenType.Function))
                {
                    // Parse extern function declaration
                    var externFunc = ParseFunctionDeclaration(new List<Modifier>());
                    members.Add(externFunc);
                }
                else
                {
                    // Skip unknown content in extern blocks for now
                    Console.WriteLine($"DEBUG: ParseExternBlock - Skipping {Current().Type} '{Current().Lexeme}'");
                    Advance();
                }
            }
            
            Consume(TokenType.RightBrace, "Expected '}' after extern block.");
            
            Console.WriteLine($"DEBUG: ParseExternBlock - Finished at line {Current().Line}");
            
            // Use NamespaceDeclaration to represent extern blocks
            return new NamespaceDeclaration(externToken, "extern", members);
        }

        private bool IsDomainOperatorMapping()
        {
            // Check if current statement is an operator mapping like "× means cross_product"
            // Look ahead to see if pattern is: [symbol] "means" [identifier]
            var savedPosition = _current;
            
            try
            {
                // Skip the operator symbol (could be various mathematical symbols)
                if (IsAtEnd()) return false;
                Console.WriteLine($"DEBUG: IsDomainOperatorMapping() checking {Current().Type} '{Current().Lexeme}'");
                Advance();
                
                // Check if followed by "means"
                if (Check(TokenType.Means))
                {
                    Console.WriteLine($"DEBUG: IsDomainOperatorMapping() - Found 'means' keyword, returning true");
                    return true;
                }
                
                Console.WriteLine($"DEBUG: IsDomainOperatorMapping() - Next token is {Current().Type} '{Current().Lexeme}', returning false");
                return false;
            }
            finally
            {
                _current = savedPosition;
            }
        }

        private Statement ParseDomainOperatorMapping()
        {
            // Parse: "× means cross_product for Vector3;"
            // or: "∇ means gradient_operator;"
            
            var operatorSymbol = Advance(); // Consume the operator symbol
            
            // Consume "means" keyword
            if (!Check(TokenType.Means))
            {
                throw Error(Current(), "Expected 'means' after operator symbol in domain mapping.");
            }
            Advance(); // consume "means"
            
            // Parse function name
            var functionName = Consume(TokenType.Identifier, "Expected function name after 'means'.");
            
            // Parse optional "for Type" clause
            Token? typeName = null;
            if (Check(TokenType.For))
            {
                Advance(); // consume "for"
                typeName = Consume(TokenType.Identifier, "Expected type name after 'for'.");
            }
            
            Consume(TokenType.Semicolon, "Expected ';' after domain operator mapping.");
            
            // For now, create an expression statement to represent this
            // In a full implementation, this would be a custom AST node type
            var mappingToken = new Token(TokenType.StringLiteral, 
                $"operator_mapping: {operatorSymbol.Lexeme} -> {functionName.Lexeme}" + 
                (typeName != null ? $" for {typeName.Lexeme}" : ""),
                $"operator_mapping: {operatorSymbol.Lexeme} -> {functionName.Lexeme}" + 
                (typeName != null ? $" for {typeName.Lexeme}" : ""),
                operatorSymbol.Line, operatorSymbol.Column, operatorSymbol.StartPosition, operatorSymbol.EndPosition,
                operatorSymbol.FileName, operatorSymbol.SyntaxLevel);
            
            var mappingExpr = new LiteralExpression(mappingToken);
            
            return new ExpressionStatement(mappingExpr);
        }

        private FunctionDeclaration ParseFunction(TypeNode returnType, Token name, List<Modifier> modifiers, List<TypeParameter> typeParameters = null)
        {
            // Consume the opening parenthesis
            Consume(TokenType.LeftParen, "Expected '(' before parameters.");
            
            var parameters = ParseParameters();
            // Type parameters are now passed in from ParseDeclaration
            if (typeParameters == null)
            {
                typeParameters = new List<TypeParameter>();
            }

            BlockStatement body = null;
            if (Match(TokenType.DoubleArrow))
            {
                // Expression-bodied function
                var expr = ParseExpression();
                body = new BlockStatement(new List<Statement> 
                { 
                    new ReturnStatement(Previous(), expr) 
                });
                Consume(TokenType.Semicolon, "Expected ';' after expression body.");
            }
            else
            {
                Consume(TokenType.LeftBrace, "Expected '{' before function body.");
                // The current syntax level should be preserved for parsing the body
                body = ParseBlock();
            }

            bool isAsync = modifiers.Contains(Modifier.Async);
            return new FunctionDeclaration(name, returnType, parameters, body, typeParameters, isAsync, modifiers);
        }

        private FunctionDeclaration ParseFunctionWithSyntaxLevel(TypeNode returnType, Token name, List<Modifier> modifiers, SyntaxLevel? scopedSyntaxLevel, List<TypeParameter> typeParameters = null)
        {
            // Store the current syntax level to restore later
            var previousLevel = _currentSyntaxLevel;
            
            // If this function has a scoped syntax level, apply it
            if (scopedSyntaxLevel.HasValue)
            {
                _currentSyntaxLevel = scopedSyntaxLevel.Value;
            }

            try
            {
                // Consume the opening parenthesis
                Consume(TokenType.LeftParen, "Expected '(' before parameters.");
                
                var parameters = ParseParameters();
                // Type parameters are now passed in from ParseDeclaration
                if (typeParameters == null)
                {
                    typeParameters = new List<TypeParameter>();
                }

                BlockStatement body = null;
                if (Match(TokenType.DoubleArrow))
                {
                    // Expression-bodied function
                    var expr = ParseExpression();
                    body = new BlockStatement(new List<Statement> 
                    { 
                        new ReturnStatement(Previous(), expr) 
                    });
                    Consume(TokenType.Semicolon, "Expected ';' after expression body.");
                }
                else
                {
                    Consume(TokenType.LeftBrace, "Expected '{' before function body.");
                    // The current syntax level is maintained for parsing the body
                    body = ParseBlock();
                }

                bool isAsync = modifiers.Contains(Modifier.Async);
                return new FunctionDeclaration(name, returnType, parameters, body, typeParameters, isAsync, modifiers);
            }
            finally
            {
                // Restore the previous syntax level
                _currentSyntaxLevel = previousLevel;
            }
        }

        private FunctionDeclaration ParseFunctionDeclaration(List<Modifier> modifiers, SyntaxLevel? scopedSyntaxLevel = null)
        {
            // Store the current syntax level to restore later
            var previousLevel = _currentSyntaxLevel;
            
            // If this function has a scoped syntax level, apply it
            if (scopedSyntaxLevel.HasValue)
            {
                _currentSyntaxLevel = scopedSyntaxLevel.Value;
            }

            try
            {
                // Parse Ouroboros syntax: function FunctionName(): ReturnType {
                // Allow 'new' as a function name (for constructors) in addition to regular identifiers
                Token name;
                if (Check(TokenType.Identifier))
                {
                    name = Advance();
                }
                else if (Check(TokenType.New))
                {
                    name = Advance();
                }
                else
                {
                    throw Error(Current(), "Expected function name.");
                }
                
                // Parse parameters
                Consume(TokenType.LeftParen, "Expected '(' after function name.");
                
                // Check if we should use Ouroboros-style parameters (name: type) or C# style (type name)
                // Peek ahead to see if we have identifier followed by colon
                List<Parameter> parameters;
                if (!Check(TokenType.RightParen) && Current().Type == TokenType.Identifier && PeekNext()?.Type == TokenType.Colon)
                {
                    // Ouroboros style: name: type
                    parameters = ParseOuroborosParameters();
                }
                else
                {
                    // C# style: type name
                    parameters = ParseParameters();
                }
                
                // Parse return type - support both ':' and '->' syntax
                TypeNode returnType;
                if (Match(TokenType.Colon))
                {
                    // Ouroboros syntax: function name(): type
                    returnType = ParseType();
                }
                else if (Check(TokenType.Arrow))
                {
                    // Arrow syntax: function name() -> type (using correct TokenType.Arrow)
                    Advance(); // consume the ->
                    returnType = ParseType();
                }
                else
                {
                    // Default to void if no return type specified
                    returnType = new TypeNode("void");
                }
                
                // Parse type parameters (if any) - for generic functions
                var typeParameters = new List<TypeParameter>();
                
                // Parse function body - this will maintain the current syntax level
                BlockStatement body = null;
                if (Match(TokenType.DoubleArrow))
                {
                    // Expression-bodied function
                    var expr = ParseExpression();
                    body = new BlockStatement(new List<Statement> 
                    { 
                        new ReturnStatement(Previous(), expr) 
                    });
                    Consume(TokenType.Semicolon, "Expected ';' after expression body.");
                }
                else
                {
                    Consume(TokenType.LeftBrace, "Expected '{' before function body.");
                    // The current syntax level is maintained for parsing the body
                    body = ParseBlock();
                }

                bool isAsync = modifiers.Contains(Modifier.Async);
                return new FunctionDeclaration(name, returnType, parameters, body, typeParameters, isAsync, modifiers);
            }
            finally
            {
                // Restore the previous syntax level
                _currentSyntaxLevel = previousLevel;
            }
        }

        private FieldDeclaration ParseField(TypeNode type, Token name, List<Modifier> modifiers)
        {
            Expression initializer = null;
            if (Match(TokenType.Assign))
            {
                initializer = ParseExpression();
            }

            Consume(TokenType.Semicolon, "Expected ';' after field declaration.");

            return new FieldDeclaration(name, type, initializer, modifiers);
        }

        private FunctionDeclaration ParseOperatorOverload(List<Modifier> modifiers)
        {
            // Store the current syntax level to restore later
            var previousLevel = _currentSyntaxLevel;
            
            try
            {
                // The operator overload should use the current syntax level (e.g., @low)
                // No need to change _currentSyntaxLevel here - it should already be set correctly
                
                // Parse the operator symbol: [], +, -, *, /, etc.
                Token operatorToken;
                string operatorName;
                
                if (Match(TokenType.LeftBracket))
                {
                    // Array indexer operator []
                    Consume(TokenType.RightBracket, "Expected ']' after '[' for indexer operator.");
                    operatorName = "op_Index";
                    operatorToken = Previous();
                }
                else if (Match(TokenType.Plus))
                {
                    operatorName = "op_Addition";
                    operatorToken = Previous();
                }
                else if (Match(TokenType.Minus))
                {
                    operatorName = "op_Subtraction";
                    operatorToken = Previous();
                }
                else if (Match(TokenType.Multiply))
                {
                    operatorName = "op_Multiply";
                    operatorToken = Previous();
                }
                else if (Match(TokenType.Divide))
                {
                    operatorName = "op_Division";
                    operatorToken = Previous();
                }
                else if (Match(TokenType.Modulo))
                {
                    operatorName = "op_Modulus";
                    operatorToken = Previous();
                }
                else if (Match(TokenType.Equal))
                {
                    operatorName = "op_Equality";
                    operatorToken = Previous();
                }
                else if (Match(TokenType.NotEqual))
                {
                    operatorName = "op_Inequality";
                    operatorToken = Previous();
                }
                else if (Match(TokenType.Less))
                {
                    operatorName = "op_LessThan";
                    operatorToken = Previous();
                }
                else if (Match(TokenType.Greater))
                {
                    operatorName = "op_GreaterThan";
                    operatorToken = Previous();
                }
                else if (Match(TokenType.LessEqual))
                {
                    operatorName = "op_LessThanOrEqual";
                    operatorToken = Previous();
                }
                else if (Match(TokenType.GreaterEqual))
                {
                    operatorName = "op_GreaterThanOrEqual";
                    operatorToken = Previous();
                }
                else
                {
                    throw Error(Current(), "Expected operator symbol after 'operator' keyword.");
                }
                
                // Create a synthetic name token for the operator
                var nameToken = new Token(TokenType.Identifier, operatorName, null, 
                                        operatorToken.Line, operatorToken.Column, 
                                        operatorToken.StartPosition, operatorToken.EndPosition,
                                        operatorToken.FileName, operatorToken.SyntaxLevel);
                
                // Parse parameters (Ouroboros style: name: type)
                Consume(TokenType.LeftParen, "Expected '(' after operator symbol.");
                var parameters = ParseOuroborosParameters();
                
                // Parse return type
                Consume(TokenType.Arrow, "Expected '->' before return type.");
                var returnType = ParseType();
                
                // Parse function body - preserve current syntax level for the body
                BlockStatement body;
                if (Match(TokenType.LeftBrace))
                {
                    // The current syntax level is maintained for parsing the body
                    body = ParseBlock();
                }
                else
                {
                    throw Error(Current(), "Expected '{' before operator body.");
                }
                
                // Mark as operator overload
                modifiers.Add(Modifier.Operator);
                
                return new FunctionDeclaration(nameToken, returnType, parameters, body, 
                                             new List<TypeParameter>(), false, modifiers);
            }
            finally
            {
                // Restore the previous syntax level
                _currentSyntaxLevel = previousLevel;
            }
        }

        private List<Parameter> ParseOuroborosParameters()
        {
            var parameters = new List<Parameter>();

            if (!Check(TokenType.RightParen))
            {
                do
                {
                    var modifier = ParameterModifier.None;
                    
                    // Parse parameter modifiers if any
                    if (Match(TokenType.Ref)) modifier = ParameterModifier.Ref;
                    else if (Match(TokenType.Out)) modifier = ParameterModifier.Out;
                    else if (Match(TokenType.In)) modifier = ParameterModifier.In;
                    else if (Match(TokenType.Params)) modifier = ParameterModifier.Params;

                    // Parse parameter name
                    var name = ConsumeIdentifier("Expected parameter name.");

                    // Parse colon
                    Consume(TokenType.Colon, "Expected ':' after parameter name.");
                    
                    // Parse parameter type
                    var type = ParseType();

                    Expression defaultValue = null;
                    if (Match(TokenType.Assign))
                    {
                        defaultValue = ParseAssignment();
                    }

                    parameters.Add(new Parameter(type, name.Lexeme, defaultValue, modifier));
                } while (Match(TokenType.Comma));
            }

            Consume(TokenType.RightParen, "Expected ')' after parameters.");
            return parameters;
        }

        // Data-oriented declarations
        private ComponentDeclaration ParseComponent(List<Modifier> modifiers)
        {
            var name = Consume(TokenType.Identifier, "Expected component name.");
            Consume(TokenType.LeftBrace, "Expected '{' before component body.");

            var fields = new List<FieldDeclaration>();
            while (!Check(TokenType.RightBrace) && !IsAtEnd())
            {
                var fieldModifiers = ParseModifiers();
                var type = ParseType();
                var fieldName = Consume(TokenType.Identifier, "Expected field name.");
                Expression initializer = null;
                
                if (Match(TokenType.Assign))
                {
                    initializer = ParseExpression();
                }
                
                Consume(TokenType.Semicolon, "Expected ';' after field.");
                fields.Add(new FieldDeclaration(fieldName, type, initializer, fieldModifiers));
            }

            Consume(TokenType.RightBrace, "Expected '}' after component body.");

            return new ComponentDeclaration(Previous(), name, fields, modifiers);
        }

        private SystemDeclaration ParseSystem(List<Modifier> modifiers)
        {
            var name = Consume(TokenType.Identifier, "Expected system name.");
            
            var requiredComponents = new List<TypeNode>();
            if (Match(TokenType.LeftBracket))
            {
                do
                {
                    requiredComponents.Add(ParseType());
                } while (Match(TokenType.Comma));
                
                Consume(TokenType.RightBracket, "Expected ']' after component list.");
            }

            Consume(TokenType.LeftBrace, "Expected '{' before system body.");

            var methods = new List<FunctionDeclaration>();
            while (!Check(TokenType.RightBrace) && !IsAtEnd())
            {
                var methodModifiers = ParseModifiers();
                var returnType = ParseType();
                var methodName = Consume(TokenType.Identifier, "Expected method name.");
                methods.Add(ParseFunction(returnType, methodName, methodModifiers, null));
            }

            Consume(TokenType.RightBrace, "Expected '}' after system body.");

            return new SystemDeclaration(Previous(), name, requiredComponents, methods, modifiers);
        }

        private EntityDeclaration ParseEntity(List<Modifier> modifiers)
        {
            var name = Consume(TokenType.Identifier, "Expected entity name.");
            
            var components = new List<TypeNode>();
            if (Match(TokenType.LeftBracket))
            {
                do
                {
                    components.Add(ParseType());
                } while (Match(TokenType.Comma));
                
                Consume(TokenType.RightBracket, "Expected ']' after component list.");
            }

            Consume(TokenType.Semicolon, "Expected ';' after entity declaration.");

            return new EntityDeclaration(Previous(), name, components, modifiers);
        }

        #endregion

        #region High-Level Syntax Parsing

        private Statement ParseHighLevelStatement()
        {
            // High-level parser is not available in this build, use fallback
            Console.WriteLine($"DEBUG: High-level parser not available, using fallback");
            return ParseHighLevelStatementFallback();
        }

        private Statement ParseHighLevelStatementFallback()
        {
            // Original simple high-level parsing for backward compatibility
            // Check for print statement
            if (Check(TokenType.Print))
                {
                Advance(); // consume 'print'
                var expr = ParseExpression();
                
                // Convert print to Console.WriteLine
                var printCall = new CallExpression(
                    new IdentifierExpression(
                        new Token(TokenType.Identifier, "Console.WriteLine", null, 0, 0, 0, 0, "", _currentSyntaxLevel)),
                    new List<Expression> { expr });
                
                // Semicolon is optional in high-level syntax
                Match(TokenType.Semicolon);
                
                return new ExpressionStatement(printCall);
            }
            
            // Check for natural assignment with :=
            if (Check(TokenType.Identifier) && PeekNext()?.Type == TokenType.Assign)
                {
                var nameToken = ConsumeIdentifier("Expected variable name.");
                Consume(TokenType.Assign, "Expected ':=' operator.");
                var initializerExpr = ParseExpression();
                
                // Semicolon is optional in high-level syntax
                Match(TokenType.Semicolon);
                
                    var varType = new TypeNode("var");
                return new VariableDeclaration(varType, nameToken, initializerExpr, false, false);
            }
            
            // Fall back to regular statement parsing
            var savedLevel = _currentSyntaxLevel;
            _currentSyntaxLevel = SyntaxLevel.Medium; // Temporarily switch to medium level
            var stmt = ParseStatement();
            _currentSyntaxLevel = savedLevel;
            return stmt;
        }

        private Statement ParseMediumLevelStatement()
        {
            // Temporarily use fallback parsing for medium-level syntax
                    Console.WriteLine($"DEBUG: Using enhanced medium-level parsing");
        return ParseMediumLevelStatementEnhanced();
        }
        
        private Statement ParseMediumLevelStatementFallback()
        {
            // Fallback for medium-level parsing - use standard C#-style parsing
            try
            {
                Console.WriteLine($"DEBUG: Medium fallback at {Current().Type} '{Current().Lexeme}' line {Current().Line}");
                Console.WriteLine($"DEBUG: Starting ParseMediumLevelStatementFallback processing");
                
                // Handle function declarations with modifiers (public static void etc)
                if (Check(TokenType.Public) || Check(TokenType.Private) || Check(TokenType.Protected) || 
                    Check(TokenType.Internal) || Check(TokenType.Static))
                {
                    Console.WriteLine($"DEBUG: Detected function declaration with modifiers");
                    var modifiers = ParseModifiers();
                    if (PeekType() != null || Check(TokenType.Void))
                    {
                        // This looks like a function declaration
                        var returnType = Check(TokenType.Void) ? new TypeNode("void") : ParseType();
                        if (Check(TokenType.Void)) Advance(); // consume void
                        
                        var name = ConsumeIdentifierOrGreekLetter("Expected function name.");
                        return ParseFunction(returnType, name, modifiers);
                    }
                }
                
                // Control flow
                if (Match(TokenType.If)) return ParseIfStatement();
                if (Match(TokenType.While)) return ParseWhileStatement();
                if (Match(TokenType.For)) return ParseForStatement();
                if (Match(TokenType.Loop)) return ParseLoopStatement();
                if (Match(TokenType.Return)) return ParseReturnStatement();
                if (Match(TokenType.Break)) return ParseBreakStatement();
                if (Match(TokenType.Continue)) return ParseContinueStatement();
                if (Match(TokenType.Throw)) return ParseThrowStatement();
                if (Match(TokenType.Try)) return ParseTryStatement();
                if (Match(TokenType.Unsafe)) return ParseUnsafeStatement();
                if (Match(TokenType.LeftBrace)) return ParseBlock();
                
                // Handle print statements (medium-level can be more lenient about semicolons)
                if (Match(TokenType.Print))
                {
                    Console.WriteLine($"DEBUG: Processing print statement in fallback");
                    var printToken = Previous();
                    Console.WriteLine($"DEBUG: About to parse expression after print token");
                    
                    try
                    {
                    var expr = ParseAssignment();
                        Console.WriteLine($"DEBUG: Successfully parsed expression after print: {expr?.GetType().Name}");
                    
                    // Convert print to Console.WriteLine call
                    var printCall = new CallExpression(
                        new IdentifierExpression(
                            new Token(TokenType.Identifier, "Console.WriteLine", null, printToken.Line, 
                                     printToken.Column, printToken.StartPosition, printToken.EndPosition, 
                                     printToken.FileName, _currentSyntaxLevel)),
                        new List<Expression> { expr });
                    
                        // Semicolon is optional for print statements in medium level
                        bool hasSemicolon = Match(TokenType.Semicolon); // Just consume if present, don't require
                        Console.WriteLine($"DEBUG: Print statement processing complete, semicolon: {hasSemicolon}");
                    return new ExpressionStatement(printCall);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"DEBUG: Exception parsing expression in print statement: {ex.Message}");
                        Console.WriteLine($"DEBUG: Current token when error occurred: {Current().Type} '{Current().Lexeme}' at line {Current().Line}");
                        
                        // Try to create a simple string literal as fallback
                        var fallbackExpr = new LiteralExpression(
                            new Token(TokenType.StringLiteral, "\"[Parse Error]\"", "[Parse Error]", printToken.Line, 
                                     printToken.Column, printToken.StartPosition, printToken.EndPosition, 
                                     printToken.FileName, _currentSyntaxLevel));
                        
                        var printCall = new CallExpression(
                            new IdentifierExpression(
                                new Token(TokenType.Identifier, "Console.WriteLine", null, printToken.Line, 
                                         printToken.Column, printToken.StartPosition, printToken.EndPosition, 
                                         printToken.FileName, _currentSyntaxLevel)),
                            new List<Expression> { fallbackExpr });
                        
                        // Skip to next statement by advancing past the problematic tokens
                        while (!IsAtEnd() && !Check(TokenType.Semicolon) && !Check(TokenType.RightBrace) && Current().Line == printToken.Line)
                        {
                            Advance();
                        }
                        Match(TokenType.Semicolon); // Consume semicolon if present
                        
                        Console.WriteLine($"DEBUG: Print statement fallback processing complete");
                        return new ExpressionStatement(printCall);
                    }
                }

                // Function declaration with 'function' keyword
                if (Match(TokenType.Function))
                {
                    return ParseFunctionDeclaration(new List<Modifier>(), null);
                }
                
                // Import statements
                if (Match(TokenType.Import))
                {
                    Console.WriteLine($"DEBUG: Processing import statement in fallback");
                    return ParseImport();
                }
                
                // Using statements (includes imports)
                if (Match(TokenType.Using))
                {
                    Console.WriteLine($"DEBUG: Processing using statement in fallback");
                    return ParseUsing();
                }
                
                // Class declarations
                if (Match(TokenType.Class))
                {
                    Console.WriteLine($"DEBUG: Processing class declaration in fallback");
                    return ParseClass(new List<Modifier>());
                }
                
                // Function declaration without modifiers (void, int, etc.)
                if (Check(TokenType.Void) || PeekType() != null)
                {
                    var returnType = Check(TokenType.Void) ? new TypeNode("void") : ParseType();
                    if (Check(TokenType.Void)) Advance(); // consume void
                    
                    // Check if followed by identifier and then parentheses (function signature)
                    if (Check(TokenType.Identifier) && PeekNext()?.Type == TokenType.LeftParen)
                    {
                        var name = ConsumeIdentifierOrGreekLetter("Expected function name.");
                        return ParseFunction(returnType, name, new List<Modifier>());
                    }
                    else
                    {
                        // It's a variable declaration
                        var name = ConsumeIdentifierOrGreekLetter("Expected variable name.");
                        return ParseVariableDeclaration(returnType, name);
                    }
                }

                // Expression statement
                Console.WriteLine($"DEBUG: Falling back to expression statement parsing in fallback");
                return ParseExpressionStatement();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Medium-level fallback parsing failed: {ex.Message}");
                Console.WriteLine($"DEBUG: Exception in fallback method: {ex.GetType().Name}: {ex.Message}");
                throw new ParseException($"Unable to parse medium-level statement at line {Current().Line}");
            }
        }
        
        private Statement ParseMediumLevelStatementEnhanced()
        {
            Console.WriteLine($"DEBUG: Enhanced medium-level parsing at {Current().Type} '{Current().Lexeme}' line {Current().Line}");
            Console.WriteLine($"DEBUG: ENTERING ParseMediumLevelStatementEnhanced method");
            
            try
            {
                Console.WriteLine($"DEBUG: Inside try block");
                // Skip any attributes before parsing the statement
                SkipAttributes();
                Console.WriteLine($"DEBUG: After SkipAttributes, current token: {Current().Type} '{Current().Lexeme}'");
                
                        // Check for C# interop method signatures (but not native Ouroboros functions with C#-style syntax)
        // Only treat as C# interop if explicitly marked or in interop context
        if (IsCSharpMethodSignature() && !IsNativeOuroborosFunctionWithCSharpSyntax())
        {
            Console.WriteLine($"DEBUG: Detected C# interop method signature in enhanced parsing");
            return ParseCSharpMethodWithOuroborosBody();
        }
            
            // Check for syntax level markers (@high, @medium, @low, @asm)
            if (Check(TokenType.HighLevel) || Check(TokenType.MediumLevel) || 
                Check(TokenType.LowLevel) || Check(TokenType.Assembly) || Check(TokenType.SpirvAssembly))
            {
                Console.WriteLine($"DEBUG: Detected syntax level marker, delegating to main parser");
                return ParseDeclaration();
            }
            Console.WriteLine($"DEBUG: Not a syntax level marker");
            
            // Check for generic function declarations: T FunctionName<T>(...)
            if (IsGenericFunctionDeclaration())
            {
                Console.WriteLine($"DEBUG: Detected generic function declaration");
                return ParseGenericFunctionDeclaration();
            }
            Console.WriteLine($"DEBUG: IsGenericFunctionDeclaration() returned false for {Current().Type} '{Current().Lexeme}'");
            
            // Check for type declarations
            if (IsTypeDeclarationEnhanced())
            {
                Console.WriteLine($"DEBUG: Detected type declaration");
                Console.WriteLine($"DEBUG: About to call ParseDeclaration() for {Current().Type} '{Current().Lexeme}'");
                // Parse class, struct, interface, enum etc.
                var result = ParseDeclaration();
                Console.WriteLine($"DEBUG: ParseDeclaration() returned successfully");
                return result;
            }
            
            // Check for variable declarations with modern syntax
            if (IsVariableDeclarationEnhanced())
            {
                Console.WriteLine($"DEBUG: Detected variable declaration");
                return ParseVariableDeclarationEnhanced();
            }
            Console.WriteLine($"DEBUG: Not a variable declaration");
            
                        // Check for domain-scoped using blocks: using DomainName { ... }
            if (Check(TokenType.Using) && IsUsingDomainBlock())
            {
                Console.WriteLine($"DEBUG: Detected domain-scoped using block");
                return ParseUsingDomainBlock();
            }
            
            // Check for const declarations in statement context
            if (Check(TokenType.Const))
            {
                Console.WriteLine($"DEBUG: Detected const declaration in statement parsing");
                return ParseConstDeclaration();
            }
            
            // Check for macro declarations in statement context
            if (Check(TokenType.Macro))
            {
                Console.WriteLine($"DEBUG: Detected macro declaration in statement parsing");
                return ParseMacroDeclaration();
            }
            
            // Check for trait declarations (treated as identifiers since no TokenType.Trait exists)
            if (Check(TokenType.Identifier) && Current().Lexeme == "trait")
            {
                Console.WriteLine($"DEBUG: Detected trait declaration in statement parsing");
                return ParseTraitDeclaration();
            }
            
            // Check for implement blocks (trait implementations)
            if (Check(TokenType.Identifier) && Current().Lexeme == "implement")
            {
                Console.WriteLine($"DEBUG: Detected implement block in statement parsing");
                return ParseImplementDeclaration();
            }
            
            // Check for destructor declarations
            if (Check(TokenType.Identifier) && Current().Lexeme == "destructor")
            {
                Console.WriteLine($"DEBUG: Detected destructor declaration in statement parsing");
                return ParseDestructorDeclaration();
            }
            
            // Debug the print check specifically
            Console.WriteLine($"DEBUG: About to check for fallback statements, current token: {Current().Type} '{Current().Lexeme}'");
            Console.WriteLine($"DEBUG: Check(TokenType.Print) = {Check(TokenType.Print)}");
            
            // Handle specific control flow statements directly
            if (Match(TokenType.While)) return ParseWhileStatement();
            if (Match(TokenType.If)) return ParseIfStatement();
            if (Match(TokenType.For)) return ParseForStatement();
            if (Match(TokenType.Return)) return ParseReturnStatement();
            if (Match(TokenType.Break)) return ParseBreakStatement();
            if (Match(TokenType.Continue)) return ParseContinueStatement();
            if (Match(TokenType.Throw)) return ParseThrowStatement();
            if (Match(TokenType.Try)) return ParseTryStatement();
            if (Match(TokenType.LeftBrace)) return ParseBlock();
            
                // Check for specific statement types that should use medium-level fallback
            if (Check(TokenType.Print) || Check(TokenType.Loop) || Check(TokenType.Function) || Check(TokenType.Unsafe) ||
                Check(TokenType.Import) || Check(TokenType.Using) || Check(TokenType.Class))
            {
                Console.WriteLine($"DEBUG: Using medium-level fallback for statement: {Current().Type}");
                return ParseMediumLevelStatementFallback();
            }
            Console.WriteLine($"DEBUG: Not a medium-level fallback statement");
            
            // Parse as expression statement using enhanced parsing
            Console.WriteLine($"DEBUG: Attempting to parse as expression statement: {Current().Type} '{Current().Lexeme}'");
            
            // Special handling for print statements that might not be caught by the fallback check
            if (Current().Type == TokenType.Print)
            {
                Console.WriteLine($"DEBUG: Found print statement in expression parsing, redirecting to fallback");
                return ParseMediumLevelStatementFallback();
            }
            
            var expr = ParseExpression(); // Changed from ParseRangeOrExpression to ParseExpression to handle assignments
            Console.WriteLine($"DEBUG: Successfully parsed expression, optional semicolon");
            // Make semicolon optional - common in modern languages
            Match(TokenType.Semicolon);
            return new ExpressionStatement(expr);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"DEBUG: Exception in ParseMediumLevelStatementEnhanced: {ex.Message}");
                Console.WriteLine($"DEBUG: Exception stack trace: {ex.StackTrace}");
                // Fall back to medium-level fallback parser on error
                return ParseMediumLevelStatementFallback();
            }
        }
        
        private bool IsTypeDeclarationEnhanced()
        {
            // Check for class, struct, interface, enum, union, type alias, domain, module keywords
            var result = Check(TokenType.Class) || Check(TokenType.Struct) || 
                         Check(TokenType.Interface) || Check(TokenType.Enum) ||
                         Check(TokenType.UnionKeyword) || Check(TokenType.Type) ||
                         Check(TokenType.Component) || Check(TokenType.System) || Check(TokenType.Entity) ||
                         Check(TokenType.Domain) || Check(TokenType.Module);
            Console.WriteLine($"DEBUG: IsTypeDeclarationEnhanced() called for {Current().Type} '{Current().Lexeme}' - returning {result}");
            return result;
        }
        
        private bool IsGenericFunctionDeclaration()
        {
            // Save current position
            var savedPosition = _current;
            
            try
            {
                // Check pattern: TypeName FunctionName<...>(...)
                // or: TypeName FunctionName(...)
                // Exclude 'var' keyword as it's for variable declarations, not generic functions
                if (!Check(TokenType.Identifier) && !IsKnownTypeName(Current()))
                {
                    return false;
                }
                
                // Exclude 'var' keyword - it's for variable declarations
                if (Check(TokenType.Var))
                {
                    return false;
                }
                
                Advance(); // consume type
                
                // Must be followed by identifier (function name)
                if (!Check(TokenType.Identifier))
                {
                    return false;
                }
                
                Advance(); // consume function name
                
                // Check for generic type parameters <...> or direct parameters (...)
                bool hasGenericParams = Check(TokenType.Less);
                if (hasGenericParams)
                {
                    // Skip generic parameters
                    int angleCount = 1;
                    Advance(); // consume <
                    while (!IsAtEnd() && angleCount > 0)
                    {
                        if (Check(TokenType.Less)) angleCount++;
                        else if (Check(TokenType.Greater)) angleCount--;
                        Advance();
                    }
                }
                
                // Must be followed by parameter list
                bool hasParameters = Check(TokenType.LeftParen);
                
                return hasParameters;
            }
            finally
            {
                // Restore position
                _current = savedPosition;
            }
        }
        
        private Statement ParseGenericFunctionDeclaration()
        {
            Console.WriteLine($"DEBUG: Parsing generic function declaration");
            
            // Parse return type
            var returnType = ParseType();
            Console.WriteLine($"DEBUG: Parsed return type: {returnType.Name}");
            
            // Parse function name
            var nameToken = Consume(TokenType.Identifier, "Expected function name.");
            Console.WriteLine($"DEBUG: Parsed function name: {nameToken.Lexeme}");
            
            // Parse generic type parameters if present
            List<TypeParameter> typeParameters = null;
            if (Match(TokenType.Less))
            {
                Console.WriteLine($"DEBUG: Parsing generic type parameters");
                typeParameters = new List<TypeParameter>();
                
                do
                {
                    var typeParamName = Consume(TokenType.Identifier, "Expected type parameter name.").Lexeme;
                    typeParameters.Add(new TypeParameter(typeParamName, new List<TypeNode>(), false, false));
                } while (Match(TokenType.Comma));
                
                Consume(TokenType.Greater, "Expected '>' after type parameters.");
            }
            
            // Parse parameters
            Consume(TokenType.LeftParen, "Expected '(' after function name.");
            var parameters = ParseParameters();
            
            // Parse where clause if present (type constraints)
            if (Match(TokenType.Where))
            {
                Console.WriteLine($"DEBUG: Parsing where clause");
                // For now, just skip the where clause - full implementation would parse constraints
                // where T : IComparable<T>
                do
                {
                    // Skip constraint specification
                    while (!Check(TokenType.LeftBrace) && !Check(TokenType.Semicolon) && !IsAtEnd())
                    {
                        Advance();
                    }
                } while (Match(TokenType.Comma));
            }
            
            // Parse function body
            Statement body;
            if (Check(TokenType.LeftBrace))
            {
                body = ParseBlock();
            }
            else
            {
                // Expression body or just semicolon
                if (Match(TokenType.Semicolon))
                {
                    body = new BlockStatement(new List<Statement>());
                }
                else
                {
                    var expr = ParseAssignment();
                    Consume(TokenType.Semicolon, "Expected ';' after expression body.");
                    body = new ExpressionStatement(expr);
                }
            }
            
            Console.WriteLine($"DEBUG: Successfully parsed generic function declaration");
            return new FunctionDeclaration(nameToken, returnType, parameters, body as BlockStatement, typeParameters, false, new List<Modifier>());
        }
        
        private bool IsVariableDeclarationEnhanced()
        {
            int savedPosition = _current;
            
            try
            {
                // Skip modifiers (volatile, static, const, readonly, atomic, thread_local, etc.)
                while (Check(TokenType.Volatile) || Check(TokenType.Static) || Check(TokenType.Const) || 
                       Check(TokenType.Readonly) || Check(TokenType.Public) || Check(TokenType.Private) ||
                       Check(TokenType.Protected) || Check(TokenType.Internal) || Check(TokenType.Atomic) ||
                       Check(TokenType.ThreadLocal))
                {
                    Advance();
                }
                
                // Check for var keyword after modifiers
                if (Check(TokenType.Var))
                {
                    _current = savedPosition;
                    return true;
                }
                    
                // Check for specific type keywords
                if (Check(TokenType.String) || Check(TokenType.Int) || Check(TokenType.Double) || 
                    Check(TokenType.Bool) || Check(TokenType.Object) || Check(TokenType.Decimal) ||
                    Check(TokenType.Float) || Check(TokenType.Long) || Check(TokenType.Short) ||
                    Check(TokenType.Byte) || Check(TokenType.Char))
                {
                    // Skip type name
                    Advance();
                    
                    // Skip nullable marker if present
                    if (Check(TokenType.Question))
                    {
                        Advance();
                    }
                    
                    // Check if followed by identifier
                    bool isDeclaration = Check(TokenType.Identifier);
                    
                    _current = savedPosition;
                    return isDeclaration;
                }
                    
                // Check if current token could be a type name and next token is an identifier
                if (!Check(TokenType.Identifier))
                {
                    _current = savedPosition;
                    return false;
                }
                
                // Special check: don't treat 'trait', 'implement', or 'destructor' as variable declarations
                if (Current().Lexeme == "trait" || Current().Lexeme == "implement" || Current().Lexeme == "destructor")
                {
                    _current = savedPosition;
                    return false;
                }
                
                // Skip type name
                Advance();
                
                // Skip nullable marker if present
                if (Check(TokenType.Question))
                {
                    Advance();
                }
                
                // Check if followed by identifier
                bool isDeclaration2 = Check(TokenType.Identifier);
                
                _current = savedPosition;
                return isDeclaration2;
            }
            catch
            {
                _current = savedPosition;
                return false;
            }
        }
        
        private Statement ParseVariableDeclarationEnhanced()
        {
            Console.WriteLine($"DEBUG: ENTERING ParseVariableDeclarationEnhanced method");
            
            // Parse modifiers first (volatile, static, const, readonly, atomic, etc.)
            Console.WriteLine($"DEBUG: About to parse modifiers");
            var modifiers = new List<Modifier>();
            while (Check(TokenType.Volatile) || Check(TokenType.Static) || Check(TokenType.Const) || 
                   Check(TokenType.Readonly) || Check(TokenType.Public) || Check(TokenType.Private) ||
                   Check(TokenType.Protected) || Check(TokenType.Internal) || Check(TokenType.Atomic) ||
                   Check(TokenType.ThreadLocal))
            {
                Console.WriteLine($"DEBUG: Found modifier: {Current().Type} '{Current().Lexeme}'");
                var modifierToken = Advance();
                
                // Map TokenType to Modifier enum
                Modifier modifier = modifierToken.Type switch
                {
                    TokenType.Volatile => Modifier.Volatile,
                    TokenType.Static => Modifier.Static,
                    TokenType.Const => Modifier.Const,
                    TokenType.Readonly => Modifier.Readonly,
                    TokenType.Public => Modifier.Public,
                    TokenType.Private => Modifier.Private,
                    TokenType.Protected => Modifier.Protected,
                    TokenType.Internal => Modifier.Internal,
                    TokenType.Atomic => Modifier.Async, // Using Async as closest equivalent for now
                    TokenType.ThreadLocal => Modifier.Static, // Using Static as closest equivalent for now
                    _ => throw new InvalidOperationException($"Unexpected modifier token: {modifierToken.Type}")
                };
                
                modifiers.Add(modifier);
            }
            Console.WriteLine($"DEBUG: Finished parsing modifiers, found {modifiers.Count} modifiers");
            
            TypeNode type;
            string variableName;
            Console.WriteLine($"DEBUG: About to handle different declaration patterns, current token: {Current().Type} '{Current().Lexeme}'");
            
            // Handle different declaration patterns
            if (Check(TokenType.Var))
            {
                Console.WriteLine($"DEBUG: Found 'var' keyword, advancing");
                Advance(); // consume 'var'
                
                // Parse variable name
                Console.WriteLine($"DEBUG: About to parse variable name, current token: {Current().Type} '{Current().Lexeme}'");
                var nameToken = ConsumeIdentifier("Expected variable name after 'var'.");
                variableName = nameToken.Lexeme;
                Console.WriteLine($"DEBUG: Parsed variable name: {variableName}");
                
                // Check for mathematical function definition: f(x) = expression
                if (Check(TokenType.LeftParen))
                {
                    // Parse parameter list for mathematical function
                    Consume(TokenType.LeftParen, "Expected '(' in function definition");
                    var parameters = new List<Parameter>();
                    
                    if (!Check(TokenType.RightParen))
                    {
                        do
                        {
                            var paramName = ConsumeIdentifier("Expected parameter name");
                            parameters.Add(new Parameter(new TypeNode("var"), paramName.Lexeme)); // Mathematical functions use inferred types
                        }
                        while (Match(TokenType.Comma));
                    }
                    Consume(TokenType.RightParen, "Expected ')' after parameters");
                    
                    Consume(TokenType.Assign, "Expected '=' in mathematical function definition");
                    var body = ParseAssignment(); // Parse the function body expression
                    
                    // Create a lambda expression as the initializer
                    var lambdaExpr = new LambdaExpression(parameters, body);
                    type = new TypeNode("Function");
                    
                    Consume(TokenType.Semicolon, "Expected ';' after mathematical function definition.");
                    
                    return new VariableDeclaration(type, new Token(TokenType.Identifier, variableName, variableName, 0, 0, 0, 0, "", _currentSyntaxLevel), lambdaExpr, false, false);
                }
                
                // Check for type annotation (: Type)
                if (Match(TokenType.Colon))
                {
                    type = ParseType();
                }
                else
                {
                    type = new TypeNode("var");
                }
            }
            else
            {
                // Handle explicit type declarations like "string? nullable_string = null;"
                type = ParseType();
                
                // Check for nullable modifier
                if (Match(TokenType.Question))
                {
                    type = new TypeNode(type.Name, type.TypeArguments, type.IsArray, type.ArrayRank, true, type.IsPointer);
                }
                
                var nameToken = ConsumeIdentifier("Expected variable name after type.");
                variableName = nameToken.Lexeme;
            }
            
            // Parse initializer
            Console.WriteLine($"DEBUG: About to parse initializer, current token: {Current().Type} '{Current().Lexeme}'");
            Expression initializer = null;
            if (Match(TokenType.Assign))
            {
                Console.WriteLine($"DEBUG: Found assignment operator, parsing initializer");
                // Handle collection initializers with braces
                if (Check(TokenType.LeftBrace))
                {
                    Console.WriteLine($"DEBUG: Parsing collection initializer");
                    initializer = ParseCollectionInitializer();
                }
                else
                {
                    // Use the full expression parsing that includes throw expressions and lambda support
                    Console.WriteLine($"DEBUG: Parsing assignment expression, current token: {Current().Type} '{Current().Lexeme}'");
                    initializer = ParseAssignment();
                    Console.WriteLine($"DEBUG: Successfully parsed assignment expression");
                }
            }
            Console.WriteLine($"DEBUG: Finished parsing initializer");
            
            // Parse memory-mapped I/O 'at' clause for embedded systems
            Expression memoryAddress = null;
            if (Match(TokenType.At))
            {
                memoryAddress = ParseAssignment(); // Parse the address expression
            }
            
            // Consume semicolon
            Console.WriteLine($"DEBUG: About to consume semicolon, current token: {Current().Type} '{Current().Lexeme}'");
            Consume(TokenType.Semicolon, "Expected ';' after variable declaration.");
            Console.WriteLine($"DEBUG: Successfully consumed semicolon");
            
            // For now, create a standard variable declaration (the memoryAddress could be stored in a custom node type later)
            Console.WriteLine($"DEBUG: Creating variable declaration and returning");
            return new VariableDeclaration(type, new Token(TokenType.Identifier, variableName, variableName, 0, 0, 0, 0, "", _currentSyntaxLevel), initializer, false, false);
        }
        
        private Expression ParseCollectionInitializer()
        {
            Consume(TokenType.LeftBrace, "Expected '{' for collection initializer.");
            
            var elements = new List<Expression>();
            
            if (!Check(TokenType.RightBrace))
            {
                do
                {
                    // Handle dictionary-style initializers like ["key"] = value
                    if (Check(TokenType.LeftBracket))
                    {
                        Advance(); // consume '['
                        var key = ParseAssignment();
                        Consume(TokenType.RightBracket, "Expected ']' after dictionary key.");
                        Consume(TokenType.Assign, "Expected '=' after dictionary key.");
                        var value = ParseAssignment();
                        
                        // Create a binary expression to represent key = value
                        elements.Add(new BinaryExpression(key, new Token(TokenType.Assign, "=", "=", 0, 0, 0, 0, "", _currentSyntaxLevel), value));
                    }
                    else
                    {
                        // Regular array element
                        elements.Add(ParseAssignment());
                    }
                } while (Match(TokenType.Comma));
            }
            
            Consume(TokenType.RightBrace, "Expected '}' after collection initializer.");
            
            return new ArrayExpression(Previous(), elements);
        }

        private Expression ParseRangeOrExpression()
        {
            // Parse first part of potential range or comparison
            var left = ParseAddition();
            
            // Check for range operator
            if (Match(TokenType.Range))
            {
                var right = ParseAddition();
                // Create a range expression - for now, use a binary expression
                return new BinaryExpression(left, Previous(), right);
            }
            
            // Check for spread operator (inclusive range)
            if (Match(TokenType.Spread))
            {
                var right = ParseAddition();
                // Create a spread range expression - for now, use a binary expression
                return new BinaryExpression(left, Previous(), right);
            }
            
            // Check for spaceship operator (three-way comparison)
            if (Match(TokenType.Spaceship))
            {
                var right = ParseAddition();
                // Create a spaceship comparison expression
                return new BinaryExpression(left, Previous(), right);
            }
            
            // Check for null coalescing operator (??)
            if (Match(TokenType.NullCoalesce))
            {
                var right = ParseAddition();
                // Create a null coalescing expression
                return new BinaryExpression(left, Previous(), right);
            }
            
            // Check for null conditional operator (?.)
            if (Match(TokenType.NullConditional))
            {
                var member = Consume(TokenType.Identifier, "Expected member name after '?.'");
                // Create a null conditional member access expression
                return new MemberExpression(left, Previous(), member);
            }
            
            // Continue parsing as normal expression if no special operators
            return FinishExpression(left);
        }
        
        private Expression FinishExpression(Expression left)
        {
            // Check for match expression
            if (Match(TokenType.Match))
            {
                return ParseMatchExpression(left);
            }
            
            // For now, just return the left expression 
            // Range parsing is handled in ParseRangeOrExpression
            return left;
        }
        
        private Expression ParseMatchExpression(Expression target)
        {
            Consume(TokenType.LeftBrace, "Expected '{' after 'match'.");
            
            var arms = new List<MatchArm>();
            
            while (!Check(TokenType.RightBrace) && !IsAtEnd())
            {
                // Parse match arm: pattern => expression
                var pattern = ParseMatchPattern();
                
                // Check for guard clause (when condition)
                Expression guard = null;
                if (Match(TokenType.When))
                {
                    guard = ParseAssignment();
                }
                
                Consume(TokenType.DoubleArrow, "Expected '=>' after pattern.");
                var expression = ParseAssignment();
                
                arms.Add(new MatchArm(pattern, guard, expression));
                
                // Optional comma between arms
                if (Check(TokenType.Comma))
                {
                    Advance();
                }
            }
            
            Consume(TokenType.RightBrace, "Expected '}' after match arms.");
            
            return new MatchExpression(target, arms);
        }
        
        private Pattern ParseMatchPattern()
        {
            // Handle wildcard pattern
            if (Match(TokenType.Underscore))
            {
                return new WildcardPattern();
            }
            
            // Handle tuple patterns
            if (Check(TokenType.LeftParen))
            {
                return ParseTupleMatchPattern();
            }
            
            // Handle literal patterns
            if (Check(TokenType.IntegerLiteral) || Check(TokenType.DoubleLiteral) || 
                Check(TokenType.StringLiteral) || Check(TokenType.BooleanLiteral))
            {
                var literal = ParsePrimary();
                return new LiteralPattern(literal);
            }
            
            // Handle identifier patterns (variable capture)
            if (Check(TokenType.Identifier))
            {
                var identifier = Advance();
                return new IdentifierPattern(identifier);
            }
            
            throw new ParseException($"Expected pattern at line {Current().Line}.");
        }
        
        private Pattern ParseTupleMatchPattern()
        {
            Consume(TokenType.LeftParen, "Expected '(' for tuple pattern.");
            
            var patterns = new List<Pattern>();
            
            if (!Check(TokenType.RightParen))
            {
                do
                {
                    patterns.Add(ParseMatchPattern());
                } while (Match(TokenType.Comma));
            }
            
            Consume(TokenType.RightParen, "Expected ')' after tuple pattern.");
            
            return new TupleMatchPattern(patterns);
        }

        #endregion

        #region Statement Parsing

        private Statement ParseStatement()
        {
            // Skip any attributes before the statement (for local type declarations)
            SkipAttributes();
            
            if (_currentSyntaxLevel == SyntaxLevel.High)
            {
                return ParseHighLevelStatement();
            }
            
            if (_currentSyntaxLevel == SyntaxLevel.Medium)
            {
                return ParseMediumLevelStatement();
            }
            
            if (_currentSyntaxLevel == SyntaxLevel.Low)
            {
                return ParseLowLevelStatement();
            }
            
            if (_currentSyntaxLevel == SyntaxLevel.Assembly)
            {
                // For assembly level, use low-level parsing which supports print statements
                // Real assembly blocks would be parsed by ParseAssemblyStatement
                return ParseLowLevelStatement();
            }
            
            // Control flow
            if (Match(TokenType.If)) return ParseIfStatement();
            if (Match(TokenType.While)) return ParseWhileStatement();
            if (Match(TokenType.For)) return ParseForStatement();
            if (Match(TokenType.ForEach)) return ParseForEachStatement();
            if (Match(TokenType.Do)) return ParseDoWhileStatement();
            if (Match(TokenType.Switch)) return ParseSwitchStatement();
            if (Match(TokenType.Match)) return ParseMatchStatement();

            // Custom loops
            if (Match(TokenType.Repeat)) return ParseRepeatStatement();
            if (Match(TokenType.Iterate)) return ParseIterateStatement();
            if (Match(TokenType.Forever)) return ParseForeverStatement();
            if (Match(TokenType.ParallelFor)) return ParseParallelForStatement();

            // Jump statements
            if (Match(TokenType.Return)) return ParseReturnStatement();
            if (Match(TokenType.Break)) return ParseBreakStatement();
            if (Match(TokenType.Continue)) return ParseContinueStatement();
            if (Match(TokenType.Throw)) return ParseThrowStatement();
            if (Match(TokenType.Yield)) return ParseYieldStatement();

            // Exception handling
            if (Match(TokenType.Try)) return ParseTryStatement();

            // Unsafe blocks
            if (Match(TokenType.Unsafe)) return ParseUnsafeStatement();
            
            // Fixed statement for pinning memory
            if (Match(TokenType.Fixed)) return ParseFixedStatement();

            // Resource management
            // Check if this is a using statement (with parentheses) or a using directive
            if (Check(TokenType.Using))
            {
                // Peek ahead to see if it's followed by '('
                if (PeekNext()?.Type == TokenType.LeftParen)
                {
                    // It's a using statement for resource management
                    Match(TokenType.Using);
                    return ParseUsingStatement();
                }
                else
                {
                    // It's a using directive, handle it here
                    Match(TokenType.Using);
                    return ParseUsing();
                }
            }
            if (Match(TokenType.Lock)) return ParseLockStatement();

            // Block
            if (Match(TokenType.LeftBrace)) return ParseBlock();

            // Assembly
            if (Match(TokenType.Assembly, TokenType.SpirvAssembly)) return ParseAssemblyStatement();

            // Check for type declarations inside methods (nested types)
            if (Match(TokenType.Class, TokenType.Interface, TokenType.Struct, TokenType.Enum))
            {
                var declType = Previous().Type;
                var modifiers = new List<Modifier>();  // Local types usually don't have modifiers
                
                switch (declType)
                {
                    case TokenType.Class:
                        // Handle class declarations properly
                        return ParseClass(modifiers);
                        
                    case TokenType.Interface:
                        // Handle interface declarations properly
                        return ParseInterface(modifiers);
                        
                    case TokenType.Enum:
                        // Handle enum declarations properly
                        return ParseEnum(modifiers);
                        
                    case TokenType.Struct:
                        // Handle struct declarations properly
                        return ParseStruct(modifiers);
                        
                    default:
                        throw Error(Current(), $"Local {declType} declarations are not supported yet.");
                }
            }

            // Var-inferred declaration using := syntax (e.g., numbers := [1,2,3])
            if (Check(TokenType.Identifier) && PeekNext()?.Type == TokenType.Assign)
            {
                var nameToken = ConsumeIdentifier("Expected variable name.");
                Consume(TokenType.Assign, "Expected ':=' operator.");
                var initializerExpr = ParseExpression();

                // Optional semicolon for medium/low syntax levels
                if (Match(TokenType.Semicolon)) { /* consume */ }

                var varType = new TypeNode("var");
                return new VariableDeclaration(varType, nameToken, initializerExpr, false, false);
            }

            // Check for enhanced variable declarations like "var name: type = value;" first
            if (Check(TokenType.Var))
            {
                return ParseVariableDeclarationEnhanced();
            }

            // Variable declaration or expression statement
            if (PeekType() != null)
            {
                // Save position to restore if this isn't a variable declaration
                var savedPosition = _current;
                
                try
                {
                    // Try to parse the full type (including array brackets, etc.)
                    var type = ParseType();
                    
                    // Now check if the next token is an identifier
                    if (Check(TokenType.Identifier) || Check(TokenType.Transform) || 
                        Check(TokenType.Data) || Check(TokenType.Component) || Check(TokenType.System) || Check(TokenType.Entity) ||
                        IsGreekLetterOrMathSymbol(Current().Type))
                    {
                        var name = ConsumeIdentifierOrGreekLetter("Expected variable name.");
                        return ParseVariableDeclaration(type, name);
                    }
                    else
                    {
                        // Not a variable declaration, restore position
                        _current = savedPosition;
                    }
                }
                catch
                {
                    // Failed to parse type, restore position
                    _current = savedPosition;
                }
            }

            var stmt = ParseExpressionStatement();

            return stmt;
        }

        private Statement ParseLowLevelStatement()
        {
            // Skip any attributes at the beginning of the statement
            SkipAttributes();
            
            // Handle print statements even in low-level syntax
            if (Check(TokenType.Print))
            {
                Advance(); // consume 'print'
                var expr = ParseExpression();
                
                // Convert print to Console.WriteLine
                var printCall = new CallExpression(
                    new IdentifierExpression(
                        new Token(TokenType.Identifier, "Console.WriteLine", null, 0, 0, 0, 0, "", _currentSyntaxLevel)),
                    new List<Expression> { expr });
                
                // Semicolon is optional in low-level syntax  
                Match(TokenType.Semicolon);
                
                return new ExpressionStatement(printCall);
            }

            // Handle struct declarations specifically to preserve syntax level
            if (Check(TokenType.Struct))
            {
                var modifiers = new List<Modifier>();
                Match(TokenType.Struct); // consume 'struct'
                return ParseStruct(modifiers);
            }

            // Handle union declarations
            if (Check(TokenType.Union))
            {
                var modifiers = new List<Modifier>();
                Match(TokenType.Union); // consume 'union'
                // For now, parse union as a struct (need to implement UnionDeclaration)
                // We've already consumed the union token, so ParseStruct will expect the name next
                return ParseStruct(modifiers);
            }

            // Handle unsafe blocks
            if (Match(TokenType.Unsafe))
            {
                return ParseUnsafeStatement();
            }
            
            // Handle return statements directly in low-level mode
            if (Check(TokenType.Return))
            {
                Advance(); // consume 'return'
                return ParseReturnStatement();
            }

            // Handle range-based for loops (for i in 0..10)
            if (Check(TokenType.For))
            {
                return ParseRangeBasedForLoop();
            }

            // Handle assembly blocks in low-level/assembly syntax
            if (Match(TokenType.Assembly, TokenType.SpirvAssembly)) return ParseAssemblyStatement();

            // Handle variable declarations in low-level mode - FIXED logic
            if (Check(TokenType.Var) || Check(TokenType.ThreadLocal) || IsVariableDeclarationPattern())
            {
                if (Match(TokenType.Var))
                {
                    var name = ConsumeIdentifierOrGreekLetter("Expected variable name after 'var'.");
                    
                    TypeNode type;
                    if (Match(TokenType.Colon))
                    {
                        type = ParseType();
                    }
                    else
                    {
                        type = new TypeNode("var"); // Type inference
                    }
                    
                    return ParseVariableDeclaration(type, name);
                }
                else if (Match(TokenType.ThreadLocal))
                {
                    // Handle thread_local variable declarations
                    var name = ConsumeIdentifierOrGreekLetter("Expected variable name after 'thread_local'.");
                    
                    TypeNode type;
                    if (Match(TokenType.Colon))
                    {
                        type = ParseType();
                    }
                    else
                    {
                        type = new TypeNode("var"); // Type inference
                    }
                    
                    return ParseVariableDeclaration(type, name);
                }
                else
                {
                    // Explicit type declaration (e.g., int x = 5;)
                    var type = ParseType();
                    var name = ConsumeIdentifierOrGreekLetter("Expected variable name.");
                    return ParseVariableDeclaration(type, name);
                }
            }

            // For other statements in low-level syntax, fall back to standard parsing
            // by temporarily switching to medium level
            var savedLevel = _currentSyntaxLevel;
            _currentSyntaxLevel = SyntaxLevel.Medium;
            var stmt = ParseStatement();
            _currentSyntaxLevel = savedLevel;
            return stmt;
        }

        private IfStatement ParseIfStatement()
        {
            Console.WriteLine($"DEBUG: Starting ParseIfStatement at token: {Current().Type} '{Current().Lexeme}'");
            
            // Support both traditional if (condition) and modern if condition syntax
            bool hasParens = Match(TokenType.LeftParen);
            Console.WriteLine($"DEBUG: If statement has parentheses: {hasParens}");
            
            try
            {
                var condition = ParseExpression();
                Console.WriteLine($"DEBUG: Parsed condition successfully, current token: {Current().Type} '{Current().Lexeme}'");
                
                if (hasParens)
                {
                    if (Check(TokenType.RightParen))
                    {
                        Consume(TokenType.RightParen, "Expected ')' after condition.");
                    }
                    else
                    {
                        Console.WriteLine($"DEBUG: Missing right paren but continuing - found {Current().Type} '{Current().Lexeme}' at position {_current}");
                        // Don't skip tokens - the condition parsing succeeded, just continue without the paren
                    }
                }

                var thenBranch = ParseStatement();
                Statement elseBranch = null;

                if (Match(TokenType.Else))
                {
                    elseBranch = ParseStatement();
                }

                return new IfStatement(Previous(), condition, thenBranch, elseBranch);
            }
            catch (ParseException ex)
            {
                Console.WriteLine($"DEBUG: ParseIfStatement failed: {ex.Message}");
                // Try to recover by finding the opening brace
                while (!Check(TokenType.LeftBrace) && !IsAtEnd())
                {
                    Advance();
                }
                
                // Create a dummy condition and continue
                var dummyCondition = new LiteralExpression(new Token(TokenType.BooleanLiteral, "true", true, 0, 0, 0, 0, "", _currentSyntaxLevel));
                var thenBranch = ParseStatement();
                return new IfStatement(Previous(), dummyCondition, thenBranch, null);
            }
        }

        private WhileStatement ParseWhileStatement()
        {
            var whileToken = Previous();
            
            // Check for Rust-style while-let pattern
            // Example: while let Some(value) = channel.receive() { }
            if (Match(TokenType.Let))
            {
                // Parse pattern matching in while
                // For now, just consume the pattern and treat as regular while
                ConsumeIdentifierOrGreekLetter("Expected pattern name.");
                
                if (Match(TokenType.LeftParen))
                {
                    // Pattern with parameters like Some(value)
                    ConsumeIdentifierOrGreekLetter("Expected pattern parameter.");
                    Consume(TokenType.RightParen, "Expected ')' after pattern parameter.");
                }
                
                Consume(TokenType.Assign, "Expected '=' in while-let pattern.");
                var letCondition = ParseExpression();
                var letBody = ParseStatement();
                
                // For now, treat as regular while with the expression
                // Full implementation would support pattern matching
                return new WhileStatement(whileToken, letCondition, letBody);
            }
            
            // Traditional while loop
            Consume(TokenType.LeftParen, "Expected '(' after 'while'.");
            var condition = ParseExpression();
            Consume(TokenType.RightParen, "Expected ')' after condition.");

            var body = ParseStatement();

            return new WhileStatement(whileToken, condition, body);
        }

        private WhileStatement ParseLoopStatement()
        {
            Console.WriteLine($"DEBUG: Parsing infinite loop statement");
            // 'loop' keyword was already consumed by Match() in the caller
            var loopToken = Previous();
            var body = ParseStatement(); // Parse the loop body (usually a block)
            
            // Infinite loop is equivalent to while(true) { ... }
            var trueCondition = new LiteralExpression(new Token(TokenType.BooleanLiteral, "true", true, 0, 0, 0, 0, "", _currentSyntaxLevel));
            return new WhileStatement(loopToken, trueCondition, body);
        }

        private ForStatement ParseForStatement()
        {
            var forToken = Previous();
            
            // Check for Rust-style for loop: "for i in range" or "for i, j in range"
            if (Check(TokenType.Identifier))
            {
                // Look ahead to see if this is a Rust-style for loop
                var currentPos = _current;
                bool isRustStyleFor = false;
                
                // Consume identifier(s)
                Advance(); // first identifier
                
                // Check for comma-separated identifiers
                while (Check(TokenType.Comma))
                {
                    Advance(); // comma
                    if (Check(TokenType.Identifier))
                    {
                        Advance(); // next identifier
                    }
                }
                
                // Now check if we have 'in' keyword
                if (Check(TokenType.In))
                {
                    isRustStyleFor = true;
                }
                
                // Restore position
                _current = currentPos;
                
                if (isRustStyleFor)
                {
                    // Parse as range-based for loop
                    var iteratorName = ConsumeIdentifierOrGreekLetter("Expected iterator variable name.");
                    
                    // Handle additional iterators (tuple destructuring)
                    while (Match(TokenType.Comma))
                    {
                        ConsumeIdentifierOrGreekLetter("Expected additional iterator variable name.");
                    }
                    
                    Consume(TokenType.In, "Expected 'in' after iterator variable(s).");
                    var rangeExpression = ParseExpression(); // Parse the range (e.g., 0..a.len())
                    
                    var rangeBody = ParseStatement();
                    
                    // Convert range-based for loop to foreach statement
                    var iteratorType = new TypeNode("var"); // Use var for type inference
                    var forEach = new ForEachStatement(forToken, iteratorType, iteratorName, rangeExpression, rangeBody);
                    
                    // For now, return a ForStatement that wraps the ForEach behavior
                    // In a full implementation, we'd have proper range iteration support
                    var init = new VariableDeclaration(iteratorType, iteratorName, null, false, false);
                    return new ForStatement(forToken, init, null, null, rangeBody);
                }
            }
            
            // Traditional C-style for loop
            Consume(TokenType.LeftParen, "Expected '(' after 'for'.");

            Statement initializer = null;
            if (!Check(TokenType.Semicolon))
            {
                if (PeekType() != null)
                {
                    var type = ParseType();
                    var name = ConsumeIdentifierOrGreekLetter("Expected variable name.");
                    initializer = ParseVariableDeclaration(type, name);
                }
                else
                {
                    initializer = new ExpressionStatement(ParseExpression());
                    Consume(TokenType.Semicolon, "Expected ';' after loop initializer.");
                }
            }
            else
            {
                Consume(TokenType.Semicolon, "Expected ';'.");
            }

            Expression condition = null;
            if (!Check(TokenType.Semicolon))
            {
                condition = ParseExpression();
            }
            Consume(TokenType.Semicolon, "Expected ';' after loop condition.");

            Expression update = null;
            if (!Check(TokenType.RightParen))
            {
                update = ParseExpression();
            }
            Consume(TokenType.RightParen, "Expected ')' after for clauses.");

            var body = ParseStatement();

            return new ForStatement(forToken, initializer, condition, update, body);
        }

        private ForEachStatement ParseForEachStatement()
        {
            var forEachToken = Previous();
            Consume(TokenType.LeftParen, "Expected '(' after 'foreach'.");

            var elementType = ParseType();
            var elementName = ConsumeIdentifierOrGreekLetter("Expected element name.");
            Consume(TokenType.In, "Expected 'in' in foreach loop.");
            var collection = ParseExpression();

            Consume(TokenType.RightParen, "Expected ')' after foreach header.");

            var body = ParseStatement();

            return new ForEachStatement(forEachToken, elementType, elementName, collection, body);
        }

        private RepeatStatement ParseRepeatStatement()
        {
            var repeatToken = Previous();

            Expression count = null;
            if (Match(TokenType.LeftParen))
            {
                count = ParseExpression();
                Consume(TokenType.RightParen, "Expected ')' after repeat count.");
            }

            var body = ParseStatement();

            return new RepeatStatement(repeatToken, count, body);
        }

        private IterateStatement ParseIterateStatement()
        {
            var iterateToken = Previous();
            Consume(TokenType.LeftParen, "Expected '(' after 'iterate'.");

            var iteratorName = Consume(TokenType.Identifier, "Expected iterator name.").Lexeme;
            Consume(TokenType.Colon, "Expected ':' after iterator name.");

            var start = ParseExpression();
            Consume(TokenType.Range, "Expected '..' in iterate statement.");
            var end = ParseExpression();

            Expression step = new LiteralExpression(new Token(TokenType.IntegerLiteral, "1", 1, 0, 0, 0, 0, "", _currentSyntaxLevel));
            if (Match(TokenType.Colon))
            {
                step = ParseExpression();
            }

            Consume(TokenType.RightParen, "Expected ')' after iterate header.");

            var body = ParseStatement();

            return new IterateStatement(iterateToken, iteratorName, start, end, step, body);
        }

        private Statement ParseForeverStatement()
        {
            var body = ParseStatement();
            
            // Forever loop is just while(true)
            var trueLiteral = new LiteralExpression(new Token(TokenType.BooleanLiteral, "true", true, 0, 0, 0, 0, "", _currentSyntaxLevel));
            return new WhileStatement(Previous(), trueLiteral, body);
        }

        private ParallelForStatement ParseParallelForStatement()
        {
            var parallelToken = Previous();
            
            int? maxParallelism = null;
            if (Match(TokenType.LeftBracket))
            {
                var parallelismExpr = ParseExpression();
                // In a real implementation, we'd evaluate this at compile time
                Consume(TokenType.RightBracket, "Expected ']' after parallelism degree.");
            }

            Consume(TokenType.For, "Expected 'for' after 'parallel'.");
            var baseFor = ParseForStatement();

            return new ParallelForStatement(parallelToken, baseFor, maxParallelism);
        }

        private MatchStatement ParseMatchStatement()
        {
            var matchToken = Previous();
            var expression = ParseExpression();
            
            Consume(TokenType.LeftBrace, "Expected '{' before match cases.");

            var cases = new List<MatchCase>();
            while (!Check(TokenType.RightBrace) && !IsAtEnd())
            {
                cases.Add(ParseMatchCase());
            }

            Consume(TokenType.RightBrace, "Expected '}' after match cases.");

            return new MatchStatement(matchToken, expression, cases);
        }

        private MatchCase ParseMatchCase()
        {
            var pattern = ParsePattern();
            
            Expression guard = null;
            if (Match(TokenType.When))
            {
                guard = ParseExpression();
            }

            Consume(TokenType.DoubleArrow, "Expected '=>' after pattern.");
            var body = ParseStatement();

            return new MatchCase(pattern, guard, body);
        }

        private Pattern ParsePattern()
        {
            // Simplified pattern parsing - in a real implementation this would be more complex
            return new ConstantPattern(ParseExpression());
        }

        private AssemblyStatement ParseAssemblyStatement()
        {
            var asmToken = Previous();
            Consume(TokenType.LeftBrace, "Expected '{' after 'asm'.");

            var assemblyCode = "";
            // In a real implementation, we'd properly parse assembly code
            while (!Check(TokenType.RightBrace) && !IsAtEnd())
            {
                assemblyCode += Current().Lexeme + " ";
                Advance();
            }

            Consume(TokenType.RightBrace, "Expected '}' after assembly code.");

            return new AssemblyStatement(asmToken, assemblyCode.Trim());
        }

        private BlockStatement ParseBlock()
        {
            var statements = new List<Statement>();

            while (!Check(TokenType.RightBrace) && !IsAtEnd())
            {
                statements.Add(ParseStatement());
            }

            Consume(TokenType.RightBrace, "Expected '}' after block.");

            return new BlockStatement(statements);
        }

        private VariableDeclaration ParseVariableDeclaration(TypeNode type, Token name)
        {
            Expression memoryAddress = null;
            
            // Handle memory-mapped I/O with 'at' clause for embedded systems
            if (Match(TokenType.At))
            {
                memoryAddress = ParseExpression(); // Parse the memory address (e.g., 0x4000_0000)
            }
            
            Expression initializer = null;
            if (Match(TokenType.Assign))
            {
                // Array initializers can use braces regardless of whether explicitly new int[] or just {}
                if (Check(TokenType.LeftBrace))
                {
                    Match(TokenType.LeftBrace);
                    var elements = new List<Expression>();
                    
                    if (!Check(TokenType.RightBrace))
                    {
                        do
                        {
                            elements.Add(ParseExpression());
                        } while (Match(TokenType.Comma));
                    }
                    
                    Consume(TokenType.RightBrace, "Expected '}' after array initializer.");
                    
                    // Create an array expression from the elements
                    initializer = new ArrayExpression(Previous(), elements);
                }
                else
                {
                    initializer = ParseExpression();
                }
            }

            Consume(TokenType.Semicolon, "Expected ';' after variable declaration.");

            bool isConst = type.Name == "const";
            bool isReadonly = type.Name == "readonly";

            // For now, we'll store the memory address as the initializer if no other initializer is provided
            // In a full implementation, this would be handled specially by the compiler/linker
            if (memoryAddress != null && initializer == null)
            {
                // Create a special expression to represent memory-mapped variables
                // This could be handled by the compiler to generate appropriate linker directives
                initializer = memoryAddress;
            }

            return new VariableDeclaration(type, name, initializer, isConst, isReadonly);
        }

        private ExpressionStatement ParseExpressionStatement()
        {
            var expr = ParseExpression();
            // Make semicolon optional in all syntax levels for better error recovery
            Match(TokenType.Semicolon); // Consume if present but don't require
            return new ExpressionStatement(expr);
        }

        private ReturnStatement ParseReturnStatement()
        {
            var returnToken = Previous();
            Expression value = null;

            if (!Check(TokenType.Semicolon) && !Check(TokenType.RightBrace) && !IsAtEnd())
            {
                value = ParseExpression();
            }

            // Make semicolon optional - common in modern languages
            Match(TokenType.Semicolon);
            return new ReturnStatement(returnToken, value);
        }

        private BreakStatement ParseBreakStatement()
        {
            var breakToken = Previous();
            // Make semicolon optional - common in modern languages
            Match(TokenType.Semicolon);
            return new BreakStatement(breakToken);
        }

        private ContinueStatement ParseContinueStatement()
        {
            var continueToken = Previous();
            Consume(TokenType.Semicolon, "Expected ';' after 'continue'.");
            return new ContinueStatement(continueToken);
        }

        private ThrowStatement ParseThrowStatement()
        {
            var throwToken = Previous();
            var exception = ParseExpression();
            Consume(TokenType.Semicolon, "Expected ';' after throw expression.");
            return new ThrowStatement(throwToken, exception);
        }

        private YieldStatement ParseYieldStatement()
        {
            var yieldToken = Previous();
            
            if (Match(TokenType.Break))
            {
                Consume(TokenType.Semicolon, "Expected ';' after 'yield break'.");
                return new YieldStatement(yieldToken, null, true);
            }

            var value = ParseExpression();
            Consume(TokenType.Semicolon, "Expected ';' after yield value.");
            return new YieldStatement(yieldToken, value, false);
        }

        private TryStatement ParseTryStatement()
        {
            var tryToken = Previous();
            Consume(TokenType.LeftBrace, "Expected '{' after 'try'.");
            var tryBlock = ParseBlock();

            var catchClauses = new List<CatchClause>();
            while (Match(TokenType.Catch))
            {
                TypeNode exceptionType = null;
                string exceptionName = null;

                if (Match(TokenType.LeftParen))
                {
                    exceptionType = ParseType();
                    exceptionName = ConsumeIdentifierOrGreekLetter("Expected exception variable name.").Lexeme;
                    Consume(TokenType.RightParen, "Expected ')' after catch clause.");
                }

                // Check for optional when clause (exception filters)
                Expression whenCondition = null;
                if (Match(TokenType.When))
                {
                    Consume(TokenType.LeftParen, "Expected '(' after 'when'.");
                    whenCondition = ParseExpression();
                    Consume(TokenType.RightParen, "Expected ')' after when condition.");
                }

                Consume(TokenType.LeftBrace, "Expected '{' after catch clause.");
                var catchBody = ParseBlock();
                catchClauses.Add(new CatchClause(exceptionType, exceptionName, catchBody, whenCondition));
            }

            Statement finallyBlock = null;
            if (Match(TokenType.Finally))
            {
                Consume(TokenType.LeftBrace, "Expected '{' after 'finally'.");
                finallyBlock = ParseBlock();
            }

            return new TryStatement(tryToken, tryBlock, catchClauses, finallyBlock);
        }

        private UnsafeStatement ParseUnsafeStatement()
        {
            var unsafeToken = Previous();
            Consume(TokenType.LeftBrace, "Expected '{' after 'unsafe'.");
            
            // Parse block statements in low-level mode to ensure proper context
            var statements = new List<Statement>();
            while (!Check(TokenType.RightBrace) && !IsAtEnd())
            {
                statements.Add(ParseLowLevelStatement());
            }
            Consume(TokenType.RightBrace, "Expected '}' after block.");
            var body = new BlockStatement(statements);
            
            return new UnsafeStatement(unsafeToken, body);
        }
        
        private ForEachStatement ParseRangeBasedForLoop()
        {
            var forToken = Previous(); // We already consumed 'for'
            
            // Parse iterator variable(s)
            // Support both single iterator and tuple destructuring:
            // - for i in 0..10
            // - for i, value in items.enumerate()
            var iteratorName = ConsumeIdentifierOrGreekLetter("Expected iterator variable name.");
            
            // Check for multiple iterators (tuple destructuring)
            if (Match(TokenType.Comma))
            {
                // For now, just consume the second iterator and treat as simple foreach
                // Full implementation would support tuple destructuring
                ConsumeIdentifierOrGreekLetter("Expected second iterator variable name.");
            }
            
            Consume(TokenType.In, "Expected 'in' after iterator variable.");
            var rangeExpression = ParseExpression(); // Parse the range (e.g., 0..256)
            
            // Parse the body
            var body = ParseStatement();
            
            // Convert range-based for loop to foreach statement
            // This treats the range as a collection to iterate over
            var iteratorType = new TypeNode("var"); // Use var for type inference
            return new ForEachStatement(forToken, iteratorType, iteratorName, rangeExpression, body);
        }
        
        private FixedStatement ParseFixedStatement()
        {
            var fixedToken = Previous();
            Consume(TokenType.LeftParen, "Expected '(' after 'fixed'.");
            
            // Parse the type (which may include pointer)
            var type = ParseType();
            
            // Parse the variable name
            var name = ConsumeIdentifierOrGreekLetter("Expected variable name.");
            
            Consume(TokenType.Assign, "Expected '=' in fixed statement.");
            
            // Parse the target expression
            var target = ParseExpression();
            
            Consume(TokenType.RightParen, "Expected ')' after fixed declaration.");
            
            // Parse the body statement or block
            var body = ParseStatement();
            
            return new FixedStatement(fixedToken, type, name, target, body);
        }

        private UsingStatement ParseUsingStatement()
        {
            var usingToken = Previous();
            Consume(TokenType.LeftParen, "Expected '(' after 'using'.");

            var type = ParseType();
            var name = ConsumeIdentifierOrGreekLetter("Expected variable name.");
            var resource = ParseVariableDeclaration(type, name);

            Consume(TokenType.RightParen, "Expected ')' after using resource.");

            var body = ParseStatement();

            return new UsingStatement(usingToken, resource, body);
        }

        private LockStatement ParseLockStatement()
        {
            var lockToken = Previous();
            Consume(TokenType.LeftParen, "Expected '(' after 'lock'.");
            var lockObject = ParseExpression();
            Consume(TokenType.RightParen, "Expected ')' after lock object.");

            var body = ParseStatement();

            return new LockStatement(lockToken, lockObject, body);
        }

        private DoWhileStatement ParseDoWhileStatement()
        {
            var doToken = Previous();
            var body = ParseStatement();
            Consume(TokenType.While, "Expected 'while' after do body.");
            Consume(TokenType.LeftParen, "Expected '(' after 'while'.");
            var condition = ParseExpression();
            Consume(TokenType.RightParen, "Expected ')' after condition.");
            Consume(TokenType.Semicolon, "Expected ';' after do-while.");

            return new DoWhileStatement(doToken, body, condition);
        }

        private SwitchStatement ParseSwitchStatement()
        {
            var switchToken = Previous();
            Consume(TokenType.LeftParen, "Expected '(' after 'switch'.");
            var expression = ParseExpression();
            Consume(TokenType.RightParen, "Expected ')' after switch expression.");
            Consume(TokenType.LeftBrace, "Expected '{' before switch body.");

            var cases = new List<CaseClause>();
            Statement defaultCase = null;

            while (!Check(TokenType.RightBrace) && !IsAtEnd())
            {
                if (Match(TokenType.Case))
                {
                    var value = ParseExpression();
                    Consume(TokenType.Colon, "Expected ':' after case value.");
                    
                    var statements = new List<Statement>();
                    while (!Check(TokenType.Case) && !Check(TokenType.Default) && 
                           !Check(TokenType.RightBrace) && !IsAtEnd())
                    {
                        statements.Add(ParseStatement());
                    }

                    cases.Add(new CaseClause(value, statements));
                }
                else if (Match(TokenType.Default))
                {
                    Consume(TokenType.Colon, "Expected ':' after 'default'.");
                    
                    var statements = new List<Statement>();
                    while (!Check(TokenType.Case) && !Check(TokenType.Default) && 
                           !Check(TokenType.RightBrace) && !IsAtEnd())
                    {
                        statements.Add(ParseStatement());
                    }

                    defaultCase = new BlockStatement(statements);
                }
                else
                {
                    throw Error(Current(), "Expected 'case' or 'default' in switch body.");
                }
            }

            Consume(TokenType.RightBrace, "Expected '}' after switch body.");

            return new SwitchStatement(switchToken, expression, cases, defaultCase);
        }

        private NamespaceDeclaration ParseNamespace()
        {
            var namespaceName = "";
            do
            {
                var part = Consume(TokenType.Identifier, "Expected namespace name.");
                namespaceName += part.Lexeme;
                if (Match(TokenType.Dot))
                {
                    namespaceName += ".";
                }
                else
                {
                    break;
                }
            } while (true);

            Consume(TokenType.LeftBrace, "Expected '{' before namespace body.");

            var members = new List<Statement>();
            while (!Check(TokenType.RightBrace) && !IsAtEnd())
            {
                members.Add(ParseDeclaration());
            }

            Consume(TokenType.RightBrace, "Expected '}' after namespace body.");

            return new NamespaceDeclaration(Previous(), namespaceName, members);
        }

        private ImportDeclaration ParseImport()
        {
            var importToken = Previous();
            var modulePath = "";
            
            do
            {
                var part = Consume(TokenType.Identifier, "Expected module name.");
                modulePath += part.Lexeme;
                if (Match(TokenType.Dot))
                {
                    modulePath += ".";
                }
                else
                {
                    break;
                }
            } while (true);

            string alias = null;
            if (Match(TokenType.As))
            {
                alias = Consume(TokenType.Identifier, "Expected alias name.").Lexeme;
            }

            var importedNames = new List<string>();
            if (Match(TokenType.LeftBrace))
            {
                do
                {
                    importedNames.Add(Consume(TokenType.Identifier, "Expected import name.").Lexeme);
                } while (Match(TokenType.Comma));
                
                Consume(TokenType.RightBrace, "Expected '}' after import list.");
            }

            Consume(TokenType.Semicolon, "Expected ';' after import.");

            return new ImportDeclaration(importToken, modulePath, alias, importedNames);
        }

        private Statement ParseUsing()
        {
            // Could be using statement or using directive
            if (Check(TokenType.LeftParen))
            {
                return ParseUsingStatement();
            }

            // Using directive
            // Note: "using" was already consumed by ParseDeclaration
            var usingToken = _tokens[_current - 1]; // Get the "using" token
            bool isStatic = false;
            
            // Check for "using static" - must be done before parsing namespace
            if (Check(TokenType.Static))
            {
                isStatic = true;
                Advance(); // Consume "static"
            }
            
            var namespacePath = "";
            
            do
            {
                var part = Consume(TokenType.Identifier, "Expected namespace name.");
                namespacePath += part.Lexeme;
                if (Match(TokenType.Dot))
                {
                    namespacePath += ".";
                }
                else
                {
                    break;
                }
            } while (true);

            Consume(TokenType.Semicolon, "Expected ';' after using directive.");

            // Pass isStatic flag to ImportDeclaration
            return new ImportDeclaration(usingToken, namespacePath, null, null, isStatic);
        }

        private TypeAliasDeclaration ParseTypeAlias()
        {
            var aliasToken = Previous();
            var name = Consume(TokenType.Identifier, "Expected alias name.");
            Consume(TokenType.Assign, "Expected '=' in type alias.");
            var type = ParseType();
            Consume(TokenType.Semicolon, "Expected ';' after type alias.");

            return new TypeAliasDeclaration(aliasToken, name, type);
        }

        #endregion

        #region Expression Parsing

        private Expression ParseExpression()
        {
            return ParseAssignment(); // Fixed: assignments have lowest precedence, should be parsed first
        }
        
        private Expression ParseAssignment()
        {
            // Check for lambda expressions first before parsing other expressions
            // This is needed because lambda expressions have lower precedence than most operators
            if (IsLambdaStart())
            {
                return ParseLambdaExpression();
            }
            
            var expr = ParseTernary();
            
            // Check if we have an assignment operator
            if (Check(TokenType.Assign) || Check(TokenType.PlusAssign) || Check(TokenType.MinusAssign) ||
                Check(TokenType.MultiplyAssign) || Check(TokenType.DivideAssign) || Check(TokenType.ModuloAssign) ||
                Check(TokenType.BitwiseAndAssign) || Check(TokenType.BitwiseOrAssign) || Check(TokenType.BitwiseXorAssign) ||
                Check(TokenType.LeftShiftAssign) || Check(TokenType.RightShiftAssign) ||
                Check(TokenType.NullCoalesceAssign) || Check(TokenType.PowerAssign))
            {
                Match(TokenType.Assign, TokenType.PlusAssign, TokenType.MinusAssign,
                     TokenType.MultiplyAssign, TokenType.DivideAssign, TokenType.ModuloAssign,
                     TokenType.BitwiseAndAssign, TokenType.BitwiseOrAssign, TokenType.BitwiseXorAssign,
                     TokenType.LeftShiftAssign, TokenType.RightShiftAssign,
                     TokenType.NullCoalesceAssign, TokenType.PowerAssign);
                     
                var op = Previous();
                var value = ParseAssignment(); // right-associative

                // Valid assignment targets: identifier, member access, indexed access, or pointer dereference
                if (expr is IdentifierExpression || expr is MemberExpression || expr is BinaryExpression || expr is UnaryExpression)
                {
                    return new AssignmentExpression(expr, op, value);
                }

                throw Error(op, "Invalid assignment target.");
            }
            
            return expr;
        }

        private Expression ParseTernary()
        {
            Console.WriteLine($"DEBUG: ENTERING ParseTernary()");
            var expr = ParseNullCoalescing();
            Console.WriteLine($"DEBUG: ParseTernary() - Got expression from ParseNullCoalescing: {expr?.GetType().Name}");

            if (Match(TokenType.Question))
            {
                var trueExpr = ParseExpression();
                Consume(TokenType.Colon, "Expected ':' in ternary expression.");
                var falseExpr = ParseExpression();
                return new ConditionalExpression(expr, trueExpr, falseExpr);
            }

            Console.WriteLine($"DEBUG: ParseTernary() returning expression: {expr?.GetType().Name}");
            return expr;
        }

        private Expression ParseNullCoalescing()
        {
            Console.WriteLine($"DEBUG: ENTERING ParseNullCoalescing()");
            var expr = ParseConditionalAccess();
            Console.WriteLine($"DEBUG: ParseNullCoalescing() - Got expression from ParseConditionalAccess: {expr?.GetType().Name}");

            while (Match(TokenType.NullCoalesce))
            {
                var op = Previous();
                var right = ParseConditionalAccess();
                expr = new BinaryExpression(expr, op, right);
            }

            Console.WriteLine($"DEBUG: ParseNullCoalescing() returning expression: {expr?.GetType().Name}");
            return expr;
        }

        private Expression ParseConditionalAccess()
        {
            Console.WriteLine($"DEBUG: ENTERING ParseConditionalAccess()");
            var expr = ParseLogicalOr();
            Console.WriteLine($"DEBUG: ParseConditionalAccess() - Got expression from ParseLogicalOr: {expr?.GetType().Name}");

            // Handle null-conditional operations at higher precedence than null-coalescing
            while (Match(TokenType.NullConditional))
            {
                var op = Previous();
                
                // Parse member access or method call after ?.
                if (Check(TokenType.Identifier))
                {
                    var name = ConsumeIdentifierOrGreekLetter("Expected property name after '?.'.");
                    expr = new MemberExpression(expr, op, name);
                    
                    // Handle method calls like obj?.Method()
                    if (Match(TokenType.LeftParen))
                    {
                        expr = FinishCall(expr);
                    }
                }
                else
                {
                    throw Error(Current(), "Expected property or method name after '?.'.");
                }
            }

            Console.WriteLine($"DEBUG: ParseConditionalAccess() returning expression: {expr?.GetType().Name}");
            return expr;
        }

        private Expression ParseLogicalOr()
        {
            Console.WriteLine($"DEBUG: ENTERING ParseLogicalOr()");
            var expr = ParseLogicalAnd();
            Console.WriteLine($"DEBUG: ParseLogicalOr() - Got expression from ParseLogicalAnd: {expr?.GetType().Name}");

            while (Match(TokenType.LogicalOr))
            {
                var op = Previous();
                var right = ParseLogicalAnd();
                expr = new BinaryExpression(expr, op, right);
            }

            return expr;
        }

        private Expression ParseLogicalAnd()
        {
            Console.WriteLine($"DEBUG: ENTERING ParseLogicalAnd()");
            var expr = ParseBitwiseOr();
            Console.WriteLine($"DEBUG: ParseLogicalAnd() - Got expression from ParseBitwiseOr: {expr?.GetType().Name}");

            while (Match(TokenType.LogicalAnd))
            {
                var op = Previous();
                var right = ParseBitwiseOr();
                expr = new BinaryExpression(expr, op, right);
            }

            return expr;
        }

        private Expression ParseBitwiseOr()
        {
            Console.WriteLine($"DEBUG: ENTERING ParseBitwiseOr()");
            var expr = ParseBitwiseXor();
            Console.WriteLine($"DEBUG: ParseBitwiseOr() - Got expression from ParseBitwiseXor: {expr?.GetType().Name}");

            while (Match(TokenType.BitwiseOr, TokenType.Union))
            {
                var op = Previous();
                var right = ParseBitwiseXor();
                expr = new BinaryExpression(expr, op, right);
            }

            return expr;
        }

        private Expression ParseBitwiseXor()
        {
            Console.WriteLine($"DEBUG: ENTERING ParseBitwiseXor()");
            var expr = ParseBitwiseAnd();
            Console.WriteLine($"DEBUG: ParseBitwiseXor() - Got expression from ParseBitwiseAnd: {expr?.GetType().Name}");

            while (Match(TokenType.BitwiseXor))
            {
                Console.WriteLine($"DEBUG: ParseBitwiseXor() - Found XOR operator, parsing right operand");
                var op = Previous();
                var right = ParseBitwiseAnd();
                expr = new BinaryExpression(expr, op, right);
            }

            Console.WriteLine($"DEBUG: ParseBitwiseXor() - About to return expression: {expr?.GetType().Name}");
            Console.WriteLine($"DEBUG: ParseBitwiseXor() returning: {expr?.GetType().Name}");
            return expr;
        }

        private Expression ParseBitwiseAnd()
        {
            Console.WriteLine($"DEBUG: ENTERING ParseBitwiseAnd()");
            var expr = ParseEquality();
            Console.WriteLine($"DEBUG: ParseBitwiseAnd() - Got expression from ParseEquality: {expr?.GetType().Name}");

            while (Match(TokenType.BitwiseAnd, TokenType.Intersection))
            {
                Console.WriteLine($"DEBUG: ParseBitwiseAnd() - Found bitwise AND operator");
                var op = Previous();
                var right = ParseEquality();
                expr = new BinaryExpression(expr, op, right);
            }

            Console.WriteLine($"DEBUG: ParseBitwiseAnd() - About to return expression: {expr?.GetType().Name}");
            Console.WriteLine($"DEBUG: ParseBitwiseAnd() returning: {expr?.GetType().Name}");
            return expr;
        }

        private Expression ParseEquality()
        {
            var expr = ParseComparison();

            while (Match(TokenType.Equal, TokenType.NotEqual, TokenType.NotEqual2,
                        TokenType.Element, TokenType.NotElement, TokenType.Identical, 
                        TokenType.NotIdentical, TokenType.Almost, TokenType.NotAlmost))
            {
                var op = Previous();
                var right = ParseComparison();
                expr = new BinaryExpression(expr, op, right);
            }

            return expr;
        }

        private Expression ParseComparison()
        {
            var expr = ParseRange();

            // In high-level syntax, don't consume 'is' as a comparison operator
            // because it's used for natural language comparisons like "is greater than"
            var comparisonTypes = _currentSyntaxLevel == SyntaxLevel.High
                ? new[] { TokenType.Greater, TokenType.GreaterEqual, TokenType.Less, 
                         TokenType.LessEqual, TokenType.LessOrEqual, TokenType.GreaterOrEqual,
                         TokenType.Spaceship, TokenType.In,
                         // Mathematical set operations
                         TokenType.Union, TokenType.Intersection, TokenType.SetDifference, TokenType.Element }
                : new[] { TokenType.Greater, TokenType.GreaterEqual, TokenType.Less, 
                         TokenType.LessEqual, TokenType.LessOrEqual, TokenType.GreaterOrEqual,
                         TokenType.Spaceship, TokenType.Is, TokenType.In,
                         // Mathematical set operations
                         TokenType.Union, TokenType.Intersection, TokenType.SetDifference, TokenType.Element };

            while (Match(comparisonTypes))
            {
                var op = Previous();
                
                // Handle 'is' pattern matching specially
                if (op.Type == TokenType.Is && _currentSyntaxLevel != SyntaxLevel.High)
                {
                    // Check if this is pattern matching (type test with optional variable declaration)
                    var savedPosition = _current;
                    
                    // Try to parse a type
                    if (PeekType() != null)
                    {
                        var type = ParseType();
                        
                        // Check if there's a variable declaration after the type
                        Token variable = null;
                        if (Check(TokenType.Identifier))
                        {
                            variable = Advance();
                        }
                        
                        expr = new IsExpression(expr, op, type, variable);
                    }
                    else
                    {
                        // Not pattern matching, treat as regular comparison
                        _current = savedPosition;
                var right = ParseRange();
                expr = new BinaryExpression(expr, op, right);
                    }
                }
                else
                {
                    var right = ParseRange();
                    expr = new BinaryExpression(expr, op, right);
                }
            }

            return expr;
        }

        private Expression ParseRange()
        {
            var expr = ParseShift();
            
            // Check for range operators
            if (Match(TokenType.Range))
            {
                var right = ParseShift();
                return new BinaryExpression(expr, Previous(), right);
            }
            
            // Check for spread operator (inclusive range)
            if (Match(TokenType.Spread))
            {
                var right = ParseShift();
                return new BinaryExpression(expr, Previous(), right);
            }
            
            return expr;
        }

        private Expression ParseShift()
        {
            var expr = ParseAddition();

            while (Match(TokenType.LeftShift, TokenType.RightShift, TokenType.UnsignedRightShift))
            {
                var op = Previous();
                var right = ParseAddition();
                expr = new BinaryExpression(expr, op, right);
            }

            return expr;
        }

        private Expression ParseAddition()
        {
            Console.WriteLine($"DEBUG: ENTERING ParseAddition()");
            var expr = ParseMultiplication();
            Console.WriteLine($"DEBUG: ParseAddition() - Got expression from ParseMultiplication: {expr?.GetType().Name}");

            while (Match(TokenType.Plus, TokenType.Minus))
            {
                var op = Previous();
                var right = ParseMultiplication();
                expr = new BinaryExpression(expr, op, right);
            }

            Console.WriteLine($"DEBUG: ParseAddition() returning: {expr?.GetType().Name}");
            return expr;
        }

        private Expression ParseMultiplication()
        {
            Console.WriteLine($"DEBUG: ENTERING ParseMultiplication()");
            var expr = ParsePower();
            Console.WriteLine($"DEBUG: ParseMultiplication() - Got expression from ParsePower: {expr?.GetType().Name}");

            // Handle only explicit multiplication operators (removed complex implicit multiplication logic)
            while (Match(TokenType.Multiply, TokenType.Divide, TokenType.Modulo, 
                         TokenType.IntegerDivide, TokenType.Times, TokenType.DivisionSign, TokenType.Dot3D))
                {
                    var op = Previous();
                    Console.WriteLine($"DEBUG: ParseMultiplication() found operator {op.Type} '{op.Lexeme}'");
                    var right = ParsePower();
                Console.WriteLine($"DEBUG: ParseMultiplication() parsed right operand: {right?.GetType().Name}");
                    expr = new BinaryExpression(expr, op, right);
            }

            Console.WriteLine($"DEBUG: ParseMultiplication() returning: {expr?.GetType().Name}");
            return expr;
        }

        private Expression ParsePower()
        {
            Console.WriteLine($"DEBUG: ENTERING ParsePower()");
            var expr = ParseUnary();
            Console.WriteLine($"DEBUG: ParsePower() - Got expression from ParseUnary: {expr?.GetType().Name}");

            if (Match(TokenType.Power))
            {
                var op = Previous();
                var right = ParsePower(); // Right associative
                expr = new BinaryExpression(expr, op, right);
            }

            Console.WriteLine($"DEBUG: ParsePower() returning: {expr?.GetType().Name}");
            return expr;
        }

        private Expression ParseUnary()
        {
            Console.WriteLine($"DEBUG: ENTERING ParseUnary() with token {Current().Type} '{Current().Lexeme}'");
            
            if (Match(TokenType.LogicalNot, TokenType.BitwiseNot, TokenType.Plus, TokenType.Minus,
                     TokenType.Increment, TokenType.Decrement, TokenType.Typeof, TokenType.Sizeof,
                     TokenType.Nameof, TokenType.New, TokenType.Delete, TokenType.Multiply, TokenType.BitwiseAnd))
            {
                var op = Previous();
                
                if (op.Type == TokenType.Typeof)
                {
                    var type = ParseType();
                    return new TypeofExpression(op, type);
                }
                else if (op.Type == TokenType.Sizeof)
                {
                    var type = ParseType();
                    return new SizeofExpression(op, type);
                }
                else if (op.Type == TokenType.Nameof)
                {
                    var expr = ParseExpression();
                    return new NameofExpression(op, expr);
                }
                else if (op.Type == TokenType.New)
                {
                    return ParseNewExpression(op);
                }
                else if (op.Type == TokenType.BitwiseAnd)
                {
                    // Special handling for reference expressions: &expr and &mut expr
                    // Check if this is &mut pattern
                    bool isMutable = false;
                    if (Check(TokenType.Identifier) && Current().Lexeme == "mut")
                    {
                        Advance(); // consume 'mut'
                        isMutable = true;
                    }
                    
                    // Parse the expression after & or &mut
                    var expr = ParseUnary();
                    
                    // Create a reference expression - using UnaryExpression with special marker
                    // In a full implementation, you might want a dedicated ReferenceExpression AST node
                    return new UnaryExpression(op, expr, true);
                }
                else
                {
                    var expr = ParseUnary();
                    return new UnaryExpression(op, expr, true);
                }
            }
            
            Console.WriteLine($"DEBUG: ParseUnary() - no unary operators matched");

            // Handle prefix mathematical operators (µ, σ, σ², ∂, ∇, etc.)
            if (IsPrefixMathematicalOperator(Current()))
            {
                Console.WriteLine($"DEBUG: ParseUnary() - found prefix mathematical operator");
                var mathOp = Advance();
                var right = ParseUnary();
                Console.WriteLine($"DEBUG: ParseUnary() - created prefix mathematical expression with {mathOp.Lexeme}");
                return new UnaryExpression(mathOp, right, true);
            }
            
            Console.WriteLine($"DEBUG: ParseUnary() - no prefix mathematical operators matched");

            // Handle limit notation: lim[x→value] expression
            if (Match(TokenType.Limit))
            {
                Console.WriteLine($"DEBUG: ParseUnary() - found limit notation");
                var limitToken = Previous();
                Consume(TokenType.LeftBracket, "Expected '[' after 'lim'.");
                
                // Parse the limit variable and approach value: x→0
                var variable = ConsumeIdentifierOrGreekLetter("Expected variable in limit.");
                Consume(TokenType.Arrow, "Expected '→' after limit variable.");
                var approachValue = ParseExpression();
                
                Consume(TokenType.RightBracket, "Expected ']' after limit approach value.");
                
                // Parse the expression after the limit
                var expr = ParseUnary();
                
                // Create a limit expression - for now use a special call expression
                // In a full implementation, you'd want a LimitExpression AST node
                var limitFunc = new IdentifierExpression(limitToken);
                var args = new List<Expression> { 
                    new IdentifierExpression(variable), 
                    approachValue, 
                    expr 
                };
                return new CallExpression(limitFunc, args);
            }

            // Handle integral notation: ∫[a to b] expression dx
            if (Match(TokenType.Integral))
            {
                Console.WriteLine($"DEBUG: ParseUnary() - found integral notation");
                var integralToken = Previous();
                Consume(TokenType.LeftBracket, "Expected '[' after '∫'.");
                
                // Parse the integration bounds: a to b
                var lowerBound = ParseExpression();
                Consume(TokenType.To, "Expected 'to' in integral bounds.");
                var upperBound = ParseExpression();
                
                Consume(TokenType.RightBracket, "Expected ']' after integral bounds.");
                
                // Parse the expression to integrate
                var expr = ParseUnary();
                
                // Look for optional dx, dy, etc.
                Token integrationVar = null;
                if (Check(TokenType.Identifier) && Current().Lexeme.StartsWith("d") && Current().Lexeme.Length == 2)
                {
                    integrationVar = Advance();
                }
                
                // Create an integral expression - for now use a special call expression
                // In a full implementation, you'd want an IntegralExpression AST node
                var integralFunc = new IdentifierExpression(integralToken);
                var args = new List<Expression> { 
                    lowerBound, 
                    upperBound, 
                    expr 
                };
                if (integrationVar != null)
                {
                    args.Add(new IdentifierExpression(integrationVar));
                }
                return new CallExpression(integralFunc, args);
            }

            Console.WriteLine($"DEBUG: ParseUnary() - about to call ParsePostfix()");
            var result = ParsePostfix();
            Console.WriteLine($"DEBUG: ParseUnary() - got result from ParsePostfix(): {result?.GetType().Name}");
            return result;
        }

        private bool IsPrefixMathematicalOperator(Token token)
        {
            // Check if the token is a mathematical symbol that can be used as a prefix operator
            // in domain contexts (like Statistics domain where µ means "mean of")
            switch (token.Type)
            {
                case TokenType.Mu:         // µ (mean)
                case TokenType.Sigma:      // σ (standard deviation)
                case TokenType.Identifier when token.Lexeme == "σ²":  // σ² (variance)
                case TokenType.PartialDerivative:  // ∂ (partial derivative)
                case TokenType.Nabla:      // ∇ (gradient)
                case TokenType.Rho:        // ρ (correlation)
                    return true;
                default:
                    return false;
            }
        }

        private Expression ParsePostfix()
        {
            // Handle unsafe expressions: unsafe { expression }
            if (Match(TokenType.Unsafe))
            {
                Console.WriteLine($"DEBUG: ParsePostfix - found unsafe expression");
                Consume(TokenType.LeftBrace, "Expected '{' after 'unsafe'.");
                var expression = ParseExpression();
                Consume(TokenType.RightBrace, "Expected '}' after unsafe expression.");
                Console.WriteLine($"DEBUG: ParsePostfix - completed unsafe expression");
                return expression;
            }
            
            var expr = ParseCall();
            Console.WriteLine($"DEBUG: ParsePostfix - after ParseCall, expr type: {expr.GetType().Name}");
            if (expr is IdentifierExpression id)
            {
                Console.WriteLine($"DEBUG: ParsePostfix - identifier expr: '{id.Name}'");
            }

            while (true)
            {
                if (Match(TokenType.Increment, TokenType.Decrement))
                {
                    var op = Previous();
                    expr = new UnaryExpression(op, expr, false);
                }
                else if (Match(TokenType.As))
                {
                    // Handle cast expression: expr as type
                    var asToken = Previous();
                    var targetType = ParseType();
                    expr = new CastExpression(asToken, targetType, expr);
                }
                else if (expr is IdentifierExpression idExpr && Match(TokenType.LogicalNot))
                {
                    // Handle macro invocation: identifier!
                    var bangToken = Previous();
                    
                    // Expect string literal or interpolated string or identifier as macro argument
                    Expression macroArg;
                    if (Check(TokenType.StringLiteral) || Check(TokenType.InterpolatedString))
                    {
                        macroArg = ParsePrimary();
                    }
                    else
                    {
                        // Parse a full expression for the macro argument
                        macroArg = ParseExpression();
                    }
                    
                    // Create a macro invocation expression - use CallExpression with the ! as part of function name
                    var macroName = idExpr.Name + "!";
                    var macroIdentifier = new IdentifierExpression(new Token(TokenType.Identifier, macroName, null,
                                                                            idExpr.Token.Line, idExpr.Token.Column,
                                                                            idExpr.Token.StartPosition, bangToken.EndPosition,
                                                                            idExpr.Token.FileName, _currentSyntaxLevel));
                    
                    var args = new List<Expression> { macroArg };
                    expr = new CallExpression(macroIdentifier, args);
                }
                else
                {
                    break;
                }
            }

            return expr;
        }

        private Expression ParseCall()
        {
            Console.WriteLine($"DEBUG: ENTERING ParseCall()");
            var expr = ParsePrimary();
            Console.WriteLine($"DEBUG: ParseCall() - Got expression from ParsePrimary: {expr?.GetType().Name}");

            while (true)
            {
                Console.WriteLine($"DEBUG: ParseCall() - while loop iteration, current token: {Current().Type} '{Current().Lexeme}'");
                
                if (Match(TokenType.Less) && expr is IdentifierExpression && IsGenericCallContext())
                {
                    // Handle generic function calls: functionName<Type>(args)
                    var genericTypes = new List<TypeNode>();
                    do
                    {
                        genericTypes.Add(ParseType());
                    } while (Match(TokenType.Comma));
                    
                    Consume(TokenType.Greater, "Expected '>' after generic type arguments.");
                    
                    // Create a special expression to hold the generic info until we see parentheses
                    var identExpr = (IdentifierExpression)expr;
                    expr = new GenericIdentifierExpression(identExpr.Token, identExpr.Name, genericTypes);
                }
                else if (Match(TokenType.LeftParen))
                {
                    expr = FinishCall(expr);
                }
                else if (Match(TokenType.Dot, TokenType.Arrow))
                {
                    var op = Previous();
                    var name = ConsumeIdentifierOrGreekLetter("Expected property name after '.'.");
                    expr = new MemberExpression(expr, op, name);
                }
                else if (Match(TokenType.DoubleColon))
                {
                    var op = Previous();
                    var name = ConsumeIdentifierOrGreekLetter("Expected name after '::'.");
                    expr = new MemberExpression(expr, op, name);
                }
                else if (Match(TokenType.LeftBracket))
                {
                    var index = ParseExpression();
                    Consume(TokenType.RightBracket, "Expected ']' after index.");
                    // Create array access expression
                    expr = new BinaryExpression(expr, Previous(), index);
                }
                else if (Match(TokenType.Match))
                {
                    Console.WriteLine($"DEBUG: ParseCall() - Found match expression");
                    // Handle match expressions: expr match { ... }
                    expr = ParseMatchExpression(expr);
                }
                else if (Check(TokenType.LeftBrace) && expr is IdentifierExpression id && IsStructLiteralContext())
                {
                    // Parse struct literal: StructName { field: value, ... }
                    expr = ParseStructLiteral(id.Token);
                }
                else
                {
                    Console.WriteLine($"DEBUG: ParseCall() - No match found, breaking from while loop");
                    break;
                }
            }

            Console.WriteLine($"DEBUG: ParseCall() - About to return expression: {expr?.GetType().Name}");
            return expr;
        }

        private bool IsStructLiteralContext()
        {
            // Check if we're in a context where identifier { } should be interpreted as a struct literal
            // vs other contexts like if statements where { } is a block
            
            // Look ahead to see what's inside the braces
            var savedPosition = _current;
            
            try
            {
                if (!Check(TokenType.LeftBrace))
                    return false;
                
                // Check if we're about to see a compound assignment operator
                // Look back at previous tokens to see if we have an assignment context
                // This helps prevent treating expressions like "x |= y" as struct literals
                if (_current > 0)
                {
                    var prevToken = _tokens[_current - 1];
                    // If the previous token could be part of a compound assignment target,
                    // don't treat this as a struct literal
                    if (prevToken.Type == TokenType.Identifier || 
                        prevToken.Type == TokenType.RightBracket ||
                        prevToken.Type == TokenType.RightParen)
                    {
                        // Look ahead past the current position to check for assignment operators
                        var checkPos = _current;
                        while (checkPos < _tokens.Count && _tokens[checkPos].Type != TokenType.Semicolon)
                        {
                            var tok = _tokens[checkPos];
                            if (tok.Type == TokenType.BitwiseOrAssign ||
                                tok.Type == TokenType.BitwiseAndAssign ||
                                tok.Type == TokenType.BitwiseXorAssign ||
                                tok.Type == TokenType.LeftShiftAssign ||
                                tok.Type == TokenType.RightShiftAssign ||
                                tok.Type == TokenType.PlusAssign ||
                                tok.Type == TokenType.MinusAssign ||
                                tok.Type == TokenType.MultiplyAssign ||
                                tok.Type == TokenType.DivideAssign ||
                                tok.Type == TokenType.ModuloAssign ||
                                tok.Type == TokenType.Assign)
                            {
                                // This is likely an assignment, not a struct literal
                                return false;
                            }
                            checkPos++;
                            // Don't look too far ahead
                            if (checkPos - _current > 5) break;
                        }
                    }
                }
                    
                Advance(); // consume {
                
                // If we immediately see }, it could be an empty struct literal
                if (Check(TokenType.RightBrace))
                    return true;
                
                // Look for patterns that suggest struct literal:
                // 1. field = value pattern
                // 2. Multiple comma-separated expressions without statements
                
                if (Check(TokenType.Identifier) && PeekNext()?.Type == TokenType.Assign)
                {
                    // field = value pattern - definitely struct literal
                    return true;
                }
                
                // Check if we see statement-like tokens that suggest this is a block, not struct literal
                if (Check(TokenType.Print) || Check(TokenType.If) || Check(TokenType.For) ||
                    Check(TokenType.While) || Check(TokenType.Return) || Check(TokenType.Var) ||
                    Check(TokenType.Let) || Check(TokenType.Function))
                {
                    // This looks like a statement block, not a struct literal
                    return false;
                }
                
                // Default to allowing struct literals in expression contexts
                return true;
            }
            finally
            {
                _current = savedPosition;
            }
        }
        
        private bool IsKeyword(string identifier)
        {
            // Check if the identifier is a reserved keyword
            var keywords = new HashSet<string>
            {
                "if", "else", "for", "while", "do", "break", "continue", "return",
                "function", "class", "struct", "enum", "interface", "module", "domain",
                "var", "const", "let", "mut", "ref", "unsafe", "async", "await",
                "try", "catch", "finally", "throw", "match", "when", "is", "as",
                "new", "delete", "typeof", "sizeof", "nameof", "default",
                "true", "false", "null", "this", "self", "super", "base",
                "public", "private", "protected", "internal", "static", "virtual",
                "override", "abstract", "sealed", "readonly", "volatile",
                "using", "namespace", "import", "export", "from", "extern",
                "operator", "implicit", "explicit", "params", "yield",
                "where", "select", "orderby", "group", "join", "into",
                "get", "set", "add", "remove", "value",
                "assembly", "asm", "loop", "repeat", "until", "in", "not"
            };
            
            return keywords.Contains(identifier);
        }
        
        private bool IsGenericCallContext()
        {
            // Use lookahead to determine if < starts generic type arguments or is a comparison
            // Generic calls look like: func<Type>(args) or func<Type1, Type2>()
            // Comparisons look like: var < expression
            
            var savedPosition = _current;
            
            try
            {
                // Look for pattern: < TypeName [, TypeName]* > (
                // If we find this pattern, it's likely a generic call
                // If we find < expression that doesn't look like a type, it's a comparison
                
                if (!Check(TokenType.Less)) return false;
                
                Advance(); // consume <
                
                // Check if the next token could be a type name
                if (!IsValidTypeStart()) return false;
                
                // Try to skip over what looks like a type
                if (!TrySkipType()) return false;
                
                // Handle comma-separated type arguments
                while (Match(TokenType.Comma))
                {
                    if (!IsValidTypeStart()) return false;
                    if (!TrySkipType()) return false;
                }
                
                // Must be followed by > and then (
                if (!Match(TokenType.Greater)) return false;
                return Check(TokenType.LeftParen);
            }
            finally
            {
                _current = savedPosition;
            }
        }
        
        private bool IsValidTypeStart()
        {
            return Check(TokenType.Identifier) || 
                   Check(TokenType.Void) || Check(TokenType.Bool) || Check(TokenType.Byte) ||
                   Check(TokenType.SByte) || Check(TokenType.Short) || Check(TokenType.UShort) ||
                   Check(TokenType.Int) || Check(TokenType.UInt) || Check(TokenType.Long) || 
                   Check(TokenType.ULong) || Check(TokenType.Float) || Check(TokenType.Double) ||
                   Check(TokenType.Decimal) || Check(TokenType.Char) || Check(TokenType.String) || 
                   Check(TokenType.Object) || Check(TokenType.Dynamic) || Check(TokenType.Var);
        }
        
        private bool TrySkipType()
        {
            if (!IsValidTypeStart()) return false;
            
            Advance(); // consume type name
            
            // Handle generic type arguments
            if (Match(TokenType.Less))
            {
                int depth = 1;
                while (!IsAtEnd() && depth > 0)
                {
                    if (Check(TokenType.Less)) depth++;
                    else if (Check(TokenType.Greater)) depth--;
                    Advance();
                }
            }
            
            // Handle array brackets
            while (Match(TokenType.LeftBracket))
            {
                while (!Check(TokenType.RightBracket) && !IsAtEnd())
                {
                    Advance();
                }
                if (!Match(TokenType.RightBracket)) return false;
            }
            
            return true;
        }

        private Expression FinishCall(Expression callee)
        {
            var arguments = new List<Expression>();

            if (!Check(TokenType.RightParen))
            {
                do
                {
                    // Enhanced argument parsing that properly handles lambda expressions
                    // Check if this looks like a lambda expression start
                    if (IsLambdaStart())
                    {
                        // Parse lambda expression specifically to avoid operator precedence issues
                        arguments.Add(ParseLambdaExpression());
                    }
                    else
                    {
                        // Parse regular expression
                        arguments.Add(ParseAssignment());
                    }
                } while (Match(TokenType.Comma));
            }

            Consume(TokenType.RightParen, "Expected ')' after arguments.");

            // Check if the callee is a generic identifier expression
            if (callee is GenericIdentifierExpression genericIdExpr)
            {
                // Create an identifier expression for the function name
                var identExpr = new IdentifierExpression(genericIdExpr.Token);
                return new CallExpression(identExpr, arguments, false, genericIdExpr.GenericTypeArguments);
            }

            return new CallExpression(callee, arguments);
        }
        
        private bool IsLambdaStart()
        {
            // Save current position
            var savedPosition = _current;
            
            // Check for single parameter lambda: identifier =>
            if (Check(TokenType.Identifier))
            {
                Advance();
                bool isLambda = Check(TokenType.DoubleArrow);
                _current = savedPosition;
                return isLambda;
            }
            
            // Check for multi-parameter lambda: (param1, param2) =>
            if (Check(TokenType.LeftParen))
            {
                Advance();
                // Skip parameters
                int parenCount = 1;
                while (!IsAtEnd() && parenCount > 0)
                {
                    if (Check(TokenType.LeftParen)) parenCount++;
                    else if (Check(TokenType.RightParen)) parenCount--;
                    Advance();
                }
                
                bool isLambda = Check(TokenType.DoubleArrow);
                _current = savedPosition;
                return isLambda;
            }
            
            return false;
        }
        
        private Expression ParseLambdaExpression()
        {
            var parameters = new List<Parameter>();
            
            if (Check(TokenType.LeftParen))
            {
                Match(TokenType.LeftParen);
                // Multiple parameters or typed parameters
                if (!Check(TokenType.RightParen))
                {
                    do
                    {
                        // Try to parse a type, but if it fails, assume var
                        TypeNode type;
                        string name;
                        
                        // Check if this looks like a typed parameter (Type Identifier)
                        if (IsKnownTypeName(Current()) && PeekNext()?.Type == TokenType.Identifier)
                        {
                            // Typed parameter: int x, string y, etc.
                            type = ParseType();
                            name = Consume(TokenType.Identifier, "Expected parameter name.").Lexeme;
                        }
                        else
                        {
                            // Just an identifier, assume var type: x, y, etc.
                            var nameToken = ConsumeIdentifierOrGreekLetter("Expected parameter name.");
                            name = nameToken.Lexeme;
                            type = new TypeNode("var");
                        }
                        
                        parameters.Add(new Parameter(type, name));
                    } while (Match(TokenType.Comma));
                }
                
                Consume(TokenType.RightParen, "Expected ')' after parameters.");
            }
            else
            {
                // Single parameter without parentheses
                var name = Consume(TokenType.Identifier, "Expected parameter name.");
                parameters.Add(new Parameter(new TypeNode("var"), name.Lexeme));
            }
            
            Consume(TokenType.DoubleArrow, "Expected '=>' in lambda expression.");
            
            AstNode body;
            if (Check(TokenType.LeftBrace))
            {
                body = ParseBlock();
            }
            else
            {
                // Parse the full expression for lambda body, ensuring we get complete expressions
                body = ParseAssignment(); // Use assignment level to get full expression precedence
            }
            
            return new LambdaExpression(parameters, body);
        }

        private Expression ParseNewExpression(Token newToken)
        {
            // For array creation syntax like "new int[5]", we need to parse the base type
            // without consuming array brackets, since those are part of the creation syntax
            
            // Save current position to potentially backtrack
            var savedPosition = _current;
            
            // First, try to parse a simple type (not an array type)
            string typeName;
            
            if (Match(TokenType.Void, TokenType.Bool, TokenType.Byte, TokenType.SByte,
                     TokenType.Short, TokenType.UShort, TokenType.Int, TokenType.UInt,
                     TokenType.Long, TokenType.ULong, TokenType.Float, TokenType.Double,
                     TokenType.Decimal, TokenType.Char, TokenType.String, TokenType.Object,
                     TokenType.Dynamic, TokenType.Var))
            {
                typeName = Previous().Type.ToString().ToLower();
            }
            else if (Match(TokenType.Vector, TokenType.Matrix, TokenType.Quaternion, TokenType.Transform))
            {
                typeName = Previous().Lexeme;
            }
            else if (Match(TokenType.Identifier) || Match(TokenType.Transform))
            {
                typeName = Previous().Lexeme;
            }
            else
            {
                throw Error(Current(), $"Expected type name after 'new', but found {Current().Type} '{Current().Lexeme}'.");
            }

            // Handle generic type arguments
            List<TypeNode> typeArguments = null;
            if (Match(TokenType.Less))
            {
                typeArguments = new List<TypeNode>();
                do
                {
                    typeArguments.Add(ParseType());
                } while (Match(TokenType.Comma));
                
                Consume(TokenType.Greater, "Expected '>' after type arguments.");
            }

            // Now check what follows
            if (Check(TokenType.LeftBracket))
            {
                // Array creation: new int[5] or new int[] { ... }
                Match(TokenType.LeftBracket);
                
                Expression size = null;
                List<Expression> arguments = new List<Expression>();
                
                // Check if brackets are empty (for array initializer syntax)
                if (!Check(TokenType.RightBracket))
                {
                    size = ParseAssignment();
                    arguments.Add(size);
                }
                
                Consume(TokenType.RightBracket, "Expected ']' after array size.");
                
                // Create array type
                var arrayType = new TypeNode(typeName, typeArguments, true, 1, false);
                
                // Check for array initializer
                List<Expression> initializer = null;
                if (Match(TokenType.LeftBrace))
                {
                    initializer = new List<Expression>();
                    
                    if (!Check(TokenType.RightBrace))
                    {
                        do
                        {
                            initializer.Add(ParseExpression());
                        } while (Match(TokenType.Comma));
                    }
                    
                    Consume(TokenType.RightBrace, "Expected '}' after array initializer.");
                    
                    // If no size was provided but we have an initializer, infer size from initializer
                    if (size == null && initializer != null)
                    {
                        // Create a literal expression with the size based on initializer count
                        size = new LiteralExpression(new Token(TokenType.IntegerLiteral, 
                                                               initializer.Count.ToString(), 
                                                               initializer.Count,
                                                               newToken.Line, newToken.Column, 0, 0, "", _currentSyntaxLevel));
                        arguments.Add(size);
                    }
                }
                else if (size == null)
                {
                    // new int[] without size or initializer is an error
                    throw Error(Current(), "Array creation must have either size or initializer.");
                }
                
                return new NewExpression(newToken, arrayType, arguments, initializer);
            }
            else
            {
                // Check for object initializer without constructor call: new Type { ... }
                if (Check(TokenType.LeftBrace))
                {
                    var type = new TypeNode(typeName, typeArguments, false, 0, false);
                    var arguments = new List<Expression>(); // Empty constructor arguments
                    
                    // Parse object initializer
                    Match(TokenType.LeftBrace);
                    var initializer = new List<Expression>();
                    
                    while (!Check(TokenType.RightBrace) && !IsAtEnd())
                    {
                        // Check if this is a collection initializer (just values) or object initializer (name = value)
                        if (Check(TokenType.Identifier) && PeekNext()?.Type == TokenType.Assign)
                        {
                            // Object initializer: Property = Value
                            var memberName = Consume(TokenType.Identifier, "Expected member name.");
                            Consume(TokenType.Assign, "Expected '=' in initializer.");
                            var value = ParseExpression();
                            
                            // Create member assignment
                            var memberExpr = new IdentifierExpression(memberName);
                            var assignment = new AssignmentExpression(memberExpr, Previous(), value);
                            initializer.Add(assignment);
                        }
                        else
                        {
                                                            // Collection initializer: just values
                                initializer.Add(ParseAssignment());
                        }
                        
                        if (!Match(TokenType.Comma)) break;
                    }
                    
                    Consume(TokenType.RightBrace, "Expected '}' after initializer.");
                    
                    return new NewExpression(newToken, type, arguments, initializer);
                }
                else if (Check(TokenType.LeftParen))
                {
                    // Constructor call: new TypeName(args)
                    var type = new TypeNode(typeName, typeArguments, false, 0, false);
                    
                    Match(TokenType.LeftParen);
                    var arguments = new List<Expression>();
                    
                    if (!Check(TokenType.RightParen))
                    {
                        do
                        {
                            arguments.Add(ParseAssignment());
                        } while (Match(TokenType.Comma));
                    }
                    
                    Consume(TokenType.RightParen, "Expected ')' after constructor arguments.");
                    
                    // Object or collection initializer
                    List<Expression> initializer = null;
                    if (Match(TokenType.LeftBrace))
                    {
                        initializer = new List<Expression>();
                        
                        while (!Check(TokenType.RightBrace) && !IsAtEnd())
                        {
                            // Check if this is a collection initializer (just values) or object initializer (name = value)
                            if (Check(TokenType.Identifier) && PeekNext()?.Type == TokenType.Assign)
                            {
                                // Object initializer: Property = Value
                                var memberName = Consume(TokenType.Identifier, "Expected member name.");
                                Consume(TokenType.Assign, "Expected '=' in initializer.");
                                var value = ParseExpression();
                                
                                // Create member assignment
                                var memberExpr = new IdentifierExpression(memberName);
                                var assignment = new AssignmentExpression(memberExpr, Previous(), value);
                                initializer.Add(assignment);
                            }
                            else
                            {
                                // Collection initializer: just values
                                initializer.Add(ParseAssignment());
                            }
                            
                            if (!Match(TokenType.Comma)) break;
                        }
                        
                        Consume(TokenType.RightBrace, "Expected '}' after initializer.");
                    }
                    
                    return new NewExpression(newToken, type, arguments, initializer);
                }
                else
                {
                    // Neither array creation, constructor call, nor object initializer
                    throw Error(Current(), "Expected '(', '[', or '{' after type in new expression.");
                }
            }
        }

        private Expression ParsePrimary()
        {
            Console.WriteLine($"DEBUG: ENTERING ParsePrimary() with token {Current().Type} '{Current().Lexeme}' at line {Current().Line}");
            
            // High-level syntax features
            if (_currentSyntaxLevel == SyntaxLevel.High)
            {
                if (Check(TokenType.Identifier) && PeekNext()?.Lexeme == "=>")
                {
                    return ParseLambda();
                }
            }

            // Math symbols and Greek letters as identifiers
            if (Match(TokenType.Pi, TokenType.Tau, TokenType.Epsilon, TokenType.Phi, 
                     TokenType.Gamma, TokenType.Rho, TokenType.Delta, TokenType.Alpha,
                     TokenType.Theta, TokenType.Mu, TokenType.Sigma, TokenType.Omega,
                     TokenType.Lambda, TokenType.Beta, TokenType.Eta, TokenType.Kappa,
                     TokenType.Nu, TokenType.Xi, TokenType.Omicron, TokenType.Upsilon,
                     TokenType.Chi, TokenType.Psi, TokenType.Zeta, TokenType.Iota))
            {
                var symbol = Previous();
                return new IdentifierExpression(new Token(TokenType.Identifier, symbol.Lexeme, null,
                                                         symbol.Line, symbol.Column, 0, 0, "", _currentSyntaxLevel));
            }

            // Mathematical operators as identifiers
            if (Match(TokenType.Infinity, TokenType.Integral, TokenType.Summation, 
                     TokenType.Product, TokenType.SquareRoot, TokenType.CubeRoot,
                     TokenType.PartialDerivative, TokenType.Nabla))
            {
                var symbol = Previous();
                return new IdentifierExpression(new Token(TokenType.Identifier, symbol.Lexeme, null,
                                                         symbol.Line, symbol.Column, 0, 0, "", _currentSyntaxLevel));
            }

            // Macro parameter expansion: $identifier
            if (Match(TokenType.Dollar))
            {
                var dollarToken = Previous();
                var paramName = ConsumeIdentifier("Expected parameter name after '$' in macro expansion.");
                
                // Create a special identifier expression for macro parameter expansion
                var macroParamName = "$" + paramName.Lexeme;
                return new IdentifierExpression(new Token(TokenType.Identifier, macroParamName, null,
                                                         dollarToken.Line, dollarToken.Column, 
                                                         dollarToken.StartPosition, paramName.EndPosition,
                                                         dollarToken.FileName, _currentSyntaxLevel));
            }

            // Literals
            if (Match(TokenType.BooleanLiteral, TokenType.NullLiteral))
            {
                return new LiteralExpression(Previous());
            }

            if (Match(TokenType.IntegerLiteral, TokenType.FloatLiteral, TokenType.DoubleLiteral,
                     TokenType.DecimalLiteral, TokenType.HexLiteral, TokenType.BinaryLiteral,
                     TokenType.OctalLiteral, TokenType.UnitLiteral))
            {
                Console.WriteLine($"DEBUG: ParsePrimary() - Found literal token, processing");
                var literal = Previous();
                Console.WriteLine($"DEBUG: ParsePrimary() - Literal token: {literal.Type} '{literal.Lexeme}'");
                
                // Handle numeric literals with type suffixes (e.g., 1u32, 42i64, 3.14f32)
                // Check if the next token is a type suffix
                if ((literal.Type == TokenType.IntegerLiteral || literal.Type == TokenType.FloatLiteral || 
                     literal.Type == TokenType.DoubleLiteral) &&
                    (Check(TokenType.UInt) || Check(TokenType.ULong) || Check(TokenType.Int) || 
                     Check(TokenType.Long) || Check(TokenType.Float) || Check(TokenType.Double) ||
                     Check(TokenType.Byte) || Check(TokenType.SByte) || Check(TokenType.Short) || 
                     Check(TokenType.UShort) ||
                     // Also check for identifier tokens that look like type suffixes
                     (Check(TokenType.Identifier) && IsTypeSuffixIdentifier(Current()))))
                {
                    // Consume the type suffix - it's part of the literal
                    var suffix = Advance();
                    
                    // Create a new literal token that includes the suffix in its lexeme
                    var combinedLexeme = literal.Lexeme + suffix.Lexeme;
                    var combinedToken = new Token(literal.Type, combinedLexeme, literal.Value,
                                                 literal.Line, literal.Column, literal.StartPosition,
                                                 suffix.EndPosition, literal.FileName, literal.SyntaxLevel);
                    return new LiteralExpression(combinedToken);
                }
                
                // Special case: handle lexer tokenization issue where 1u32 becomes 1u + 32
                // Look for patterns like '1u' followed by '32' and combine them
                if (literal.Type == TokenType.IntegerLiteral && literal.Lexeme.EndsWith("u") && 
                    Check(TokenType.IntegerLiteral))
                {
                    var sizeToken = Current();
                    var sizeLexeme = sizeToken.Lexeme;
                    
                    // Check if this looks like a valid type suffix size (8, 16, 32, 64, etc.)
                    if (sizeLexeme == "8" || sizeLexeme == "16" || sizeLexeme == "32" || sizeLexeme == "64" ||
                        sizeLexeme == "size")
                    {
                        // Consume the size token
                        Advance();
                        
                        // Create a properly formatted combined literal
                        var combinedLexeme = literal.Lexeme + sizeLexeme;
                        var combinedToken = new Token(literal.Type, combinedLexeme, literal.Value,
                                                     literal.Line, literal.Column, literal.StartPosition,
                                                     sizeToken.EndPosition, literal.FileName, literal.SyntaxLevel);
                        return new LiteralExpression(combinedToken);
                    }
                }
                
                Console.WriteLine($"DEBUG: ParsePrimary() - About to return LiteralExpression for {literal.Type} '{literal.Lexeme}'");
                return new LiteralExpression(literal);
            }

            if (Match(TokenType.StringLiteral, TokenType.CharLiteral))
            {
                return new LiteralExpression(Previous());
            }

            if (Match(TokenType.InterpolatedString))
            {
                return ParseInterpolatedString(Previous());
            }

            // Special expressions
            if (Match(TokenType.This))
            {
                return new ThisExpression(Previous());
            }

            if (Match(TokenType.Base, TokenType.Super))
            {
                return new BaseExpression(Previous());
            }
            
            // Stackalloc expression for low-level memory allocation
            if (Match(TokenType.Stackalloc))
            {
                return ParseStackallocExpression(Previous());
            }

            // Identifiers (regular or Greek letters/math symbols)
            if (Match(TokenType.Identifier))
            {
                return new IdentifierExpression(Previous());
            }

            if (IsGreekLetterOrMathSymbol(Current().Type))
            {
                var token = Advance();
                return new IdentifierExpression(token);
            }
            
            // Allow contextual keywords as identifiers in expressions
            if (Match(TokenType.Data, TokenType.Component, TokenType.System, TokenType.Entity,
                     TokenType.Channel, TokenType.Thread, TokenType.Lock, TokenType.Atomic))
            {
                var token = Previous();
                return new IdentifierExpression(new Token(TokenType.Identifier, token.Lexeme, null,
                                                         token.Line, token.Column, 0, 0, "", _currentSyntaxLevel));
            }

            // Grouping
            if (Match(TokenType.LeftParen))
            {
                // Check if this is a cast expression: (type) expression
                var savedPosition = _current;
                
                // Try to parse a type
                if (PeekType() != null)
                {
                    // Try to parse as cast expression
                    var type = ParseType();
                    
                    // If we successfully parsed a type and the next token is ')', it's a cast
                    if (Match(TokenType.RightParen))
                    {
                        // This is a cast expression
                        var castToken = Previous();
                        var expr = ParseUnary(); // Cast has same precedence as unary
                        return new CastExpression(castToken, type, expr);
                    }
                    else
                    {
                        // Not a cast, restore position and parse as grouping or tuple
                        _current = savedPosition;
                    }
                }
                
                // Parse the expression inside parentheses
                var innerExpr = ParseExpression();
                
                // Check if this is a tuple literal by looking for comma after first expression
                if (Match(TokenType.Comma))
                {
                    var tupleElements = new List<Expression> { innerExpr };
                    
                    // Parse additional tuple elements
                    do
                    {
                        tupleElements.Add(ParseExpression());
                    } while (Match(TokenType.Comma));
                    
                    Consume(TokenType.RightParen, "Expected ')' after tuple elements.");
                    
                    // Create a tuple expression - for now, use ArrayExpression with a special marker
                    // In a real implementation, you'd want a TupleExpression AST node
                    return new ArrayExpression(Previous(), tupleElements);
                }
                else
                {
                    // This is a regular grouping expression
                    Consume(TokenType.RightParen, "Expected ')' after expression.");
                    return innerExpr; // Return the inner expression directly
                }
            }

            // Array literals
            if (Match(TokenType.LeftBracket))
            {
                return ParseArrayLiteral();
            }
            
            // Array/Collection initializers with braces: { 1, 2, 3 }
            if (Match(TokenType.LeftBrace))
            {
                var elements = new List<Expression>();
                
                if (!Check(TokenType.RightBrace))
                {
                    do
                    {
                        elements.Add(ParseExpression());
                    } while (Match(TokenType.Comma));
                }
                
                Consume(TokenType.RightBrace, "Expected '}' after initializer.");
                
                return new ArrayExpression(Previous(), elements);
            }

            // Vector/Matrix literals
            if (Match(TokenType.Vector))
            {
                return ParseVectorLiteral();
            }

            if (Match(TokenType.Matrix))
            {
                return ParseMatrixLiteral();
            }

            if (Match(TokenType.Quaternion))
            {
                return ParseQuaternionLiteral();
            }

            // Lambda
            if (Check(TokenType.LeftParen) && CheckLambda())
            {
                return ParseLambda();
            }

            // Throw expressions (C# 7.0+ feature)
            if (Match(TokenType.Throw))
            {
                var throwToken = Previous();
                var expression = ParseAssignment(); // Use full expression parsing for throw operand
                return new ThrowExpression(throwToken, expression);
            }

            // Allow using 'transform' keyword as an identifier when it appears in expression context
            if (Match(TokenType.Transform))
            {
                // Treat it the same way as an identifier expression so user code can use a variable named 'transform'.
                return new IdentifierExpression(Previous());
            }

            // Handle type keywords that might appear as identifiers (like u32, i64, etc.)
            if (Match(TokenType.UInt, TokenType.ULong, TokenType.Int, TokenType.Long, 
                     TokenType.UShort, TokenType.Short, TokenType.Byte, TokenType.SByte))
            {
                // These type keywords can appear as identifiers in some contexts
                // For example, when they're type suffixes that got tokenized separately
                var typeToken = Previous();
                return new IdentifierExpression(new Token(TokenType.Identifier, typeToken.Lexeme, null,
                                                         typeToken.Line, typeToken.Column, typeToken.StartPosition, 
                                                         typeToken.EndPosition, typeToken.FileName, _currentSyntaxLevel));
            }

            // Handle unknown tokens gracefully - treat them as identifiers
            if (Current().Type == TokenType.Unknown)
            {
                var token = Advance();
                return new IdentifierExpression(new Token(TokenType.Identifier, token.Lexeme, null,
                                                         token.Line, token.Column, 0, 0, token.FileName, _currentSyntaxLevel));
            }
            
            // Only convert non-keywords to identifiers in expression context  
            // Exclude common keywords and structural tokens that should never be treated as identifiers in expressions
            var excludedKeywords = new HashSet<TokenType>
            {
                TokenType.Void, TokenType.Int, TokenType.Double, TokenType.String, TokenType.Bool,
                TokenType.Class, TokenType.Interface, TokenType.Struct, TokenType.Enum,
                TokenType.Public, TokenType.Private, TokenType.Protected, TokenType.Static,
                TokenType.If, TokenType.Else, TokenType.While, TokenType.For, TokenType.Return,
                TokenType.Break, TokenType.Continue, TokenType.Try, TokenType.Catch, TokenType.Finally,
                TokenType.New, TokenType.Using, TokenType.Import, TokenType.Namespace,
                
                // Structural tokens that should never be identifiers
                TokenType.LeftParen, TokenType.RightParen,
                TokenType.LeftBrace, TokenType.RightBrace,
                TokenType.LeftBracket, TokenType.RightBracket,
                TokenType.Semicolon, TokenType.Comma, TokenType.Dot,
                TokenType.Colon, TokenType.DoubleArrow, TokenType.Arrow,
                
                // Operators that should never be identifiers
                TokenType.Plus, TokenType.Minus, TokenType.Multiply, TokenType.Divide,
                TokenType.Modulo, TokenType.Assign, TokenType.Equal, TokenType.NotEqual,
                TokenType.Less, TokenType.Greater, TokenType.LessEqual, TokenType.GreaterEqual,
                TokenType.LogicalAnd, TokenType.LogicalOr, TokenType.LogicalNot, TokenType.BitwiseAnd,
                TokenType.BitwiseOr, TokenType.BitwiseXor, TokenType.BitwiseNot,
                TokenType.LeftShift, TokenType.RightShift,
                
                // Other reserved words
                TokenType.Function, TokenType.Case, TokenType.Default, TokenType.Switch,
                TokenType.Match, TokenType.In, TokenType.Is, TokenType.As
            };
            
            // If we reach here and have a token with non-empty lexeme that's not a reserved keyword, treat it as an identifier
            if (!string.IsNullOrEmpty(Current().Lexeme) && !excludedKeywords.Contains(Current().Type))
            {
                var token = Advance();
                return new IdentifierExpression(new Token(TokenType.Identifier, token.Lexeme, null,
                                                         token.Line, token.Column, 0, 0, token.FileName, _currentSyntaxLevel));
            }
            
            throw Error(Current(), "Expected expression.");
        }

        private Expression ParseInterpolatedString(Token stringToken)
        {
            var parts = new List<Expression>();
            var content = stringToken.Value?.ToString() ?? stringToken.Lexeme;
            
            // Parse the interpolated string content
            int i = 0;
            var currentPart = new StringBuilder();
            
            while (i < content.Length)
            {
                if (i < content.Length - 1 && content[i] == '{' && content[i + 1] != '{')
                {
                    // Save the current string part if any
                    if (currentPart.Length > 0)
                    {
                        parts.Add(new LiteralExpression(new Token(TokenType.StringLiteral, currentPart.ToString(), 
                                                                    currentPart.ToString(), stringToken.Line, stringToken.Column, 
                                                                    0, 0, stringToken.FileName, stringToken.SyntaxLevel)));
                        currentPart.Clear();
                    }
                    
                    // Find the matching closing brace
                    i++; // Skip the opening brace
                    var exprStart = i;
                    int braceCount = 1;
                    
                    while (i < content.Length && braceCount > 0)
                    {
                        if (content[i] == '{')
                            braceCount++;
                        else if (content[i] == '}')
                            braceCount--;
                        if (braceCount > 0) i++;
                    }
                    
                    if (braceCount != 0)
                    {
                        throw Error(stringToken, "Unmatched braces in interpolated string.");
                    }
                    
                    // Parse the expression inside the braces
                    var exprCode = content.Substring(exprStart, i - exprStart);
                    
                    // Create a mini-lexer/parser for the expression
                    // For now, we'll create a simple identifier or member access expression
                    var exprParts = exprCode.Split('.');
                    Expression expr = new IdentifierExpression(new Token(TokenType.Identifier, exprParts[0].Trim(), 
                                                              exprParts[0].Trim(), stringToken.Line, stringToken.Column, 
                                                              0, 0, stringToken.FileName, stringToken.SyntaxLevel));
                    
                    for (int j = 1; j < exprParts.Length; j++)
                    {
                        var memberName = exprParts[j].Trim();
                        // Handle method calls in interpolations
                        if (memberName.EndsWith("()"))
                        {
                            memberName = memberName.Substring(0, memberName.Length - 2);
                            var dotToken = new Token(TokenType.Dot, ".", null, stringToken.Line, stringToken.Column, 0, 0, stringToken.FileName, stringToken.SyntaxLevel);
                            var memberToken = new Token(TokenType.Identifier, memberName, memberName, stringToken.Line, stringToken.Column, 0, 0, stringToken.FileName, stringToken.SyntaxLevel);
                            expr = new MemberExpression(expr, dotToken, memberToken);
                            expr = new CallExpression(expr, new List<Expression>());
                        }
                        else
                        {
                            var dotToken = new Token(TokenType.Dot, ".", null, stringToken.Line, stringToken.Column, 0, 0, stringToken.FileName, stringToken.SyntaxLevel);
                            var memberToken = new Token(TokenType.Identifier, memberName, memberName, stringToken.Line, stringToken.Column, 0, 0, stringToken.FileName, stringToken.SyntaxLevel);
                            expr = new MemberExpression(expr, dotToken, memberToken);
                        }
                    }
                    
                    parts.Add(expr);
                    i++; // Skip the closing brace
                }
                else if (i < content.Length - 1 && content[i] == '{' && content[i + 1] == '}')
                {
                    // Escaped brace
                    currentPart.Append('{');
                    i += 2;
                }
                else if (i < content.Length - 1 && content[i] == '}' && content[i + 1] == '}')
                {
                    // Escaped brace
                    currentPart.Append('}');
                    i += 2;
                }
                else
                {
                    currentPart.Append(content[i]);
                    i++;
                }
            }
            
            // Add any remaining string part
            if (currentPart.Length > 0)
            {
                parts.Add(new LiteralExpression(new Token(TokenType.StringLiteral, currentPart.ToString(), 
                                                            currentPart.ToString(), stringToken.Line, stringToken.Column, 
                                                            0, 0, stringToken.FileName, stringToken.SyntaxLevel)));
            }
            
            return new InterpolatedStringExpression(stringToken, parts);
        }

        private Expression ParseArrayLiteral()
        {
            var elements = new List<Expression>();
            
            if (!Check(TokenType.RightBracket))
            {
                var firstElement = ParseExpression();
                
                // Check for Rust-style array initialization: [value; count]
                if (Match(TokenType.Semicolon))
                {
                    var count = ParseExpression();
                    Consume(TokenType.RightBracket, "Expected ']' after array size.");
                    
                    // Create a special array expression that represents repeated initialization
                    // For now, we'll expand it to a full array with repeated elements
                    // In a full implementation, this would be optimized at compile time
                    if (count is LiteralExpression literalCount && literalCount.Value is int countValue)
                    {
                        for (int i = 0; i < countValue; i++)
                        {
                            elements.Add(firstElement);
                        }
                    }
                    else
                    {
                        // If count is not a literal, we'll need special handling in the compiler
                        // For now, just add the first element
                        elements.Add(firstElement);
                    }
                    
                    return new ArrayExpression(Previous(), elements);
                }
                
                // Regular array literal: [elem1, elem2, ...]
                elements.Add(firstElement);
                
                while (Match(TokenType.Comma))
                {
                    // Check for spread operator (...expression)
                    if (Match(TokenType.Spread))
                    {
                        var spreadToken = Previous();
                        var spreadExpression = ParseAssignment();
                        
                        // Create a spread expression - use a special spread expression type
                        // For now, we'll use a binary expression with the spread token
                        elements.Add(new BinaryExpression(null, spreadToken, spreadExpression));
                    }
                    else
                    {
                        elements.Add(ParseExpression());
                    }
                }
            }
            
            Consume(TokenType.RightBracket, "Expected ']' after array elements.");
            
            return new ArrayExpression(Previous(), elements);
        }

        private Expression ParseVectorLiteral()
        {
            var vectorToken = Previous();
            Consume(TokenType.LeftParen, "Expected '(' after 'vector'.");
            
            var components = new List<Expression>();
            do
            {
                components.Add(ParseAssignment());
            } while (Match(TokenType.Comma));
            
            Consume(TokenType.RightParen, "Expected ')' after vector components.");
            
            return new VectorExpression(vectorToken, components);
        }

        private Expression ParseMatrixLiteral()
        {
            var matrixToken = Previous();
            Consume(TokenType.LeftBracket, "Expected '[' after 'matrix'.");
            
            var rows = new List<List<Expression>>();
            
            do
            {
                var row = new List<Expression>();
                Consume(TokenType.LeftBracket, "Expected '[' for matrix row.");
                
                do
                {
                                            row.Add(ParseAssignment());
                } while (Match(TokenType.Comma));
                
                Consume(TokenType.RightBracket, "Expected ']' after matrix row.");
                rows.Add(row);
                
            } while (Match(TokenType.Comma));
            
            Consume(TokenType.RightBracket, "Expected ']' after matrix rows.");
            
            return new MatrixExpression(matrixToken, rows);
        }

        private Expression ParseQuaternionLiteral()
        {
            var quaternionToken = Previous();
            Consume(TokenType.LeftParen, "Expected '(' after 'quaternion'.");
            
                            var w = ParseAssignment();
            Consume(TokenType.Comma, "Expected ',' after w component.");
                            var x = ParseAssignment();
            Consume(TokenType.Comma, "Expected ',' after x component.");
                            var y = ParseAssignment();
            Consume(TokenType.Comma, "Expected ',' after y component.");
                            var z = ParseAssignment();
            
            Consume(TokenType.RightParen, "Expected ')' after quaternion components.");
            
            return new QuaternionExpression(quaternionToken, w, x, y, z);
        }

        private bool CheckLambda()
        {
            // Look ahead to check if this is a lambda expression
            // Case 1: Simple lambda: x => expr
            if (Check(TokenType.Identifier) && PeekNext()?.Type == TokenType.DoubleArrow)
            {
                return true;
            }
            
            // Case 2: Parenthesized lambda: (x, y) => expr
            if (Check(TokenType.LeftParen))
            {
                var i = _current + 1;
                int parenCount = 1;
                
                while (i < _tokens.Count && parenCount > 0)
                {
                    if (_tokens[i].Type == TokenType.LeftParen) parenCount++;
                    else if (_tokens[i].Type == TokenType.RightParen) parenCount--;
                    i++;
                }
                
                return i < _tokens.Count && _tokens[i].Type == TokenType.DoubleArrow;
            }
            
            return false;
        }

        private Expression ParseLambda()
        {
            var parameters = new List<Parameter>();
            
            if (Check(TokenType.LeftParen))
            {
                Match(TokenType.LeftParen);
                // Multiple parameters or typed parameters
                if (!Check(TokenType.RightParen))
                {
                    do
                    {
                        // Try to parse a type, but if it fails, assume var
                        TypeNode type;
                        string name;
                        
                        // Check if this looks like a typed parameter (Type Identifier)
                        // We need to distinguish between "int x" and just "x"
                        if (IsKnownTypeName(Current()) && PeekNext()?.Type == TokenType.Identifier)
                        {
                            // Typed parameter: int x, string y, etc.
                            type = ParseType();
                            name = Consume(TokenType.Identifier, "Expected parameter name.").Lexeme;
                        }
                        else
                        {
                            // Just an identifier, assume var type: x, y, etc.
                            // Also accept Greek letters as parameter names
                            var nameToken = ConsumeIdentifierOrGreekLetter("Expected parameter name.");
                            name = nameToken.Lexeme;
                            type = new TypeNode("var");
                        }
                        
                        parameters.Add(new Parameter(type, name));
                    } while (Match(TokenType.Comma));
                }
                
                Consume(TokenType.RightParen, "Expected ')' after parameters.");
            }
            else
            {
                // Single parameter without parentheses
                var name = Consume(TokenType.Identifier, "Expected parameter name.");
                parameters.Add(new Parameter(new TypeNode("var"), name.Lexeme));
            }
            
            Consume(TokenType.DoubleArrow, "Expected '=>' in lambda expression.");
            
            AstNode body;
            if (Check(TokenType.LeftBrace))
            {
                body = ParseBlock();
            }
            else
            {
                body = ParseAssignment();
            }
            
            return new LambdaExpression(parameters, body);
        }
        
        private Expression ParseStackallocExpression(Token stackallocToken)
        {
            // Parse: stackalloc type[size]
            // Note: For stackalloc, we parse just the base type, not an array type
            // The brackets are part of the stackalloc syntax, not the type
            string typeName;
            
            if (Match(TokenType.Void, TokenType.Bool, TokenType.Byte, TokenType.SByte,
                     TokenType.Short, TokenType.UShort, TokenType.Int, TokenType.UInt,
                     TokenType.Long, TokenType.ULong, TokenType.Float, TokenType.Double,
                     TokenType.Decimal, TokenType.Char, TokenType.String, TokenType.Object,
                     TokenType.Dynamic, TokenType.Var))
            {
                typeName = Previous().Type.ToString().ToLower();
            }
            else if (Match(TokenType.Identifier))
            {
                typeName = Previous().Lexeme;
            }
            else
            {
                throw Error(Current(), $"Expected type name after 'stackalloc', but found {Current().Type} '{Current().Lexeme}'.");
            }
            
            var type = new TypeNode(typeName);
            
            // Expect array syntax for stackalloc
            Consume(TokenType.LeftBracket, "Expected '[' after type in stackalloc.");
                            var size = ParseAssignment();
            Consume(TokenType.RightBracket, "Expected ']' after size in stackalloc.");
            
            // Create a special NewExpression variant for stackalloc
            // We'll use a special "stackalloc" type name to distinguish it
            var stackallocType = new TypeNode("stackalloc_" + type.Name, null, true, 1);
            
            return new NewExpression(stackallocToken, stackallocType, new List<Expression> { size });
        }

        #endregion

        #region Helper Methods

        private void SkipAttributes()
        {
            // Process syntax level attributes first
            ProcessSyntaxLevelAttributes();
            
            // Skip attributes in the form [AttributeName] or [AttributeName(args)]
            while (Match(TokenType.LeftBracket))
            {
                // Skip everything until we find the matching right bracket
                int bracketCount = 1;
                while (bracketCount > 0 && !IsAtEnd())
                {
                    if (Match(TokenType.LeftBracket))
                    {
                        bracketCount++;
                    }
                    else if (Match(TokenType.RightBracket))
                    {
                        bracketCount--;
                    }
                    else
                    {
                        Advance();
                    }
                }
            }
            
            // Skip remaining @ attributes that aren't syntax level
            while (Match(TokenType.At))
            {
                // Check for syntax level attributes first
                if (Check(TokenType.HighLevel) || Check(TokenType.MediumLevel) || Check(TokenType.LowLevel))
                {
                    // These should have been processed already
                    Advance();
                }
                else if (Check(TokenType.Repr))
                {
                    // Handle @repr attribute specifically
                    Advance(); // consume 'repr'
                    
                    // If followed by parentheses, skip the parameter list (e.g., @repr(C))
                    if (Match(TokenType.LeftParen))
                    {
                        int parenCount = 1;
                        while (parenCount > 0 && !IsAtEnd())
                        {
                            if (Match(TokenType.LeftParen))
                            {
                                parenCount++;
                            }
                            else if (Match(TokenType.RightParen))
                            {
                                parenCount--;
                            }
                            else
                            {
                                Advance();
                            }
                        }
                    }
                }
                // Handle advanced attributes
                else if (Check(TokenType.Wasm) || Check(TokenType.Webgl) || Check(TokenType.Component) ||
                        Check(TokenType.System) || Check(TokenType.Entity) || Check(TokenType.WasmSimd) ||
                        Check(TokenType.Gpu) || Check(TokenType.State) || Check(TokenType.Receive) ||
                        Check(TokenType.External) || Check(TokenType.Payable) || Check(TokenType.Public) ||
                        Check(TokenType.View) || Check(TokenType.Event) || Check(TokenType.Import))
                {
                    Advance(); // consume the specific attribute token
                    
                    // If followed by parentheses, skip the parameter list
                    if (Match(TokenType.LeftParen))
                    {
                        int parenCount = 1;
                        while (parenCount > 0 && !IsAtEnd())
                        {
                            if (Match(TokenType.LeftParen))
                            {
                                parenCount++;
                            }
                            else if (Match(TokenType.RightParen))
                            {
                                parenCount--;
                            }
                            else
                            {
                                Advance();
                            }
                        }
                    }
                }
                else if (Check(TokenType.Identifier))
                {
                    var attributeName = Current().Lexeme;
                    
                    // Check for alternative syntax level attribute names
                    if (attributeName == "high" || attributeName == "medium" || attributeName == "low")
                    {
                        // These should have been processed already
                        Advance();
                    }
                    else if (attributeName == "repr")
                    {
                        // Handle @repr attribute by identifier as fallback
                        Advance(); // consume 'repr'
                        
                        // If followed by parentheses, skip the parameter list (e.g., @repr(C))
                        if (Match(TokenType.LeftParen))
                        {
                            int parenCount = 1;
                            while (parenCount > 0 && !IsAtEnd())
                            {
                                if (Match(TokenType.LeftParen))
                                {
                                    parenCount++;
                                }
                                else if (Match(TokenType.RightParen))
                                {
                                    parenCount--;
                                }
                                else
                                {
                                    Advance();
                                }
                            }
                        }
                    }
                    // Handle advanced attributes as identifiers (fallback)
                    else if (attributeName == "wasm" || attributeName == "webgl" || attributeName == "component" ||
                            attributeName == "system" || attributeName == "entity" || attributeName == "wasm_simd" ||
                            attributeName == "gpu" || attributeName == "state" || attributeName == "receive" ||
                            attributeName == "external" || attributeName == "payable" || attributeName == "public" ||
                            attributeName == "view" || attributeName == "event" || attributeName == "inline" ||
                            attributeName == "zero_cost" || attributeName == "packed" || attributeName == "volatile" ||
                            attributeName == "async" || attributeName == "unsafe" || attributeName == "actor" ||
                            attributeName == "supervisor" || attributeName == "contract" || attributeName == "oracle" ||
                            attributeName == "state_channel" || attributeName == "secure" || attributeName == "dna" ||
                            attributeName == "molecular_dynamics" || attributeName == "genomics" || attributeName == "spatial" ||
                            attributeName == "spatial_index" || attributeName == "fixed_point" || attributeName == "shader" ||
                            attributeName == "kernel" || attributeName == "shared" || attributeName == "table" ||
                            attributeName == "primary_key" || attributeName == "index" || attributeName == "foreign_key" ||
                            attributeName == "verified" || attributeName == "ghost" || attributeName == "real_time" ||
                            attributeName == "priority_ceiling" || attributeName == "periodic" || attributeName == "deadline" ||
                            attributeName == "wcet" || attributeName == "sporadic" || attributeName == "cyclic_executive" ||
                            attributeName == "differentiable" || attributeName == "model" || attributeName == "import" ||
                            attributeName == "zkp" || attributeName == "mpc" || attributeName == "constant_time" ||
                            attributeName == "no_std" || attributeName == "no_alloc" || attributeName == "section" ||
                            attributeName == "interrupt" || attributeName == "no_mangle" || attributeName == "global_allocator" ||
                            attributeName == "naked" || attributeName == "no_stack" || attributeName == "compile_time" ||
                            attributeName == "emit" || attributeName == "simd" || attributeName == "parallel")
                    {
                        Advance(); // consume the attribute name
                        
                        // If followed by parentheses, skip the parameter list
                        if (Match(TokenType.LeftParen))
                        {
                            int parenCount = 1;
                            while (parenCount > 0 && !IsAtEnd())
                            {
                                if (Match(TokenType.LeftParen))
                                {
                                    parenCount++;
                                }
                                else if (Match(TokenType.RightParen))
                                {
                                    parenCount--;
                    }
                    else
                    {
                                    Advance();
                                }
                            }
                        }
                    }
                    else
                    {
                        Advance(); // Skip unknown attribute name
                        
                        // If followed by parentheses, skip the parameter list
                        if (Match(TokenType.LeftParen))
                        {
                            int parenCount = 1;
                            while (parenCount > 0 && !IsAtEnd())
                            {
                                if (Match(TokenType.LeftParen))
                                {
                                    parenCount++;
                                }
                                else if (Match(TokenType.RightParen))
                                {
                                    parenCount--;
                                }
                                else
                                {
                                    Advance();
                                }
                            }
                        }
                    }
                }
                else
                {
                    // If not followed by identifier, just break (malformed attribute)
                    break;
                }
            }
        }
        
        private void ProcessSyntaxLevelAttributes()
        {
            // Process @ syntax level attributes and set the current syntax level
            while (Check(TokenType.At))
            {
                var atToken = Current();
                Advance(); // consume @
                
                if (Check(TokenType.HighLevel))
                {
                    _currentSyntaxLevel = SyntaxLevel.High;
                    Advance(); // consume high level token
                }
                else if (Check(TokenType.MediumLevel))
                {
                    _currentSyntaxLevel = SyntaxLevel.Medium;
                    Advance(); // consume medium level token
                }
                else if (Check(TokenType.LowLevel))
                {
                    _currentSyntaxLevel = SyntaxLevel.Low;
                    Advance(); // consume low level token
                }
                else if (Check(TokenType.Assembly) || Check(TokenType.SpirvAssembly))
                {
                    // @asm functions use assembly-level syntax
                    _currentSyntaxLevel = SyntaxLevel.Assembly;
                    Advance(); // consume asm token
                }
                else if (Check(TokenType.Identifier))
                {
                    var attributeName = Current().Lexeme;
                    
                    // Check for alternative syntax level attribute names
                    if (attributeName == "high")
                    {
                        _currentSyntaxLevel = SyntaxLevel.High;
                        Advance(); // consume identifier
                    }
                    else if (attributeName == "medium")
                    {
                        _currentSyntaxLevel = SyntaxLevel.Medium;
                        Advance(); // consume identifier
                    }
                    else if (attributeName == "low")
                    {
                        _currentSyntaxLevel = SyntaxLevel.Low;
                        Advance(); // consume identifier
                    }
                    else if (attributeName == "asm")
                    {
                        // @asm functions use low-level syntax
                        _currentSyntaxLevel = SyntaxLevel.Low;
                        Advance(); // consume identifier
                    }
                    else
                    {
                        // Not a syntax level attribute, back up and let SkipAttributes handle it
                        _current--; // back up past the @
                        break;
                    }
                }
                else
                {
                    // Not a syntax level attribute, back up and let SkipAttributes handle it
                    _current--; // back up past the @
                    break;
                }
                
                // Skip optional parentheses for attributes like @low() or @high(always)
                if (Match(TokenType.LeftParen))
                {
                    int parenCount = 1;
                    while (parenCount > 0 && !IsAtEnd())
                    {
                        if (Match(TokenType.LeftParen))
                        {
                            parenCount++;
                        }
                        else if (Match(TokenType.RightParen))
                        {
                            parenCount--;
                        }
                        else
                        {
                            Advance();
                        }
                    }
                }
            }
        }

        private List<Modifier> ParseModifiers()
        {
            var modifiers = new List<Modifier>();

            while (true)
            {
                if (Match(TokenType.Public)) modifiers.Add(Modifier.Public);
                else if (Match(TokenType.Private)) modifiers.Add(Modifier.Private);
                else if (Match(TokenType.Protected)) modifiers.Add(Modifier.Protected);
                else if (Match(TokenType.Internal)) modifiers.Add(Modifier.Internal);
                else if (Match(TokenType.Static)) modifiers.Add(Modifier.Static);
                else if (Match(TokenType.Abstract)) modifiers.Add(Modifier.Abstract);
                else if (Match(TokenType.Virtual)) modifiers.Add(Modifier.Virtual);
                else if (Match(TokenType.Override)) modifiers.Add(Modifier.Override);
                else if (Match(TokenType.Sealed)) modifiers.Add(Modifier.Sealed);
                else if (Match(TokenType.Readonly)) modifiers.Add(Modifier.Readonly);
                else if (Match(TokenType.Const)) modifiers.Add(Modifier.Const);
                else if (Match(TokenType.Volatile)) modifiers.Add(Modifier.Volatile);
                else if (Match(TokenType.Unsafe)) modifiers.Add(Modifier.Unsafe);
                else if (Match(TokenType.Async)) modifiers.Add(Modifier.Async);
                else if (Match(TokenType.Partial)) modifiers.Add(Modifier.Partial);
                else break;
            }

            return modifiers;
        }

                private TypeNode ParseFunctionType()
        {
            // Parse (param1, param2, ...) -> returnType
            Consume(TokenType.LeftParen, "Expected '(' for function type.");
            
            var paramTypes = new List<TypeNode>();
            if (!Check(TokenType.RightParen))
            {
                do
                {
                    paramTypes.Add(ParseType());
                } while (Match(TokenType.Comma));
            }
            
            Consume(TokenType.RightParen, "Expected ')' after function parameter types.");
            Consume(TokenType.Arrow, "Expected '->' in function type.");
            
            var returnType = ParseType();
            
            // Create a function type representation as a string
            var functionTypeName = $"({string.Join(", ", paramTypes.Select(p => p.Name))}) -> {returnType.Name}";
            return new TypeNode(functionTypeName);
        }

        private TypeNode ParseType()
        {
            // Check for function type syntax: (int, int) -> int
            if (Check(TokenType.LeftParen))
            {
                return ParseFunctionType();
            }

                            // Check for type qualifiers: volatile, const, static, etc.
            var typeQualifiers = new List<string>();
            Console.WriteLine($"DEBUG: ParseType() - starting type qualifier parsing, current token: {Current().Type} '{Current().Lexeme}'");
            while (true)
            {
                if (Match(TokenType.Volatile))
                {
                    Console.WriteLine($"DEBUG: ParseType() - found and consumed volatile qualifier");
                    typeQualifiers.Add("volatile");
                }
                else if (Check(TokenType.Identifier) && 
                        (Current().Lexeme == "const" || Current().Lexeme == "static" || 
                         Current().Lexeme == "extern" || Current().Lexeme == "inline" ||
                         Current().Lexeme == "restrict" || Current().Lexeme == "atomic"))
                {
                    Console.WriteLine($"DEBUG: ParseType() - found and consuming identifier qualifier: {Current().Lexeme}");
                    typeQualifiers.Add(Current().Lexeme);
                    Advance(); // consume the identifier qualifier
                }
                else
                {
                    Console.WriteLine($"DEBUG: ParseType() - no more qualifiers, current token: {Current().Type} '{Current().Lexeme}'");
                    break; // no more qualifiers
                }
            }
            Console.WriteLine($"DEBUG: ParseType() - collected {typeQualifiers.Count} qualifiers: [{string.Join(", ", typeQualifiers)}]");

            // We'll handle reference types later, after checking for arrays/slices
            bool isReference = false;
            bool isMutable = false;

            // Check for Rust-style fixed-size array syntax: [type; size]
            if (Check(TokenType.LeftBracket))
            {
                Advance(); // consume '['
                var elementType = ParseType();
                
                // Check for fixed-size array syntax [type; size]
                if (Match(TokenType.Semicolon))
                {
                    var sizeExpr = ParseExpression();
                    Consume(TokenType.RightBracket, "Expected ']' after array size.");
                    
                    // Create array type with fixed size notation
                    var arrayTypeName = $"[{elementType.Name}; size]";
                    var arrayType = new TypeNode(arrayTypeName, elementType.TypeArguments, true, 1, elementType.IsNullable, elementType.IsPointer, elementType.IsReference);
                    
                    return arrayType;
                }
                else
                {
                    // Just [type] - slice type
                    Consume(TokenType.RightBracket, "Expected ']' after slice type.");
                    var sliceTypeName = $"[{elementType.Name}]";
                    var sliceType = new TypeNode(sliceTypeName, elementType.TypeArguments, true, 1, elementType.IsNullable, elementType.IsPointer, elementType.IsReference);
                    
                    return sliceType;
                }
            }

            // Check for prefix pointer syntax (*char, *int, *mut T, *const T, etc.)
            bool isPointer = Match(TokenType.Multiply);
            
            // Check for prefix reference syntax (&char, &int, &T, &mut T, &[type], &mut [type], etc.)
            if (!isPointer && Match(TokenType.BitwiseAnd))
            {
                isReference = true;
                
                // Check for &mut
                if (Check(TokenType.Identifier) && Current().Lexeme == "mut")
                {
                    Advance(); // consume mut
                    isMutable = true;
                }
                
                // Check for array/slice syntax after reference: &[type] or &mut [type]
                if (Check(TokenType.LeftBracket))
                {
                    Advance(); // consume '['
                    var elementType = ParseType();
                    
                    // Check for fixed-size array syntax [type; size]
                    if (Match(TokenType.Semicolon))
                    {
                        var sizeExpr = ParseExpression();
                        Consume(TokenType.RightBracket, "Expected ']' after array size.");
                        
                        // Create reference to fixed-size array type
                        var arrayTypeName = $"[{elementType.Name}; size]";
                        var refPrefix = isMutable ? "&mut " : "&";
                        return new TypeNode(refPrefix + arrayTypeName, elementType.TypeArguments, true, 1, elementType.IsNullable, elementType.IsPointer, true);
                    }
                    else
                    {
                        // Just [type] - reference to slice type
                        Consume(TokenType.RightBracket, "Expected ']' after slice type.");
                        var sliceTypeName = $"[{elementType.Name}]";
                        var refPrefix = isMutable ? "&mut " : "&";
                        return new TypeNode(refPrefix + sliceTypeName, elementType.TypeArguments, true, 1, elementType.IsNullable, elementType.IsPointer, true);
                    }
                }
            }

            // Check for type qualifiers after pointer operator (*volatile, *const, *mut, etc.)
            var postPointerQualifiers = new List<string>();
            if (isPointer)
            {
                while (true)
                {
                    if (Match(TokenType.Volatile))
                    {
                        postPointerQualifiers.Add("volatile");
                    }
                    else if (Check(TokenType.Identifier) && 
                            (Current().Lexeme == "const" || Current().Lexeme == "mut" ||
                             Current().Lexeme == "static" || Current().Lexeme == "extern" || 
                             Current().Lexeme == "inline" || Current().Lexeme == "restrict" || 
                             Current().Lexeme == "atomic"))
                    {
                        postPointerQualifiers.Add(Current().Lexeme);
                        Advance(); // consume the identifier qualifier
                    }
                    else
                    {
                        break; // no more qualifiers
                    }
                }
            }
            
            string typeName;

            if (Match(TokenType.Void, TokenType.Bool, TokenType.Byte, TokenType.SByte,
                     TokenType.Short, TokenType.UShort, TokenType.Int, TokenType.UInt,
                     TokenType.Long, TokenType.ULong, TokenType.Float, TokenType.Double,
                     TokenType.Decimal, TokenType.Char, TokenType.String, TokenType.Object,
                     TokenType.Dynamic, TokenType.Var))
            {
                typeName = Previous().Type.ToString().ToLower();
            }
            else if (Match(TokenType.Vector, TokenType.Matrix, TokenType.Quaternion, TokenType.Transform))
            {
                typeName = Previous().Lexeme;
            }
            // Handle keywords that can be used as types with generics
            else if (Match(TokenType.Channel, TokenType.Thread, TokenType.Lock, TokenType.Atomic))
            {
                typeName = Previous().Type.ToString();
            }
            else if (Check(TokenType.Identifier))
            {
                // Special check: don't consume 'trait' if it looks like a trait declaration
                if (Current().Lexeme == "trait")
                {
                    // Check if this looks like a trait declaration: trait Name<T> or trait Name {
                    var nextToken = PeekNext();
                    if (nextToken != null && nextToken.Type == TokenType.Identifier)
                    {
                        // Could be a trait declaration, don't consume it as a type
                        throw Error(Current(), $"Expected type name, but found {Current().Type} '{Current().Lexeme}'.");
                    }
                }
                
                // Check if this identifier is a generic type parameter in scope
                if (_currentGenericTypeParameters.Contains(Current().Lexeme) || 
                    IsCommonGenericTypeParameter(Current().Lexeme))
            {
                    Console.WriteLine($"DEBUG: ParseType() - found generic type parameter '{Current().Lexeme}'");
                    Advance();
                typeName = Previous().Lexeme;
                }
                else
                {
                    // Regular identifier type name
                    Advance();
                    typeName = Previous().Lexeme;
                }
            }
            else
            {
                throw Error(Current(), $"Expected type name, but found {Current().Type} '{Current().Lexeme}'.");
            }

            // Generic type arguments
            List<TypeNode> typeArguments = null;
            if (Match(TokenType.Less))
            {
                typeArguments = new List<TypeNode>();
                do
                {
                    typeArguments.Add(ParseType());
                } while (Match(TokenType.Comma));
                
                Consume(TokenType.Greater, "Expected '>' after type arguments.");
            }

            // Array
            bool isArray = false;
            int arrayRank = 0;
            if (Match(TokenType.LeftBracket))
            {
                isArray = true;
                arrayRank = 1;
                
                while (Match(TokenType.Comma))
                {
                    arrayRank++;
                }
                
                Consume(TokenType.RightBracket, "Expected ']' after array rank.");
            }

            // Nullable
            bool isNullable = Match(TokenType.Question);

            // Support postfix pointer syntax as well (char*)
            if (!isPointer && Match(TokenType.Multiply))
            {
                isPointer = true;
            }

            // Create final type name with qualifiers
            var finalTypeName = typeName;
            
            // Combine both pre-pointer and post-pointer qualifiers
            var allQualifiers = new List<string>();
            allQualifiers.AddRange(typeQualifiers);
            
            // For pointer types, also check for qualifiers after the pointer
            // REMOVED DUPLICATE: var postPointerQualifiers = new List<string>();
            if (false) // isPointer - DISABLED DUPLICATE
            {
                while (true)
                {
                    if (Match(TokenType.Volatile))
                    {
                        postPointerQualifiers.Add("volatile");
                    }
                    else if (Check(TokenType.Identifier) && 
                            (Current().Lexeme == "const" || Current().Lexeme == "static" || 
                             Current().Lexeme == "extern" || Current().Lexeme == "inline" ||
                             Current().Lexeme == "restrict" || Current().Lexeme == "atomic"))
                    {
                        postPointerQualifiers.Add(Current().Lexeme);
                        Advance(); // consume the identifier qualifier
                    }
                    else
                    {
                        break; // no more qualifiers
                    }
                }
            }
            
            if (allQualifiers.Count > 0)
            {
                finalTypeName = string.Join(" ", allQualifiers) + " " + typeName;
            }

            var typeNode = new TypeNode(finalTypeName, typeArguments, isArray, arrayRank, isNullable, isPointer, isReference);
            
            // If this was a reference type, wrap it
            if (isReference)
            {
                var refPrefix = isMutable ? "&mut " : "&";
                return new TypeNode(refPrefix + typeNode.Name, typeNode.TypeArguments, typeNode.IsArray, typeNode.ArrayRank, typeNode.IsNullable, typeNode.IsPointer, true);
            }
            
            return typeNode;
        }

        private TypeNode PeekType()
        {
            // Try to peek if the next tokens form a type
            if (Check(TokenType.Void) || Check(TokenType.Bool) || Check(TokenType.Byte) ||
                Check(TokenType.SByte) || Check(TokenType.Short) || Check(TokenType.UShort) ||
                Check(TokenType.Int) || Check(TokenType.UInt) || Check(TokenType.Long) || 
                Check(TokenType.ULong) || Check(TokenType.Float) || Check(TokenType.Double) ||
                Check(TokenType.Decimal) || Check(TokenType.Char) || Check(TokenType.String) || 
                Check(TokenType.Object) || Check(TokenType.Dynamic) || Check(TokenType.Var))
            {
                return new TypeNode(Current().Type.ToString().ToLower());
            }
            
            if (Check(TokenType.Vector) || Check(TokenType.Matrix) || Check(TokenType.Quaternion) || Check(TokenType.Transform))
            {
                return new TypeNode(Current().Lexeme);
            }

            if (Check(TokenType.Identifier))
            {
                return new TypeNode(Current().Lexeme);
            }

            return null;
        }

        private List<Parameter> ParseParameters()
        {
            var parameters = new List<Parameter>();

            if (!Check(TokenType.RightParen))
            {
                do
                {
                    var modifier = ParameterModifier.None;
                    
                    if (Match(TokenType.Ref)) modifier = ParameterModifier.Ref;
                    else if (Match(TokenType.Out)) modifier = ParameterModifier.Out;
                    else if (Match(TokenType.In)) modifier = ParameterModifier.In;
                    else if (Match(TokenType.Params)) modifier = ParameterModifier.Params;

                    // Special handling for &self or &mut self parameters (Rust-style)
                    if (Check(TokenType.BitwiseAnd))
                    {
                        Advance(); // consume &
                        bool isMutable = false;
                        if (Check(TokenType.Identifier) && Current().Lexeme == "mut")
                        {
                            Advance(); // consume mut
                            isMutable = true;
                        }
                        if (Check(TokenType.Self))
                        {
                            var selfToken = Advance();
                            var selfType = new TypeNode("&self");
                            if (isMutable) selfType = new TypeNode("&mut self");
                            parameters.Add(new Parameter(selfType, "self", null, modifier));
                        }
                        else
                        {
                            // Not self, parse as regular & type
                            _current--; // back up to reparse the & as part of type
                            if (isMutable) _current--; // back up mut too
                            var type = ParseType();
                            var name = Consume(TokenType.Identifier, "Expected parameter name.");
                            Expression defaultValue = null;
                            if (Match(TokenType.Assign))
                            {
                                defaultValue = ParseAssignment();
                            }
                            parameters.Add(new Parameter(type, name.Lexeme, defaultValue, modifier));
                        }
                    }
                    else
                    {
                    var type = ParseType();
                    var name = Consume(TokenType.Identifier, "Expected parameter name.");

                    Expression defaultValue = null;
                    if (Match(TokenType.Assign))
                    {
                        defaultValue = ParseAssignment();
                    }

                    parameters.Add(new Parameter(type, name.Lexeme, defaultValue, modifier));
                    }
                } while (Match(TokenType.Comma));
            }

            Consume(TokenType.RightParen, "Expected ')' after parameters.");
            return parameters;
        }

        private List<TypeParameter> ParseTypeParameters()
        {
            if (!Match(TokenType.Less))
            {
                return new List<TypeParameter>();
            }

            var typeParameters = new List<TypeParameter>();

            do
            {
                bool isCovariant = Match(TokenType.Out);
                bool isContravariant = !isCovariant && Match(TokenType.In);

                var name = Consume(TokenType.Identifier, "Expected type parameter name.").Lexeme;

                var constraints = new List<TypeNode>();
                if (Match(TokenType.Colon))
                {
                    do
                    {
                        constraints.Add(ParseType());
                    } while (Match(TokenType.Comma));
                }

                typeParameters.Add(new TypeParameter(name, constraints, isCovariant, isContravariant));
            } while (Match(TokenType.Comma));

            Consume(TokenType.Greater, "Expected '>' after type parameters.");

            return typeParameters;
        }

        private bool Match(params TokenType[] types)
        {
            foreach (var type in types)
            {
                if (Check(type))
                {
                    Advance();
                    return true;
                }
            }
            return false;
        }

        private bool Check(TokenType type)
        {
            if (IsAtEnd()) return false;
            return Current().Type == type;
        }

        private Token Advance()
        {
            if (!IsAtEnd()) _current++;
            return Previous();
        }

        private bool IsAtEnd()
        {
            return Current().Type == TokenType.EndOfFile;
        }

        private Token Current()
        {
            return _tokens[_current];
        }

        private Token Previous()
        {
            return _tokens[_current - 1];
        }

        private Token PeekNext()
        {
            if (_current + 1 >= _tokens.Count) return null;
            return _tokens[_current + 1];
        }

        private Token PeekPrevious()
        {
            if (_current - 1 < 0) return null;
            return _tokens[_current - 1];
        }

        private Token Consume(TokenType type, string message)
        {
            if (Check(type)) return Advance();
            throw Error(Current(), message + $" (got {Current().Type} '{Current().Lexeme}')");
        }

        private ParseException Error(Token token, string message)
        {
            var error = new ParseException(message, token);
            RecordError(error);
            return error;
        }

        private void Synchronize()
        {
            // Enhanced error recovery - synchronize to next likely statement boundary
            Advance();

            while (!IsAtEnd())
            {
                // If we hit a semicolon, we're likely at the end of a statement
                if (Previous().Type == TokenType.Semicolon) return;

                // Check for statement/declaration keywords that indicate a new statement
                switch (Current().Type)
                {
                    // Declaration keywords
                    case TokenType.Class:
                    case TokenType.Function:
                    case TokenType.Struct:
                    case TokenType.Interface:
                    case TokenType.Enum:
                    case TokenType.Namespace:
                    case TokenType.Using:
                    case TokenType.Import:
                    case TokenType.Module:
                    case TokenType.Domain:
                    case TokenType.Component:
                    case TokenType.System:
                    case TokenType.Entity:
                    case TokenType.Const:
                    case TokenType.Alias:
                    case TokenType.Type:
                    // Statement keywords
                    case TokenType.If:
                    case TokenType.While:
                    case TokenType.For:
                    case TokenType.Do:
                    case TokenType.Switch:
                    case TokenType.Return:
                    case TokenType.Break:
                    case TokenType.Continue:
                    case TokenType.Try:
                    case TokenType.Throw:
                    case TokenType.Yield:
                    case TokenType.Var:
                    // Modifiers that typically start declarations
                    case TokenType.Public:
                    case TokenType.Private:
                    case TokenType.Protected:
                    case TokenType.Static:
                    case TokenType.Abstract:
                    case TokenType.Virtual:
                    case TokenType.Override:
                    case TokenType.Async:
                    // Syntax level markers
                    case TokenType.HighLevel:
                    case TokenType.MediumLevel:
                    case TokenType.LowLevel:
                    case TokenType.Assembly:
                        return;
                        
                    // Also stop at closing braces as they often end blocks
                    case TokenType.RightBrace:
                        return;
                }

                Advance();
            }
        }

        // Helper method to consume an identifier or Greek letter
        private Token ConsumeIdentifierOrGreekLetter(string message)
        {
            // Check for regular identifier
            if (Check(TokenType.Identifier))
            {
                return Advance();
            }
            
            // Check for Greek letters and math symbols
            if (IsGreekLetterOrMathSymbol(Current().Type))
            {
                return Advance();
            }
            
            // Allow certain keywords to be used as identifiers in variable names
            // This is common in languages where contextual keywords can be identifiers
            if (Check(TokenType.Data) || Check(TokenType.Component) || Check(TokenType.System) || 
                Check(TokenType.Entity) || Check(TokenType.Numbers) || Check(TokenType.Length) ||
                Check(TokenType.Width) || Check(TokenType.Area) || Check(TokenType.Counter) ||
                Check(TokenType.Pin))  // Allow 'pin' as property name
            {
                return Advance();
            }
            
            throw Error(Current(), message);
        }
        
        // Helper method to check if a token type is a Greek letter or math symbol
        private bool IsGreekLetterOrMathSymbol(TokenType type)
        {
            switch (type)
            {
                // Greek letters
                case TokenType.Pi:
                case TokenType.Tau:
                case TokenType.Epsilon:
                case TokenType.Phi:
                case TokenType.Gamma:
                case TokenType.Rho:
                case TokenType.Delta:
                case TokenType.Alpha:
                case TokenType.Theta:
                case TokenType.Mu:
                case TokenType.Sigma:
                case TokenType.Omega:
                case TokenType.Lambda:
                case TokenType.Beta:
                case TokenType.Eta:
                case TokenType.Kappa:
                case TokenType.Nu:
                case TokenType.Xi:
                case TokenType.Omicron:
                case TokenType.Upsilon:
                case TokenType.Chi:
                case TokenType.Psi:
                case TokenType.Zeta:
                case TokenType.Iota:
                // Math symbols
                case TokenType.Infinity:
                case TokenType.Integral:
                case TokenType.Summation:
                case TokenType.Product:
                case TokenType.SquareRoot:
                case TokenType.CubeRoot:
                case TokenType.PartialDerivative:
                case TokenType.Nabla:
                    return true;
                default:
                    return false;
            }
        }
        
        // Helper method to check if a token represents a known type name
        private bool IsKnownTypeName(Token token)
        {
            switch (token.Type)
            {
                // Built-in types
                case TokenType.Void:
                case TokenType.Bool:
                case TokenType.Byte:
                case TokenType.SByte:
                case TokenType.Short:
                case TokenType.UShort:
                case TokenType.Int:
                case TokenType.UInt:
                case TokenType.Long:
                case TokenType.ULong:
                case TokenType.Float:
                case TokenType.Double:
                case TokenType.Decimal:
                case TokenType.Char:
                case TokenType.String:
                case TokenType.Object:
                case TokenType.Dynamic:
                case TokenType.Var:
                    return true;
                // For identifiers, we could check against a list of known type names
                // but for now, we'll be conservative and assume identifiers are parameter names
                // unless they're followed by another identifier
                case TokenType.Identifier:
                    // We could add a more sophisticated check here if needed
                    return false;
                default:
                    return false;
            }
        }
        
        // Helper method to check if an identifier token looks like a numeric type suffix
        private bool IsTypeSuffixIdentifier(Token token)
        {
            if (token.Type != TokenType.Identifier)
                return false;
                
            var lexeme = token.Lexeme.ToLower();
            return lexeme == "u8" || lexeme == "u16" || lexeme == "u32" || lexeme == "u64" ||
                   lexeme == "i8" || lexeme == "i16" || lexeme == "i32" || lexeme == "i64" ||
                   lexeme == "f32" || lexeme == "f64" || lexeme == "usize" || lexeme == "isize";
        }

        private Token ConsumeIdentifier(string message)
        {
            // Allow reserved keywords that can be used as identifiers
            if (Check(TokenType.Identifier) || 
                Check(TokenType.Length) || Check(TokenType.Width) || Check(TokenType.Area) ||
                Check(TokenType.Numbers) || Check(TokenType.Counter) || Check(TokenType.Data) ||
                Check(TokenType.System) || Check(TokenType.Component) || Check(TokenType.Entity) ||
                Check(TokenType.UnionKeyword) ||  // Allow 'union' as variable name
                Check(TokenType.Limit) ||          // Allow 'limit' as variable name
                Check(TokenType.Channel) ||        // Allow 'Channel' as variable name
                Check(TokenType.Thread) ||         // Allow 'Thread' as variable name
                Check(TokenType.Lock) ||           // Allow 'Lock' as variable name
                Check(TokenType.Atomic) ||         // Allow 'Atomic' as variable name
                IsGreekLetterOrMathSymbol(Current().Type))
            {
                return Advance();
            }
            
            throw Error(Current(), message);
        }

        // Public methods needed by MediumLevelParser
        public int GetCurrentPosition()
        {
            return _current;
        }
        
        public void SetPosition(int position)
        {
            _current = position;
        }
        
        // Make key parsing methods accessible to other parsers
        public bool PublicMatch(params TokenType[] types) => Match(types);
        public bool PublicCheck(TokenType type) => Check(type);
        public Token PublicAdvance() => Advance();
        public bool PublicIsAtEnd() => IsAtEnd();
        public Token PublicCurrent() => Current();
        public Token PublicPrevious() => Previous();
        public Token PublicConsume(TokenType type, string message) => Consume(type, message);
        public Expression PublicParseExpression() => ParseExpression();
        public Expression PublicParseAssignment() => ParseAssignment();

        #endregion

        // Helper method to detect variable declaration patterns vs assignments
        private bool IsVariableDeclarationPattern()
        {
            // Don't treat as variable declaration if this is clearly an assignment
            if (Check(TokenType.Identifier) && PeekNext() != null)
            {
                var nextToken = PeekNext().Type;
                // If we see identifier[...] it's an array access assignment, not a declaration
                if (nextToken == TokenType.LeftBracket)
                {
                    return false;
                }
                // If we see identifier = it might be assignment, check if it looks like a type
                if (nextToken == TokenType.Assign)
                {
                    return false;
                }
                // If we see identifier: it's likely a variable declaration with type annotation
                if (nextToken == TokenType.Colon)
                {
                    return true;
                }
            }
            
            // Use PeekType for other cases, but be more conservative
            return PeekType() != null && !Check(TokenType.Identifier);
        }

        // Helper method to check if an identifier is a common generic type parameter
        private bool IsCommonGenericTypeParameter(string name)
        {
            // Common single-letter generic type parameters
            return name.Length == 1 && "TUVWKR".Contains(name);
        }

        private Expression ParseStructLiteral(Token structName)
        {
            // structName '{' field: value, ... '}'
            Consume(TokenType.LeftBrace, "Expected '{' after struct name");
            
            var fields = new Dictionary<string, Expression>();
            
            // Parse field initializers
            while (!Check(TokenType.RightBrace))
            {
                // Parse field name
                var fieldName = Consume(TokenType.Identifier, "Expected field name");
                Consume(TokenType.Colon, "Expected ':' after field name");
                
                // Parse field value
                var value = ParseExpression();
                fields[fieldName.Lexeme] = value;
                
                // Check for comma or end of struct
                if (!Check(TokenType.RightBrace))
                {
                    Consume(TokenType.Comma, "Expected ',' after field initializer");
                }
            }
            
            Consume(TokenType.RightBrace, "Expected '}' after struct literal");
            
            return new StructLiteral(structName, fields);
        }
        
        private TypeNode InferTypeFromExpression(Expression expr)
        {
            // Infer type from the initializer expression
            switch (expr)
            {
                case LiteralExpression literal:
                    if (literal.Token.Type == TokenType.IntegerLiteral)
                        return new TypeNode("int");
                    if (literal.Token.Type == TokenType.FloatLiteral)
                        return new TypeNode("float");
                    if (literal.Token.Type == TokenType.DoubleLiteral)
                        return new TypeNode("double");
                    if (literal.Token.Type == TokenType.StringLiteral || literal.Token.Type == TokenType.InterpolatedString)
                        return new TypeNode("string");
                    if (literal.Token.Type == TokenType.BooleanLiteral)
                        return new TypeNode("bool");
                    if (literal.Token.Type == TokenType.NullLiteral)
                        return new TypeNode("object");
                    if (literal.Token.Type == TokenType.UnitLiteral)
                        return new TypeNode("Unit");
                    break;
                    
                case ArrayExpression array:
                    if (array.Elements.Count > 0)
                    {
                        var elementType = InferTypeFromExpression(array.Elements[0]);
                        return new TypeNode(elementType.Name + "[]", null, true, 1);
                    }
                    return new TypeNode("object[]", null, true, 1);
                    
                case NewExpression newExpr:
                    return newExpr.Type;
                    
                case CallExpression call:
                    // For now, assume function calls return 'var'
                    return new TypeNode("var");
                    
                case BinaryExpression binary:
                    // For arithmetic operations, infer from operands
                    if (IsArithmeticOperator(binary.Operator))
                    {
                        var leftType = InferTypeFromExpression(binary.Left);
                        var rightType = InferTypeFromExpression(binary.Right);
                        return GetWidestNumericType(leftType, rightType);
                    }
                    // Comparison operations return bool
                    if (IsComparisonOperator(binary.Operator))
                    {
                        return new TypeNode("bool");
                    }
                    break;
                    
                case VectorExpression:
                    return new TypeNode("Vector");
                    
                case MatrixExpression:
                    return new TypeNode("Matrix");
                    
                case QuaternionExpression:
                    return new TypeNode("Quaternion");
            }
            
            // Default to 'var' for unknown types
            return new TypeNode("var");
        }
        
        private bool IsArithmeticOperator(Token op)
        {
            return op.Type == TokenType.Plus || op.Type == TokenType.Minus ||
                   op.Type == TokenType.Multiply || op.Type == TokenType.Divide ||
                   op.Type == TokenType.Modulo || op.Type == TokenType.Power;
        }
        
        private bool IsComparisonOperator(Token op)
        {
            return op.Type == TokenType.Equal || op.Type == TokenType.NotEqual ||
                   op.Type == TokenType.Less || op.Type == TokenType.Greater ||
                   op.Type == TokenType.LessEqual || op.Type == TokenType.GreaterEqual;
        }
        
        private TypeNode GetWidestNumericType(TypeNode left, TypeNode right)
        {
            // Numeric type promotion rules
            if (left.Name == "double" || right.Name == "double")
                return new TypeNode("double");
            if (left.Name == "float" || right.Name == "float")
                return new TypeNode("float");
            if (left.Name == "long" || right.Name == "long")
                return new TypeNode("long");
            if (left.Name == "int" || right.Name == "int")
                return new TypeNode("int");
            
            // Default to var if types are unknown
            return new TypeNode("var");
        }
    }

    public class ParseException : Exception
    {
        public Token? Token { get; }
        
        public ParseException(string message) : base(message) { }
        
        public ParseException(string message, Token token) : base(message)
        {
            Token = token;
        }
    }

    // Pattern classes for pattern matching
    public class ConstantPattern : Pattern
    {
        public Expression Value { get; }

        public ConstantPattern(Expression value)
        {
            Value = value;
        }
    }
} 

