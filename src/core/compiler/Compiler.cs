using System;
using System.Collections.Generic;
using System.Linq;
using Ouro.Core.AST;
using Ouro.Core.VM;
using Ouro.Tools;
using Ouro.Tokens;

namespace Ouro.Core.Compiler
{
    /// <summary>
    /// Main compiler that generates bytecode from AST
    /// </summary>
    public class Compiler : IAstVisitor<object?>
    {
        private BytecodeBuilder builder;
        private SymbolTable symbols;
        private TypeChecker typeChecker;
        private OptimizationLevel optimizationLevel;
        private List<CompilerError> errors;
        private CompilerContext context;
        private Dictionary<string, Type> importedTypes;
        private CompiledProgram? program;
        
        public Compiler(OptimizationLevel optimization = OptimizationLevel.Release)
        {
            optimizationLevel = optimization;
            errors = new List<CompilerError>();
            context = new CompilerContext();
            symbols = new SymbolTable();  // Initialize symbols
            importedTypes = new Dictionary<string, Type>();
            
            builder = new BytecodeBuilder();
            typeChecker = new TypeChecker();
            
            // Register built-in global symbols
            RegisterBuiltins();
        }
        
        private void RegisterBuiltins()
        {
            // Register basic types in importedTypes so they're treated as types, not local variables
            importedTypes["double"] = typeof(System.Double);
            importedTypes["int"] = typeof(System.Int32);
            importedTypes["string"] = typeof(System.String);
            importedTypes["bool"] = typeof(System.Boolean);
            importedTypes["object"] = typeof(System.Object);
            importedTypes["void"] = typeof(void);
            
            // Register UI types and functions in importedTypes
            importedTypes["UIBuiltins"] = typeof(Ouro.StdLib.UI.UIBuiltins);
            importedTypes["Console"] = typeof(System.Console);
            importedTypes["console"] = typeof(System.Console);  // Add lowercase console alias
            importedTypes["Math"] = typeof(System.Math);
            importedTypes["MathFunctions"] = typeof(Ouro.StdLib.Math.MathFunctions);
            importedTypes["Thread"] = typeof(System.Threading.Thread);
            
            // Register console functions
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
            symbols.Define("print", null);
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
            symbols.Define("println", null);
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
            
            // Register math constants and functions
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
            symbols.Define("PI", null);
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
            symbols.Define("E", null);
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
            
            // Register parsing functions
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
            symbols.Define("parseNumber", null);
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
            symbols.Define("formatNumber", null);
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
            
            // Register additional built-in symbols that might be used
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
            symbols.Define("true", null);
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
            symbols.Define("false", null);
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
            symbols.Define("null", null);
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
        }
        
        public CompiledProgram Compile(object ast)
        {
            if (ast == null)
                throw new ArgumentNullException(nameof(ast));

            builder = new BytecodeBuilder();
            symbols = new SymbolTable();
            context = new CompilerContext { Symbols = symbols };
            importedTypes = new Dictionary<string, Type>();
            
            // Initialize the program for function storage
            this.program = new CompiledProgram
            {
                Functions = new Dictionary<string, FunctionInfo>(),
                SymbolTable = symbols,
                Metadata = new CompilerMetadata
                {
                    Version = "1.0.0",
                    CompileTime = DateTime.Now
                }
            };

            RegisterBuiltins();

            Logger.Debug($"Starting compilation of AST: {ast.GetType().Name}");

            try
            {
                var astType = ast.GetType();
                Logger.Debug($"AST type: {astType.Name}, ContainsGenericParameters: {astType.ContainsGenericParameters}");
                
                if (astType.ContainsGenericParameters)
                {
                    Logger.Debug($"Skipping AST compilation for generic type {astType.Name}");
                    throw new CompilerException($"Cannot compile generic AST type definition {astType.Name}");
                }
                
                try 
                {
                    // Instead of using reflection to call the generic Accept method,
                    // use the visitor pattern directly since we know the types
                    if (ast is Ouro.Core.AST.Program program)
                    {
                        Logger.Debug($"Calling VisitProgram directly on Program AST");
                        this.VisitProgram(program);
                    }
                    else
                    {
                        // For other AST nodes, try the reflection approach but with proper generic handling
                        var acceptMethod = astType.GetMethod("Accept");
                        if (acceptMethod != null && !acceptMethod.ContainsGenericParameters)
                        {
                            acceptMethod.Invoke(ast, new object[] { this });
                        }
                        else
                        {
                            Logger.Debug($"Cannot invoke generic Accept method on {astType.Name}");
                            throw new CompilerException($"Cannot compile AST node of type {astType.Name} - generic Accept method not supported");
                        }
                    }
                }
                catch (Exception innerEx)
                {
                    Logger.Debug($"Inner exception during AST compilation: {innerEx.Message}");
                    Logger.Debug($"Inner exception type: {innerEx.GetType().Name}");
                    Logger.Debug($"Inner stack trace: {innerEx.StackTrace}");
                    throw;
                }
                
                var bytecode = builder.GetBytecode();

                var compiledProgram = new CompiledProgram
                {
                    Bytecode = bytecode,
                    Functions = this.program.Functions, // Use the functions we collected during compilation
                    SymbolTable = symbols,
                    SourceFile = context.SourceFile,
                    Metadata = new CompilerMetadata
                    {
                        Version = "1.0.0",
                        CompileTime = DateTime.Now
                    }
                };

                Logger.Debug($"Code generation completed successfully");
                Logger.Debug($"CompiledProgram.Functions contains {compiledProgram.Functions.Count} functions");
                
                return compiledProgram;
            }
            catch (Exception ex)
            {
                Logger.Debug($"Compilation failed: {ex.Message}");
                throw new CompilerException($"Compilation failed: {ex.Message}");
            }
        }
        
        private void RegisterImportedTypes(string modulePath)
        {
            // Register types from the imported module
            switch (modulePath)
            {
                case "System":
                    importedTypes["Console"] = typeof(System.Console);
                    importedTypes["DateTime"] = typeof(System.DateTime);
                    importedTypes["Math"] = typeof(System.Math);
                    importedTypes["Convert"] = typeof(System.Convert);
                    importedTypes["Exception"] = typeof(System.Exception);
                    importedTypes["DivideByZeroException"] = typeof(System.DivideByZeroException);
                    importedTypes["NullReferenceException"] = typeof(System.NullReferenceException);
                    importedTypes["double"] = typeof(System.Double);
                    importedTypes["int"] = typeof(System.Int32);
                    importedTypes["string"] = typeof(System.String);
                    break;
                    
                case "Ouro.StdLib.IO":
                    importedTypes["File"] = typeof(System.IO.File);
                    importedTypes["Directory"] = typeof(System.IO.Directory);
                    importedTypes["Path"] = typeof(System.IO.Path);
                    break;
                    
                case "Ouro.StdLib.Math":
                    importedTypes["MathSymbols"] = typeof(Ouro.StdLib.Math.MathSymbols);
                    importedTypes["Vector"] = typeof(Ouro.StdLib.Math.Vector);
                    importedTypes["Matrix"] = typeof(Ouro.StdLib.Math.Matrix);
                    importedTypes["Quaternion"] = typeof(Ouro.StdLib.Math.Quaternion);
                    importedTypes["Transform"] = typeof(Ouro.StdLib.Math.Transform);
                    importedTypes["Complex"] = typeof(Ouro.StdLib.Math.MathSymbols.Complex);
                    break;
                    
                case "Ouro.StdLib.UI":
                    importedTypes["UIBuiltins"] = typeof(Ouro.StdLib.UI.UIBuiltins);
                    importedTypes["Window"] = typeof(Ouro.StdLib.UI.Window);
                    importedTypes["Button"] = typeof(Ouro.StdLib.UI.Button);
                    importedTypes["Label"] = typeof(Ouro.StdLib.UI.Label);
                    importedTypes["TextBox"] = typeof(Ouro.StdLib.UI.TextBox);
                    importedTypes["MenuBar"] = typeof(Ouro.StdLib.UI.MenuBar);
                    importedTypes["ToolBar"] = typeof(Ouro.StdLib.UI.ToolBar);
                    importedTypes["TabControl"] = typeof(Ouro.StdLib.UI.TabControl);
                    importedTypes["TabPage"] = typeof(Ouro.StdLib.UI.TabPage);
                    importedTypes["CheckBox"] = typeof(Ouro.StdLib.UI.CheckBox);
                    importedTypes["RadioButton"] = typeof(Ouro.StdLib.UI.RadioButton);
                    importedTypes["Slider"] = typeof(Ouro.StdLib.UI.Slider);
                    importedTypes["ProgressBar"] = typeof(Ouro.StdLib.UI.ProgressBar);
                    importedTypes["ComboBox"] = typeof(Ouro.StdLib.UI.ComboBox);
                    importedTypes["NumericUpDown"] = typeof(Ouro.StdLib.UI.NumericUpDown);
                    importedTypes["DatePicker"] = typeof(Ouro.StdLib.UI.DatePicker);
                    importedTypes["ColorPicker"] = typeof(Ouro.StdLib.UI.ColorPicker);
                    importedTypes["Theme"] = typeof(Ouro.StdLib.UI.Theme);
                    break;
                    
                case "Ouro.StdLib.System":
                    importedTypes["Console"] = typeof(System.Console);
                    importedTypes["DateTime"] = typeof(System.DateTime);
                    importedTypes["Environment"] = typeof(System.Environment);
                    break;
                    
                case "Ouro.StdLib.Collections":
                    // Don't register open generic types as they cause ContainsGenericParameters errors
                    // Instead register them only when used with specific type arguments
                    Logger.Debug("Skipping generic collection types to avoid ContainsGenericParameters errors");
                    // importedTypes["List"] = typeof(System.Collections.Generic.List<>);
                    // importedTypes["Dictionary"] = typeof(System.Collections.Generic.Dictionary<,>);
                    // importedTypes["Stack"] = typeof(System.Collections.Generic.Stack<>);
                    // importedTypes["Queue"] = typeof(System.Collections.Generic.Queue<>);
                    // importedTypes["HashSet"] = typeof(System.Collections.Generic.HashSet<>);
                    break;
            }
        }
        
        #region Expression Compilation
        
        public object? VisitBinaryExpression(BinaryExpression expr)
        {
            // Handle mathematical symbol preprocessing
            if (expr.Operator.Type == TokenType.Times && expr.Left is IdentifierExpression leftId && expr.Right is IdentifierExpression rightId)
            {
                // Check for mathematical cross product notation (F⃗ × r⃗)
                if (leftId.Name.EndsWith("⃗") && rightId.Name.EndsWith("⃗"))
                {
                    expr.Left.Accept(this);
                    expr.Right.Accept(this);
                    builder.Emit(Opcode.CrossProduct3D);
                    return null;
                }
            }
            
            if (expr.Operator.Type == TokenType.Dot3D)
            {
                // Dot product operator (⋅)
                expr.Left.Accept(this);
                expr.Right.Accept(this);
                builder.Emit(Opcode.DotProduct3D);
                return null;
            }
            
            // Handle short-circuit logical operators specially
            if (expr.Operator.Type == TokenType.LogicalAnd)
            {
                expr.Left.Accept(this);
                var falseJump = builder.EmitJump(Opcode.JumpIfFalse);
                
                // If left is true, evaluate right
                builder.Emit(Opcode.Pop); // Remove left operand
                expr.Right.Accept(this);
                
                var endJump = builder.EmitJump(Opcode.Jump);
                builder.PatchJump(falseJump);
                
                // If left is false, result is false (left operand is still on stack)
                builder.PatchJump(endJump);
                return null;
            }
            
            if (expr.Operator.Type == TokenType.LogicalOr)
            {
                expr.Left.Accept(this);
                var trueJump = builder.EmitJump(Opcode.JumpIfTrue);
                
                // If left is false, evaluate right
                builder.Emit(Opcode.Pop); // Remove left operand
                expr.Right.Accept(this);
                
                var endJump = builder.EmitJump(Opcode.Jump);
                builder.PatchJump(trueJump);
                
                // If left is true, result is true (left operand is still on stack)
                builder.PatchJump(endJump);
                return null;
            }
            
            // Compile operands for other operators
            expr.Left.Accept(this);
            expr.Right.Accept(this);
            
            // Generate operation
            switch (expr.Operator.Type)
            {
                case TokenType.Plus:
                    builder.Emit(Opcode.Add);
                    break;
                case TokenType.Minus:
                    builder.Emit(Opcode.Subtract);
                    break;
                case TokenType.Multiply:
                    builder.Emit(Opcode.Multiply);
                    break;
                case TokenType.Divide:
                    builder.Emit(Opcode.Divide);
                    break;
                case TokenType.Modulo:
                    builder.Emit(Opcode.Modulo);
                    break;
                case TokenType.Power: // ** operator
                    builder.Emit(Opcode.Power);
                    break;
                case TokenType.IntegerDivide: // // operator (integer division)
                    builder.Emit(Opcode.IntegerDivision);
                    break;
                    
                // Comparison operators
                case TokenType.Equal:
                    builder.Emit(Opcode.Equal);
                    break;
                case TokenType.NotEqual:
                case TokenType.NotEqual2:
                    builder.Emit(Opcode.NotEqual);
                    break;
                case TokenType.Less:
                    builder.Emit(Opcode.Less);
                    break;
                case TokenType.Greater:
                    builder.Emit(Opcode.Greater);
                    break;
                case TokenType.LessEqual:
                case TokenType.LessOrEqual:
                    builder.Emit(Opcode.LessEqual);
                    break;
                case TokenType.GreaterEqual:
                case TokenType.GreaterOrEqual:
                    builder.Emit(Opcode.GreaterEqual);
                    break;
                    
                // Logical operators
                case TokenType.LogicalAnd:
                    builder.Emit(Opcode.LogicalAnd);
                    break;
                case TokenType.LogicalOr:
                    builder.Emit(Opcode.LogicalOr);
                    break;
                    
                // Bitwise operators
                case TokenType.BitwiseAnd:
                    // Handle address-of operator (&) in unary context
                    builder.Emit(Opcode.AddressOf);
                    break;
                case TokenType.BitwiseOr:
                    builder.Emit(Opcode.BitwiseOr);
                    break;
                case TokenType.BitwiseXor:
                    builder.Emit(Opcode.BitwiseXor);
                    break;
                case TokenType.LeftShift:
                    builder.Emit(Opcode.LeftShift);
                    break;
                case TokenType.RightShift:
                    builder.Emit(Opcode.RightShift);
                    break;
                    
                // Special operators
                case TokenType.NullCoalesce:
                    builder.Emit(Opcode.NullCoalesce);
                    break;
                case TokenType.Spaceship: // <=> operator (three-way comparison)
                    builder.Emit(Opcode.SpaceshipCompare);
                    break;
                    
                // Array indexing
                case TokenType.RightBracket:
                    // For array[index], left is array, right is index
                    // Both are already on the stack from above
                    builder.Emit(Opcode.LoadElement);
                    break;
                    
                // Set operations
                case TokenType.Union:
                    builder.Emit(Opcode.SetUnion);
                    break;
                    
                case TokenType.Intersection:
                    builder.Emit(Opcode.SetIntersection);
                    break;
                    
                case TokenType.Element:
                    builder.Emit(Opcode.ElementOf);
                    break;
                    
                case TokenType.NotElement:
                    // Generate element check and then negate result
                    builder.Emit(Opcode.ElementOf);
                    builder.Emit(Opcode.LogicalNot);
                    break;
                    
                case TokenType.SetDifference:
                    builder.Emit(Opcode.SetDifference);
                    break;
                    
                // The 'in' operator - check if left value is in right collection
                case TokenType.In:
                    builder.Emit(Opcode.ElementOf);
                    break;
                    
                // Mathematical symbols
                case TokenType.Times: // × symbol for cross product when used with vectors
                    // Default to multiplication, cross product handled above based on context
                    builder.Emit(Opcode.Multiply);
                    break;
                    
                case TokenType.Dot3D: // ⋅ symbol for dot product
                    // Handled above before operand compilation
                    builder.Emit(Opcode.DotProduct3D);
                    break;
                    
                // Range operators
                case TokenType.Range: // .. operator (inclusive range)
                    builder.Emit(Opcode.MakeRange, 0); // 0 = inclusive
                    break;
                    
                case TokenType.Spread: // ... operator (exclusive range or spread)
                    // For spread in expressions, treat as exclusive range
                    // In other contexts (like function arguments), this would be handled differently
                    builder.Emit(Opcode.MakeRange, 1); // 1 = exclusive
                    break;
                    
                default:
                    throw new CompilerException($"Unknown binary operator: {expr.Operator.Type}");
            }
            
            return null;
        }
        
        public object? VisitUnaryExpression(UnaryExpression expr)
        {
            // Handle pointer dereference specially
            if (expr.Operator.Type == TokenType.Multiply)
            {
                // Pointer dereference: *p
                expr.Operand.Accept(this);  // Load pointer
                builder.Emit(Opcode.LoadConstant, builder.AddConstant(0));  // Index 0
                builder.Emit(Opcode.LoadElement);  // Load value at pointer
                return null;
            }
            
            expr.Operand.Accept(this);
            
            switch (expr.Operator.Type)
            {
                case TokenType.Plus:
                    // Unary plus: no operation needed, value is already on stack
                    break;
                case TokenType.Minus:
                    builder.Emit(Opcode.Negate);
                    break;
                case TokenType.LogicalNot:
                    builder.Emit(Opcode.LogicalNot);
                    break;
                case TokenType.BitwiseNot:
                    builder.Emit(Opcode.BitwiseNot);
                    break;
                case TokenType.Increment:
                    if (expr.IsPrefix)
                        builder.Emit(Opcode.PreIncrement);
                    else
                        builder.Emit(Opcode.PostIncrement);
                    break;
                case TokenType.Decrement:
                    if (expr.IsPrefix)
                        builder.Emit(Opcode.PreDecrement);
                    else
                        builder.Emit(Opcode.PostDecrement);
                    break;
                case TokenType.BitwiseAnd:
                    // Address-of operator: &variable
                    builder.Emit(Opcode.AddressOf);
                    break;
                case TokenType.PartialDerivative:
                    // Partial derivative operator: ∂f/∂x
                    builder.Emit(Opcode.PartialDerivative);
                    break;
                case TokenType.Nabla:
                    // Nabla/gradient operator: ∇f
                    builder.Emit(Opcode.Gradient);
                    break;
                case TokenType.Mu:
                    // Mu operator: μ for statistical mean, micro notation
                    builder.Emit(Opcode.Mean);
                    break;
                case TokenType.Sigma:
                    // Lowercase sigma operator: σ for standard deviation
                    builder.Emit(Opcode.StandardDeviation);
                    break;
                case TokenType.Summation:
                    // Uppercase Sigma operator: Σ for summation notation
                    builder.Emit(Opcode.Summation);
                    break;
                case TokenType.Identifier:
                    // Handle compound mathematical operators like σ²
                    if (expr.Operator.Lexeme == "σ²")
                    {
                        // Variance operator: σ²
                        builder.Emit(Opcode.Variance);
                    }
                    else
                    {
                        throw new CompilerException($"Unknown unary identifier operator: {expr.Operator.Lexeme}");
                    }
                    break;
                case TokenType.Rho:
                    // Rho operator: ρ for correlation
                    builder.Emit(Opcode.Correlation);
                    break;
                default:
                    throw new CompilerException($"Unknown unary operator: {expr.Operator.Type}");
            }
            
            return null;
        }
        
        public object? VisitLiteralExpression(LiteralExpression expr)
        {
            var index = builder.AddConstant(expr.Value);
            Logger.Debug($"Added literal constant '{expr.Value}' at index {index}");
            builder.Emit(Opcode.LoadConstant, index);
            return null;
        }
        
        public object? VisitIdentifierExpression(IdentifierExpression expr)
        {
            // Check if it's 'this'
            if (expr.Name == "this")
            {
                builder.Emit(Opcode.LoadThis);
                return null;
            }
            
            // Check if it's an imported type
            if (importedTypes.ContainsKey(expr.Name))
            {
                var typeConstantIndex = builder.AddConstant(importedTypes[expr.Name]);
                builder.Emit(Opcode.LoadConstant, typeConstantIndex);
                return null;
            }

            // CRITICAL Trace the exact failure point
            var symbol = symbols.Lookup(expr.Name);
            Logger.Debug($"VisitIdentifierExpression for '{expr.Name}': symbol={symbol} (null={symbol == null})");
            
            // Check if it's a type or if symbol was not found 
            if (symbol != null && string.Equals(symbol.Type?.ToString(), "type", StringComparison.Ordinal))
            {
                var typeConstantIndex = builder.AddConstant(symbol.Type ?? "type");
                builder.Emit(Opcode.LoadConstant, typeConstantIndex);
                return null;
            }
            
            // If symbol found, emit load instruction
            if (symbol != null)
            {
                Logger.Debug($"Symbol '{expr.Name}' found successfully - emitting load instruction");
                if (symbol.IsGlobal)
                    builder.Emit(Opcode.LoadGlobal, symbol.Index);
                else
                    builder.Emit(Opcode.LoadLocal, symbol.Index);
                    
                return null;
            }
            
            // Symbol is null - auto-define it
            Logger.Debug($"Symbol '{expr.Name}' not found - auto-defining as local variable");
            
            // Auto-define undefined variables as local variables with inferred type
            var newSymbol = symbols.Define(expr.Name, "var");
            
            // Initialize with null value
            builder.Emit(Opcode.LoadNull);
            if (newSymbol.IsGlobal)
                builder.Emit(Opcode.StoreGlobal, newSymbol.Index);
            else
                builder.Emit(Opcode.StoreLocal, newSymbol.Index);
            
            // Now load it
            if (newSymbol.IsGlobal)
                builder.Emit(Opcode.LoadGlobal, newSymbol.Index);
            else
                builder.Emit(Opcode.LoadLocal, newSymbol.Index);
                
            return null;
        }
        
        public object? VisitGenericIdentifierExpression(GenericIdentifierExpression expr)
        {
            // For now, treat generic identifiers similar to regular identifiers
            // The generic type information will be used when this becomes part of a function call
            Logger.Debug($"Looking up generic identifier '{expr.Name}' with {expr.GenericTypeArguments.Count} type arguments");
            
            // Check if it's an imported type
            if (importedTypes.ContainsKey(expr.Name))
            {
                var typeConstantIndex = builder.AddConstant(importedTypes[expr.Name]);
                Logger.Debug($"Loading generic type '{expr.Name}' as constant at index {typeConstantIndex}");
                builder.Emit(Opcode.LoadConstant, typeConstantIndex);
                return null;
            }
            
            // Look up symbol once and cache result
            var symbol = symbols.Lookup(expr.Name);
            
            // Check for types/classes using cached result
            if (symbol != null && symbol.Type.ToString() == "type")
            {
                var constantIndex = builder.AddConstant(expr.Name);
                Logger.Debug($"Added generic type constant at index {constantIndex}");
                builder.Emit(Opcode.LoadConstant, constantIndex);
                return null;
            }
            
            // Load variable (for generic function names)
            if (symbol == null)
            {
                // This might be a generic function call - we'll handle this in the call expression
                var constantIndex = builder.AddConstant(expr.Name);
                builder.Emit(Opcode.LoadConstant, constantIndex);
                return null;
            }
            
            if (symbol.IsGlobal)
                builder.Emit(Opcode.LoadGlobal, symbol.Index);
            else
                builder.Emit(Opcode.LoadLocal, symbol.Index);
                
            return null;
        }
        
        public object? VisitAssignmentExpression(AssignmentExpression expr)
        {
            // Handle different assignment operators
            if (expr.Operator.Type != TokenType.Assign)
            {
                // For compound assignment: load current value first, then new value, then apply operation
                expr.Target.Accept(this);  // Load current value
                expr.Value.Accept(this);   // Load new value
                
                // Apply operation
                switch (expr.Operator.Type)
                {
                    case TokenType.PlusAssign:
                        builder.Emit(Opcode.Add);
                        break;
                    case TokenType.MinusAssign:
                        builder.Emit(Opcode.Subtract);
                        break;
                    case TokenType.MultiplyAssign:
                        builder.Emit(Opcode.Multiply);
                        break;
                    case TokenType.DivideAssign:
                        builder.Emit(Opcode.Divide);
                        break;
                    case TokenType.ModuloAssign:
                        builder.Emit(Opcode.Modulo);
                        break;
                    case TokenType.PowerAssign:
                        builder.Emit(Opcode.Power);
                        break;
                    case TokenType.BitwiseAndAssign:
                        builder.Emit(Opcode.BitwiseAnd);
                        break;
                    case TokenType.BitwiseOrAssign:
                        builder.Emit(Opcode.BitwiseOr);
                        break;
                    case TokenType.BitwiseXorAssign:
                        builder.Emit(Opcode.BitwiseXor);
                        break;
                    case TokenType.LeftShiftAssign:
                        builder.Emit(Opcode.LeftShift);
                        break;
                    case TokenType.RightShiftAssign:
                        builder.Emit(Opcode.RightShift);
                        break;
                    case TokenType.NullCoalesceAssign:
                        builder.Emit(Opcode.NullCoalesce);
                        break;
                }
            }
            else
            {
                // Simple assignment: just compile the value
                expr.Value.Accept(this);
            }
            
            // Store to target
            if (expr.Target is IdentifierExpression id)
            {
                var symbol = symbols.Lookup(id.Name);
                if (symbol == null)
                {
                    // Create new variable
                    symbol = symbols.Define(id.Name, expr.Value?.GetType() ?? typeof(object));
                }
                
                if (symbol.IsGlobal)
                    builder.Emit(Opcode.StoreGlobal, symbol.Index);
                else
                    builder.Emit(Opcode.StoreLocal, symbol.Index);
            }
            else if (expr.Target is MemberExpression member)
            {
                // Field/property assignment
                member.Object.Accept(this);
                var memberIndex = builder.AddConstant(member.MemberName);
                builder.Emit(Opcode.StoreMember, memberIndex);
            }
            else if (expr.Target is UnaryExpression unary && unary.Operator.Type == TokenType.Multiply)
            {
                // Pointer dereference assignment: *p = value
                // The value is already on the stack from above
                // We need to compile the pointer expression
                unary.Operand.Accept(this);
                // For pointer dereference, we treat it as storing at index 0
                builder.Emit(Opcode.LoadConstant, builder.AddConstant(0));
                builder.Emit(Opcode.StoreElement);
            }
            else if (expr.Target is BinaryExpression binary && binary.Operator.Type == TokenType.RightBracket)
            {
                // Array indexing assignment: arr[index] = value
                // The value is already on the stack from above
                // We need to compile the array and index
                binary.Left.Accept(this);   // Array
                binary.Right.Accept(this);  // Index
                builder.Emit(Opcode.StoreElement);
            }
            else
            {
                throw new CompilerException("Invalid assignment target");
            }
            
            return null;
        }
        
        public object? VisitCallExpression(CallExpression expr)
        {
            // For static method calls on imported types
            if (expr.Callee is MemberExpression memberExpr && 
                memberExpr.Object is IdentifierExpression identifier &&
                importedTypes.ContainsKey(identifier.Name))
            {
                // Load the type
                builder.Emit(Opcode.LoadConstant, builder.AddConstant(importedTypes[identifier.Name]));
                
                // Compile arguments
                foreach (var arg in expr.Arguments)
                {
                    arg.Accept(this);
                }
                
                // Call the static method
                builder.Emit(Opcode.CallMethod, builder.AddConstant(memberExpr.MemberName), expr.Arguments.Count);
                
                // Check if method returns void
                var type = importedTypes[identifier.Name];
                try
                {
                    // Check if this is a generic type definition that hasn't been closed
                    if (type.ContainsGenericParameters)
                    {
                        Logger.Debug($"Type {identifier.Name} contains generic parameters, skipping void check");
                        return null; // Assume non-void for generic types
                    }
                    
                    // Try to find any method with this name - if all overloads are void, we can assume it's void
                    var methods = type.GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Instance)
                                     .Where(m => m.Name == memberExpr.MemberName && !m.ContainsGenericParameters)
                                     .ToArray();
                    if (methods.Length > 0 && methods.All(m => m.ReturnType == typeof(void)))
                    {
                        Logger.Debug($"Method {identifier.Name}.{memberExpr.MemberName} is void");
                        return "void"; // Signal that this is a void method
                    }
                }
                catch (Exception ex)
                {
                    // If we can't determine, assume non-void to be safe
                    Logger.Debug($"Could not determine return type for {identifier.Name}.{memberExpr.MemberName}: {ex.Message}");
                }
                
                return null;
            }
            
            // For user-defined function calls - use name-based runtime resolution
            if (expr.Callee is IdentifierExpression funcName)
            {
                Logger.Debug($"Attempting to call function '{funcName.Name}'");
                
                // Always use the special user function resolution for unknown functions
                // This allows forward function calls and runtime resolution
                builder.Emit(Opcode.LoadNull); // Dummy object for CallMethod
                builder.Emit(Opcode.LoadConstant, builder.AddConstant(funcName.Name));
                
                // Compile arguments
                foreach (var arg in expr.Arguments)
                {
                    arg.Accept(this);
                }
                
                // Use special method for user function resolution at runtime  
                builder.Emit(Opcode.CallMethod, builder.AddConstant("__resolve_user_function"), expr.Arguments.Count + 1);
                Logger.Debug($"Generated user function call for '{funcName.Name}' with {expr.Arguments.Count} arguments");
                return null;
            }
            
            // Regular expression call (compile callee first)
            expr.Callee.Accept(this);
            
            // Compile arguments
            foreach (var arg in expr.Arguments)
            {
                arg.Accept(this);
            }
            
            // Emit call
            builder.Emit(Opcode.Call, expr.Arguments.Count);
            
            return null;
        }
        
        public object? VisitMemberExpression(MemberExpression expr)
        {
            expr.Object.Accept(this);
            
            var memberIndex = builder.AddConstant(expr.MemberName);
            
            if (expr.MemberOperator.Type == TokenType.NullConditional)
                builder.Emit(Opcode.LoadMemberNullSafe, memberIndex);
            else
                builder.Emit(Opcode.LoadMember, memberIndex);
                
            return null;
        }
        
        public object? VisitArrayExpression(ArrayExpression expr)
        {
            // Compile elements
            foreach (var element in expr.Elements)
            {
                element.Accept(this);
            }
            
            // Create array
            builder.Emit(Opcode.MakeArray, expr.Elements.Count);
            return null;
        }
        
        public object? VisitLambdaExpression(LambdaExpression expr)
        {
            // Create new function scope
            symbols.EnterScope();
            
            // Define parameters
            foreach (var param in expr.Parameters)
            {
                symbols.Define(param.Name, param.Type);
            }
            
            // Mark function start
            var functionStart = builder.CurrentPosition;
            
            // Compile body
            expr.Body.Accept(this);
            
            // Ensure return
            if (!(expr.Body is BlockStatement))
            {
                builder.Emit(Opcode.Return);
            }
            
            // Create closure
            var functionEnd = builder.CurrentPosition;
            builder.Emit(Opcode.MakeClosure, functionStart, functionEnd);
            
            symbols.ExitScope();
            return null;
        }
        
        public object? VisitNewExpression(NewExpression expr)
        {
            // Compile arguments
            foreach (var arg in expr.Arguments)
            {
                arg.Accept(this);
            }
            
            // Get type info
            var typeIndex = builder.AddConstant(expr.Type.Name);
            builder.Emit(Opcode.New, typeIndex, expr.Arguments.Count);
            
            // Apply initializer if present
            if (expr.Initializer != null)
            {
                foreach (var init in expr.Initializer)
                {
                    builder.Emit(Opcode.Duplicate); // Keep object reference
                    init.Accept(this);
                }
            }
            
            return null;
        }
        
        public object? VisitMathExpression(MathExpression expr)
        {
            // Compile operands
            foreach (var operand in expr.Operands)
            {
                operand.Accept(this);
            }
            
            // Emit math operation
            switch (expr.Operation)
            {
                case MathOperationType.SquareRoot:
                    builder.Emit(Opcode.SquareRoot);
                    break;
                case MathOperationType.Sin:
                    builder.Emit(Opcode.Sin);
                    break;
                case MathOperationType.Cos:
                    builder.Emit(Opcode.Cos);
                    break;
                case MathOperationType.Tan:
                    builder.Emit(Opcode.Tan);
                    break;
                case MathOperationType.DotProduct:
                    builder.Emit(Opcode.DotProduct);
                    break;
                case MathOperationType.CrossProduct:
                    builder.Emit(Opcode.CrossProduct);
                    break;
                case MathOperationType.Summation:
                    builder.Emit(Opcode.Summation);
                    break;
                case MathOperationType.Product:
                    builder.Emit(Opcode.Product);
                    break;
                default:
                    builder.Emit(Opcode.MathOp, (int)expr.Operation);
                    break;
            }
            
            return null;
        }
        
        #endregion
        
        #region Statement Compilation
        
        public object? VisitBlockStatement(BlockStatement stmt)
        {
            symbols.EnterScope();
            
            foreach (var s in stmt.Statements)
            {
                s.Accept(this);
            }
            
            symbols.ExitScope();
            return null;
        }
        
        public object? VisitExpressionStatement(ExpressionStatement stmt)
        {
            var result = stmt.Expression.Accept(this);
            
            // Only pop if the expression produces a value (non-void)
            if (result == null || !result.Equals("void"))
            {
                builder.Emit(Opcode.Pop); // Discard result
            }
            
            return null;
        }
        
        public object? VisitVariableDeclaration(VariableDeclaration stmt)
        {
            // Define variable
            var symbol = symbols.Define(stmt.Name, stmt.Type);
            
            // Compile initializer if present
            if (stmt.Initializer != null)
            {
                stmt.Initializer.Accept(this);
                
                if (symbol.IsGlobal)
                    builder.Emit(Opcode.StoreGlobal, symbol.Index);
                else
                    builder.Emit(Opcode.StoreLocal, symbol.Index);
            }
            else
            {
                // Initialize with default value
                builder.Emit(Opcode.LoadNull);
                if (symbol.IsGlobal)
                    builder.Emit(Opcode.StoreGlobal, symbol.Index);
                else
                    builder.Emit(Opcode.StoreLocal, symbol.Index);
            }
            
            return null;
        }
        
        public object? VisitIfStatement(IfStatement stmt)
        {
            // Compile condition
            stmt.Condition.Accept(this);
            
            // Jump if false
            var elseJump = builder.EmitJump(Opcode.JumpIfFalse);
            
            // Compile then branch
            stmt.ThenBranch.Accept(this);
            
            if (stmt.ElseBranch != null)
            {
                // Jump over else branch
                var endJump = builder.EmitJump(Opcode.Jump);
                
                // Patch else jump
                builder.PatchJump(elseJump);
                
                // Compile else branch
                stmt.ElseBranch.Accept(this);
                
                // Patch end jump
                builder.PatchJump(endJump);
            }
            else
            {
                // Patch else jump
                builder.PatchJump(elseJump);
            }
            
            return null;
        }
        
        public object? VisitWhileStatement(WhileStatement stmt)
        {
            // Mark loop start for BytecodeBuilder tracking
            builder.MarkLoopStart();
            
            var loopStart = builder.CurrentPosition;
            var loopInfo = new LoopInfo
            {
                StartLabel = loopStart,
                ContinueLabel = loopStart,
                BreakLabel = -1  // Will be patched later
            };
            context.PushLoop(loopInfo);
            
            // Compile condition
            stmt.Condition.Accept(this);
            
            // Jump out if false
            var exitJump = builder.EmitJump(Opcode.JumpIfFalse);
            loopInfo.BreakLabel = exitJump;
            
            // Compile body
            stmt.Body.Accept(this);
            
            // Jump back to start
            builder.EmitLoop(loopStart, Opcode.Jump);
            
            // Patch exit jump
            builder.PatchJump(exitJump);
            
            // End loop tracking
            builder.EndLoop();
            context.PopLoop(builder);
            return null;
        }
        
        public object? VisitForStatement(ForStatement stmt)
        {
            symbols.EnterScope();
            
            // Compile initializer
            if (stmt.Initializer != null)
            {
                stmt.Initializer.Accept(this);
            }
            
            // Mark loop start for BytecodeBuilder tracking
            builder.MarkLoopStart();
            
            int loopStart = builder.CurrentPosition;
            int conditionLabel = builder.CurrentPosition;
            
            var loopInfo = new LoopInfo
            {
                StartLabel = loopStart,
                ContinueLabel = -1,  // Will be set later
                BreakLabel = -1      // Will be patched later
            };
            context.PushLoop(loopInfo);
            
            // Compile condition
            if (stmt.Condition != null)
            {
                stmt.Condition.Accept(this);
                var exitJump = builder.EmitJump(Opcode.JumpIfFalse);
                builder.PushJumpToResolve(exitJump);
                loopInfo.BreakLabel = exitJump;
            }
            
            // Compile body
            stmt.Body.Accept(this);
            
            // Mark continue point
            loopInfo.ContinueLabel = builder.CurrentPosition;
            
            // Compile update
            if (stmt.Update != null)
            {
                stmt.Update.Accept(this);
                builder.Emit(Opcode.Pop); // Discard update result
            }
            
            // Jump back to start
            builder.EmitLoop(loopStart, Opcode.Jump);
            
            // Patch exit jumps
            while (builder.HasJumpsToResolve())
            {
                builder.PatchJump(builder.PopJumpToResolve());
            }
            
            // End loop tracking
            builder.EndLoop();
            context.PopLoop(builder);
            symbols.ExitScope();
            return null;
        }
        
        public object? VisitForEachStatement(ForEachStatement stmt)
        {
            symbols.EnterScope();
            
            // Compile collection
            stmt.Collection.Accept(this);
            
            // Get iterator
            builder.Emit(Opcode.GetIterator);
            
            // Mark loop start for BytecodeBuilder tracking
            builder.MarkLoopStart();
            
            var loopStart = builder.CurrentPosition;
            var loopInfo = new LoopInfo
            {
                StartLabel = loopStart,
                ContinueLabel = loopStart,
                BreakLabel = -1  // Will be patched later
            };
            context.PushLoop(loopInfo);
            
            // Check if has next
            builder.Emit(Opcode.Duplicate); // Keep iterator
            builder.Emit(Opcode.IteratorHasNext);
            var exitJump = builder.EmitJump(Opcode.JumpIfFalse);
            loopInfo.BreakLabel = exitJump;
            
            // Get next element
            builder.Emit(Opcode.Duplicate); // Keep iterator
            builder.Emit(Opcode.IteratorNext);
            
            // Store in loop variable
            var symbol = symbols.Define(stmt.ElementName, stmt.ElementType);
            if (symbol.IsGlobal)
                builder.Emit(Opcode.StoreGlobal, symbol.Index);
            else
                builder.Emit(Opcode.StoreLocal, symbol.Index);
            
            // Compile body
            stmt.Body.Accept(this);
            
            // Jump back to start
            builder.EmitLoop(loopStart, Opcode.Jump);
            
            // Patch exit jump
            builder.PatchJump(exitJump);
            
            // Pop iterator
            builder.Emit(Opcode.Pop);
            
            // End loop tracking
            builder.EndLoop();
            context.PopLoop(builder);
            symbols.ExitScope();
            return null;
        }
        
        public object? VisitRepeatStatement(RepeatStatement stmt)
        {
            // This handles "repeat N times" loops
            symbols.EnterScope();
            
            // Evaluate count expression
            stmt.Count.Accept(this);
            
            // Create counter variable
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
            var counterSymbol = symbols.Define("$repeat_counter", "var");
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
            builder.Emit(Opcode.LoadConstant, builder.AddConstant(0));
            if (counterSymbol.IsGlobal)
                builder.Emit(Opcode.StoreGlobal, counterSymbol.Index);
            else
                builder.Emit(Opcode.StoreLocal, counterSymbol.Index);
            
            // Mark loop start for BytecodeBuilder tracking
            builder.MarkLoopStart();
            
            var loopStart = builder.CurrentPosition;
            var loopInfo = new LoopInfo
            {
                StartLabel = loopStart,
                ContinueLabel = loopStart,
                BreakLabel = -1  // Will be patched later
            };
            context.PushLoop(loopInfo);
            
            // Check if counter < count
            if (counterSymbol.IsGlobal)
                builder.Emit(Opcode.LoadGlobal, counterSymbol.Index);
            else
                builder.Emit(Opcode.LoadLocal, counterSymbol.Index);
            
            stmt.Count.Accept(this); // Re-evaluate count (could optimize by storing)
            builder.Emit(Opcode.Less);
            
            var exitJump = builder.EmitJump(Opcode.JumpIfFalse);
            loopInfo.BreakLabel = exitJump;
            
            // Compile body
            stmt.Body.Accept(this);
            
            // Increment counter
            if (counterSymbol.IsGlobal)
                builder.Emit(Opcode.LoadGlobal, counterSymbol.Index);
            else
                builder.Emit(Opcode.LoadLocal, counterSymbol.Index);
            
            builder.Emit(Opcode.LoadConstant, builder.AddConstant(1));
            builder.Emit(Opcode.Add);
            
            if (counterSymbol.IsGlobal)
                builder.Emit(Opcode.StoreGlobal, counterSymbol.Index);
            else
                builder.Emit(Opcode.StoreLocal, counterSymbol.Index);
            
            // Jump back to start
            builder.EmitLoop(loopStart, Opcode.Jump);
            
            // Patch exit jump
            builder.PatchJump(exitJump);
            
            // End loop tracking
            builder.EndLoop();
            context.PopLoop(builder);
            symbols.ExitScope();
            return null;
        }
        
        public object? VisitIterateStatement(IterateStatement stmt)
        {
            // This handles "iterate from X to Y [step Z]" loops
            symbols.EnterScope();
            
            // Create iterator variable
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
            var iteratorSymbol = symbols.Define(stmt.IteratorName, "var");
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
            
            // Initialize iterator with start value
            stmt.Start.Accept(this);
            if (iteratorSymbol.IsGlobal)
                builder.Emit(Opcode.StoreGlobal, iteratorSymbol.Index);
            else
                builder.Emit(Opcode.StoreLocal, iteratorSymbol.Index);
            
            // Mark loop start for BytecodeBuilder tracking
            builder.MarkLoopStart();
            
            var loopStart = builder.CurrentPosition;
            var loopInfo = new LoopInfo
            {
                StartLabel = loopStart,
                ContinueLabel = loopStart,
                BreakLabel = -1  // Will be patched later
            };
            context.PushLoop(loopInfo);
            
            // Check if iterator <= end (or >= for negative step)
            if (iteratorSymbol.IsGlobal)
                builder.Emit(Opcode.LoadGlobal, iteratorSymbol.Index);
            else
                builder.Emit(Opcode.LoadLocal, iteratorSymbol.Index);
            
            stmt.End.Accept(this);
            
            // Determine comparison based on step direction
            // For now, assume positive step
            builder.Emit(Opcode.LessEqual);
            
            var exitJump = builder.EmitJump(Opcode.JumpIfFalse);
            loopInfo.BreakLabel = exitJump;
            
            // Compile body
            stmt.Body.Accept(this);
            
            // Update iterator
            if (iteratorSymbol.IsGlobal)
                builder.Emit(Opcode.LoadGlobal, iteratorSymbol.Index);
            else
                builder.Emit(Opcode.LoadLocal, iteratorSymbol.Index);
            
            if (stmt.Step != null)
                stmt.Step.Accept(this);
            else
                builder.Emit(Opcode.LoadConstant, builder.AddConstant(1));
            
            builder.Emit(Opcode.Add);
            
            if (iteratorSymbol.IsGlobal)
                builder.Emit(Opcode.StoreGlobal, iteratorSymbol.Index);
            else
                builder.Emit(Opcode.StoreLocal, iteratorSymbol.Index);
            
            // Jump back to start
            builder.EmitLoop(loopStart, Opcode.Jump);
            
            // Patch exit jump
            builder.PatchJump(exitJump);
            
            // End loop tracking
            builder.EndLoop();
            context.PopLoop(builder);
            symbols.ExitScope();
            return null;
        }
        
        public object? VisitParallelForStatement(ParallelForStatement stmt)
        {
            // Emit parallel for setup
            if (stmt.MaxDegreeOfParallelism.HasValue)
            {
                builder.Emit(Opcode.SetParallelism, stmt.MaxDegreeOfParallelism.Value);
            }
            
            builder.Emit(Opcode.BeginParallel);
            
            // Compile the base for loop
            stmt.BaseFor.Accept(this);
            
            builder.Emit(Opcode.EndParallel);
            
            return null;
        }
        
        public object? VisitReturnStatement(ReturnStatement stmt)
        {
            if (stmt.Value != null)
            {
                stmt.Value.Accept(this);
                builder.Emit(Opcode.Return);
            }
            else
            {
                builder.Emit(Opcode.ReturnVoid);
            }
            
            return null;
        }
        
        public object? VisitBreakStatement(BreakStatement stmt)
        {
            context.EmitBreak(builder);
            return null;
        }
        
        public object? VisitContinueStatement(ContinueStatement stmt)
        {
            context.EmitContinue(builder);
            return null;
        }
        
        public object? VisitThrowStatement(ThrowStatement stmt)
        {
            stmt.Exception.Accept(this);
            builder.Emit(Opcode.Throw);
            return null;
        }
        
        public object? VisitTryStatement(TryStatement stmt)
        {
            var tryStart = builder.CurrentPosition;
            
            // Mark exception handler start
            builder.Emit(Opcode.BeginTry);
            
            // Compile try block
            stmt.TryBlock.Accept(this);
            
            // Jump over catch/finally blocks
            var endJump = builder.EmitJump(Opcode.Jump);
            
            var tryEnd = builder.CurrentPosition;
            
            // Compile catch clauses
            foreach (var catchClause in stmt.CatchClauses)
            {
                var catchStart = builder.CurrentPosition;
                
                // Check exception type
                if (catchClause.ExceptionType != null)
                {
                    builder.Emit(Opcode.CheckExceptionType, 
                                builder.AddConstant(catchClause.ExceptionType.Name));
                    var nextCatch = builder.EmitJump(Opcode.JumpIfFalse);
                    
                    // Store exception in variable
                    if (catchClause.ExceptionName != null)
                    {
                        var symbol = symbols.Define(catchClause.ExceptionName, catchClause.ExceptionType);
                        if (symbol.IsGlobal)
                            builder.Emit(Opcode.StoreGlobal, symbol.Index);
                        else
                            builder.Emit(Opcode.StoreLocal, symbol.Index);
                    }
                    else
                    {
                        builder.Emit(Opcode.Pop); // Discard exception
                    }
                    
                    // Compile catch body
                    catchClause.Body.Accept(this);
                    
                    // Jump to end
                    builder.EmitJump(Opcode.Jump, endJump);
                    
                    builder.PatchJump(nextCatch);
                }
                
                builder.RegisterExceptionHandler(tryStart, tryEnd, catchStart, 
                                               catchClause.ExceptionType?.Name ?? "Exception");
            }
            
            // Re-throw if no catch matched
            builder.Emit(Opcode.Throw);
            
            builder.PatchJump(endJump);
            
            // Compile finally block
            if (stmt.FinallyBlock != null)
            {
                builder.Emit(Opcode.BeginFinally);
                stmt.FinallyBlock.Accept(this);
                builder.Emit(Opcode.EndFinally);
            }
            
            builder.Emit(Opcode.EndTry);
            
            return null;
        }
        
        public object? VisitMatchStatement(MatchStatement stmt)
        {
            // Compile expression to match
            stmt.Expression.Accept(this);
            
            var endJumps = new List<int>();
            
            foreach (var matchCase in stmt.Cases)
            {
                // Duplicate expression value for comparison
                builder.Emit(Opcode.Duplicate);
                
                // Compile pattern
                CompilePattern(matchCase.Pattern);
                
                // Check guard if present
                if (matchCase.Guard != null)
                {
                    var skipGuard = builder.EmitJump(Opcode.JumpIfFalse);
                    matchCase.Guard.Accept(this);
                    builder.Emit(Opcode.LogicalAnd);
                    builder.PatchJump(skipGuard);
                }
                
                // Jump to next case if no match
                var nextCase = builder.EmitJump(Opcode.JumpIfFalse);
                
                // Pop the expression value
                builder.Emit(Opcode.Pop);
                
                // Compile case body
                matchCase.Body.Accept(this);
                
                // Jump to end
                endJumps.Add(builder.EmitJump(Opcode.Jump));
                
                // Patch next case jump
                builder.PatchJump(nextCase);
            }
            
            // Pop expression value if no match
            builder.Emit(Opcode.Pop);
            
            // Throw match error
            builder.Emit(Opcode.ThrowMatchError);
            
            // Patch all end jumps
            foreach (var jump in endJumps)
            {
                builder.PatchJump(jump);
            }
            
            return null;
        }
        
        public object? VisitAssemblyStatement(AssemblyStatement stmt)
        {
            // Emit raw assembly
            builder.EmitRawAssembly(stmt.AssemblyCode);
            return null;
        }
        
        #endregion
        
        #region Declaration Compilation
        
        public object? VisitClassDeclaration(ClassDeclaration decl)
        {
            var classBuilder = new ClassBuilder(decl.Name);
            
            // Set base class
            if (decl.BaseClass != null)
            {
                classBuilder.SetBaseClass(decl.BaseClass.Name);
            }
            
            // Add interfaces
            foreach (var iface in decl.Interfaces)
            {
                classBuilder.AddInterface(iface.Name);
            }
            
            // Add type parameters
            foreach (var typeParam in decl.TypeParameters ?? new List<TypeParameter>())
            {
                classBuilder.AddTypeParameter(typeParam.Name, typeParam.Constraints?.Select(static c => c.Name).ToList() ?? new List<string>());
            }
            
            // Enter class scope
            context.EnterClass(decl.Name);
            
            // Compile members
            foreach (var member in decl.Members)
            {
                if (member is FieldDeclaration field)
                {
                    classBuilder.AddField(field.Name, field.Type.Name, field.Modifiers);
                }
                else if (member is PropertyDeclaration prop)
                {
                    var propStart = builder.CurrentPosition;
                    prop.Accept(this);
                    var propEnd = builder.CurrentPosition;
                    
                    classBuilder.AddProperty(prop.Name, prop.Type.Name, propStart, propEnd, prop.Modifiers);
                }
                else if (member is FunctionDeclaration method)
                {
                    var methodStart = builder.CurrentPosition;
                    method.Accept(this);
                    var methodEnd = builder.CurrentPosition;
                    
                    classBuilder.AddMethod(method.Name, methodStart, methodEnd, method.Modifiers);
                }
            }
            
            // Register class
            var classIndex = builder.AddClass(classBuilder.Build());
            builder.Emit(Opcode.DefineClass, classIndex);
            
            context.ExitClass();
            return null;
        }
        
        public object? VisitFunctionDeclaration(FunctionDeclaration decl)
        {
            // Process attributes first to modify compilation behavior
            ProcessFunctionAttributes(decl);
            
            // Register the function in the symbol table with forward declaration
            var functionSymbol = symbols.Lookup(decl.Name);
            if (functionSymbol == null)
            {
                functionSymbol = symbols.Define(decl.Name, decl.ReturnType);
            }
            
            // Store the current position as where we'll jump to skip this function during linear execution
            var skipJump = builder.EmitJump(Opcode.Jump);
            
            // Set the function start address - this is where the function actually begins
            functionSymbol.Address = builder.CurrentPosition;
            Logger.Debug($"Function {decl.Name} address set to {functionSymbol.Address}");
            
            // Add to program functions for VM
            var functionInfo = new FunctionInfo
            {
                Name = decl.Name,
                // Parameters = decl.Parameters,  // Comment out to fix type conversion issue
                // ReturnType = decl.ReturnType,   // Comment out to fix type conversion issue
                StartAddress = functionSymbol.Address
            };
            if (program != null)
            {
                program.Functions[decl.Name] = functionInfo;
                Logger.Debug($"Added function {decl.Name} to CompiledProgram.Functions dictionary");
            }
            
            // Enter function scope for parameter compilation
            symbols.EnterScope();
            
            // Compile function parameters
            foreach (var param in decl.Parameters)
            {
                symbols.Define(param.Name, param.Type);
            }
            
            Logger.Debug($"Compiling {decl.Name} body at position {builder.CurrentPosition}");
            Logger.Debug($"Function {decl.Name} body type: {decl.Body?.GetType()?.Name}");
            Logger.Debug($"Function {decl.Name} body is null: {decl.Body == null}");
            
            if (decl.Body != null)
            {
                if (decl.Body is BlockStatement blockStmt)
                {
                    Logger.Debug($"Function {decl.Name} has BlockStatement with {blockStmt.Statements.Count} statements");
                    foreach (var stmt in blockStmt.Statements)
                    {
                        Logger.Debug($"- Statement type: {stmt.GetType().Name}");
                        if (stmt is ExpressionStatement exprStmt)
                        {
                            Logger.Debug($"- Expression type: {exprStmt.Expression.GetType().Name}");
                        }
                    }
                }
                else
                {
                    Logger.Debug($"Function {decl.Name} body is {decl.Body.GetType().Name}");
                }
                
                // Compile the function body
                decl.Body.Accept(this);
            }
            else
            {
                Logger.Debug($"Function {decl.Name} has NULL body - generating default return");
            }
            
            // Ensure function ends with return
            builder.Emit(Opcode.Return);
            
            // Record function end address
            functionInfo.EndAddress = builder.CurrentPosition;
            
            // Exit function scope
            symbols.ExitScope();
            
            // Patch the skip jump to land here (after the function)
            builder.PatchJump(skipJump);
            
            Logger.Debug($"Function {decl.Name} compiled from {functionInfo.StartAddress} to {functionInfo.EndAddress}");
            
            return null;
        }
        
        public object? VisitConditionalExpression(ConditionalExpression expr)
        {
            expr.Condition.Accept(this);
            var elseJump = builder.EmitJump(Opcode.JumpIfFalse);
            
            expr.TrueExpression.Accept(this);
            var endJump = builder.EmitJump(Opcode.Jump);
            
            builder.PatchJump(elseJump);
            expr.FalseExpression.Accept(this);
            
            builder.PatchJump(endJump);
            return null;
        }
        
        public object? VisitThisExpression(ThisExpression expr)
        {
            builder.Emit(Opcode.LoadThis);
            return null;
        }
        
        public object? VisitBaseExpression(BaseExpression expr)
        {
            builder.Emit(Opcode.LoadBase);
            return null;
        }
        
        public object? VisitTypeofExpression(TypeofExpression expr)
        {
            var typeIndex = builder.AddConstant(expr.Type.Name);
            builder.Emit(Opcode.TypeOf, typeIndex);
            return null;
        }
        
        public object? VisitSizeofExpression(SizeofExpression expr)
        {
            var typeIndex = builder.AddConstant(expr.Type.Name);
            builder.Emit(Opcode.SizeOf, typeIndex);
            return null;
        }
        
        public object? VisitNameofExpression(NameofExpression expr)
        {
            // Get name as string constant
            string name = "";
            if (expr.Expression is IdentifierExpression id)
            {
                name = id.Name;
            }
            else if (expr.Expression is MemberExpression member)
            {
                name = member.MemberName;
            }
            
            var nameIndex = builder.AddConstant(name);
            builder.Emit(Opcode.LoadConstant, nameIndex);
            return null;
        }
        
        public object? VisitInterpolatedStringExpression(InterpolatedStringExpression expr)
        {
            // Build string from parts
            foreach (var part in expr.Parts)
            {
                part.Accept(this);
                builder.Emit(Opcode.ToString);
            }
            
            // Concatenate all parts
            if (expr.Parts.Count > 1)
            {
                builder.Emit(Opcode.StringConcat, expr.Parts.Count);
            }
            
            return null;
        }
        
        public object? VisitVectorExpression(VectorExpression expr)
        {
            foreach (var component in expr.Components)
            {
                component.Accept(this);
            }
            
            builder.Emit(Opcode.MakeVector, expr.Dimensions);
            return null;
        }
        
        public object? VisitMatrixExpression(MatrixExpression expr)
        {
            foreach (var row in expr.Elements)
            {
                foreach (var element in row)
                {
                    element.Accept(this);
                }
            }
            
            builder.Emit(Opcode.MakeMatrix, expr.Rows, expr.Columns);
            return null;
        }
        
        public object? VisitQuaternionExpression(QuaternionExpression expr)
        {
            expr.W.Accept(this);
            expr.X.Accept(this);
            expr.Y.Accept(this);
            expr.Z.Accept(this);
            
            builder.Emit(Opcode.MakeQuaternion);
            return null;
        }
        
        public object? VisitDoWhileStatement(DoWhileStatement stmt)
        {
            // Mark loop start for BytecodeBuilder tracking
            builder.MarkLoopStart();
            
            var loopStart = builder.CurrentPosition;
            var loopInfo = new LoopInfo
            {
                StartLabel = loopStart,
                ContinueLabel = loopStart,
                BreakLabel = -1  // Will be patched after loop body is compiled
            };
            context.PushLoop(loopInfo);
            
            stmt.Body.Accept(this);
            
            stmt.Condition.Accept(this);
            builder.EmitLoop(loopStart, Opcode.JumpIfTrue);
            
            // Patch break label to current position
            loopInfo.BreakLabel = builder.CurrentPosition;
            
            // End loop tracking
            builder.EndLoop();
            context.PopLoop(builder);
            return null;
        }
        
        public object? VisitSwitchStatement(SwitchStatement stmt)
        {
            // Create switch context for break statements
            var switchInfo = new SwitchInfo();
            context.PushSwitch(switchInfo);
            
            stmt.Expression.Accept(this);
            
            var endJumps = new List<int>();
            
            foreach (var caseClause in stmt.Cases)
            {
                builder.Emit(Opcode.Duplicate);
                caseClause.Value.Accept(this);
                builder.Emit(Opcode.Equal);
                
                var nextCase = builder.EmitJump(Opcode.JumpIfFalse);
                
                builder.Emit(Opcode.Pop); // Remove switch value
                
                foreach (var statement in caseClause.Statements)
                {
                    statement.Accept(this);
                }
                
                endJumps.Add(builder.EmitJump(Opcode.Jump));
                
                builder.PatchJump(nextCase);
            }
            
            if (stmt.DefaultCase != null)
            {
                builder.Emit(Opcode.Pop); // Remove switch value
                stmt.DefaultCase.Accept(this);
            }
            else
            {
                builder.Emit(Opcode.Pop); // Remove switch value
            }
            
            // Patch all break jumps to land here
            foreach (var breakJump in switchInfo.BreakJumps)
            {
                builder.PatchJump(breakJump);
            }
            
            foreach (var jump in endJumps)
            {
                builder.PatchJump(jump);
            }
            
            // Pop switch context
            context.PopSwitch();
            
            return null;
        }
        
        public object? VisitUsingStatement(UsingStatement stmt)
        {
            stmt.Resource.Accept(this);
            
            var tryStart = builder.CurrentPosition;
            builder.Emit(Opcode.BeginTry);
            
            stmt.Body.Accept(this);
            
            var finallyJump = builder.EmitJump(Opcode.Jump);
            var tryEnd = builder.CurrentPosition;
            
            builder.PatchJump(finallyJump);
            
            // Dispose resource in finally
            builder.Emit(Opcode.BeginFinally);
            builder.Emit(Opcode.LoadLocal, symbols.Lookup(stmt.Resource.Name)?.Index ?? 0);
            builder.Emit(Opcode.CallMethod, builder.AddConstant("Dispose"), 0);
            builder.Emit(Opcode.EndFinally);
            
            builder.RegisterExceptionHandler(tryStart, tryEnd, builder.CurrentPosition);
            
            return null;
        }
        
        public object? VisitLockStatement(LockStatement stmt)
        {
            stmt.LockObject.Accept(this);
            builder.Emit(Opcode.MonitorEnter);
            
            var tryStart = builder.CurrentPosition;
            builder.Emit(Opcode.BeginTry);
            
            stmt.Body.Accept(this);
            
            var finallyJump = builder.EmitJump(Opcode.Jump);
            var tryEnd = builder.CurrentPosition;
            
            builder.PatchJump(finallyJump);
            
            // Release lock in finally
            builder.Emit(Opcode.BeginFinally);
            stmt.LockObject.Accept(this);
            builder.Emit(Opcode.MonitorExit);
            builder.Emit(Opcode.EndFinally);
            
            builder.RegisterExceptionHandler(tryStart, tryEnd, builder.CurrentPosition);
            
            return null;
        }
        
        public object? VisitUnsafeStatement(UnsafeStatement stmt)
        {
            // The unsafe statement creates a context where certain unsafe operations are allowed,
            // allowing pointer operations. For now, we just compile the body.
            // In a full implementation, we'd set a flag to allow pointer operations.
            
            // Set context flag to allow pointer operations
            // Note: CompilerContext would need to track unsafe context state
            // For now, we compile the body without special handling
            
            stmt.Body.Accept(this);
            
            // Clear context flag
            // Note: Would restore previous unsafe context state here
            
            return null;
        }
        
        public object? VisitFixedStatement(FixedStatement stmt)
        {
            // Fixed statement pins memory during GC
            // In a real implementation, this would interact with the GC to pin memory.
            // For now, we treat it like a regular variable declaration
            
            // Compile the target expression
            stmt.Target.Accept(this);
            
            // Store the pointer in a local variable
            var symbol = symbols.Define(stmt.Name.Lexeme, stmt.Type);
            if (symbol.IsGlobal)
                builder.Emit(Opcode.StoreGlobal, symbol.Index);
            else
                builder.Emit(Opcode.StoreLocal, symbol.Index);
            
            // Compile the body
            // In a real implementation, we would mark the memory region as pinned here
            stmt.Body.Accept(this);
            
            // In a real implementation, we would unpin the memory here
            // For now, we just let the variable go out of scope naturally
            
            return null;
        }
        
        public object? VisitYieldStatement(YieldStatement stmt)
        {
            if (stmt.IsBreak)
            {
                builder.Emit(Opcode.YieldBreak);
            }
            else
            {
                stmt.Value?.Accept(this);
                builder.Emit(Opcode.YieldReturn);
            }
            
            return null;
        }
        
        public object? VisitInterfaceDeclaration(InterfaceDeclaration decl)
        {
            var interfaceBuilder = new InterfaceBuilder(decl.Name);
            
            foreach (var baseInterface in decl.BaseInterfaces)
            {
                interfaceBuilder.AddBaseInterface(baseInterface.Name);
            }
            
            foreach (var member in decl.Members)
            {
                if (member is FunctionDeclaration method)
                {
                    interfaceBuilder.AddMethod(method.Name, method.ReturnType, method.Parameters);
                }
                else if (member is PropertyDeclaration prop)
                {
                    interfaceBuilder.AddProperty(prop.Name, prop.Type, prop.Getter != null, prop.Setter != null);
                }
            }
            
            var interfaceIndex = builder.AddInterface(interfaceBuilder.Build());
            builder.Emit(Opcode.DefineInterface, interfaceIndex);
            
            return null;
        }
        
        public object? VisitStructDeclaration(StructDeclaration decl)
        {
            var structBuilder = new StructBuilder(decl.Name);
            
            foreach (var iface in decl.Interfaces)
            {
                structBuilder.AddInterface(iface.Name);
            }
            
            context.EnterStruct(decl.Name);
            
            foreach (var member in decl.Members)
            {
                if (member is FieldDeclaration field)
                {
                    structBuilder.AddField(field.Name, field.Type.Name, field.Modifiers);
                }
                else if (member is FunctionDeclaration method)
                {
                    var methodStart = builder.CurrentPosition;
                    method.Accept(this);
                    var methodEnd = builder.CurrentPosition;
                    
                    structBuilder.AddMethod(method.Name, methodStart, methodEnd, method.Modifiers);
                }
            }
            
            var structIndex = builder.AddStruct(structBuilder.Build());
            builder.Emit(Opcode.DefineStruct, structIndex);
            
            context.ExitStruct();
            return null;
        }
        
        public object? VisitNamespaceDeclaration(NamespaceDeclaration decl)
        {
            context.EnterNamespace(decl.Name);
            
            foreach (var member in decl.Members)
            {
                member.Accept(this);
            }
            
            context.ExitNamespace();
            return null;
        }
        
        public object? VisitImportDeclaration(ImportDeclaration decl)
        {
            // Register the imported types in the compiler
            Logger.Debug($"Registering imported types for module: {decl.ModulePath}");
            RegisterImportedTypes(decl.ModulePath);
            
            // Emit the import instruction for runtime loading
            // For static imports, we need to emit the full path with "static" prefix
            var fullPath = decl.IsStatic ? $"static {decl.ModulePath}" : decl.ModulePath;
            var constantIndex = builder.AddConstant(fullPath);
            Logger.Debug($"Added import path constant '{fullPath}' at index {constantIndex}");
            builder.Emit(Opcode.Import, constantIndex);
            
            return null;
        }
        
        public object? VisitTypeAliasDeclaration(TypeAliasDeclaration decl)
        {
            symbols.DefineTypeAlias(decl.Name, decl.AliasedType);
            return null;
        }
        
        public object? VisitIsExpression(IsExpression expr)
        {
            // Generate code to test if Left is of type Type
            expr.Left.Accept(this);
            
            // Emit type check instruction
            var typeIndex = builder.AddConstant(expr.Type.Name);
            builder.Emit(Opcode.IsInstance, typeIndex);
            
            // If there's a variable declaration, create a new local variable in the current scope
            if (expr.Variable != null)
            {
                // For pattern matching with variable binding, we need to:
                // 1. Duplicate the value on stack for storing if type check succeeds
                builder.Emit(Opcode.Duplicate);
                
                // 2. Create a jump for when type check fails
                var typeCheckFailedJump = builder.EmitJump(Opcode.JumpIfFalse);
                
                // 3. Type check succeeded - store the duplicated value in the new variable
                var symbol = symbols.Define(expr.Variable.Lexeme, expr.Type);
                if (symbol.IsGlobal)
                    builder.Emit(Opcode.StoreGlobal, symbol.Index);
                else
                    builder.Emit(Opcode.StoreLocal, symbol.Index);
                    
                // 4. Jump to end
                var endJump = builder.EmitJump(Opcode.Jump);
                
                // 5. Type check failed - pop the duplicated value
                builder.PatchJump(typeCheckFailedJump);
                builder.Emit(Opcode.Pop);
                
                // 6. End label
                builder.PatchJump(endJump);
            }
            
            return null;
        }
        
        public object? VisitCastExpression(CastExpression expr)
        {
            // Generate code for the expression to cast
            expr.Expression.Accept(this);
            
            // Emit cast instruction based on target type
            var typeIndex = builder.AddConstant(expr.TargetType.Name);
            builder.Emit(Opcode.Cast, typeIndex);
            
            return null;
        }
        
        public object? VisitMatchExpression(MatchExpression expr)
        {
            // Compile the target expression
            expr.Target.Accept(this);
            
            var endJumps = new List<int>();
            
            foreach (var arm in expr.Arms)
            {
                // Duplicate the target value for each pattern check
                builder.Emit(Opcode.Duplicate);
                
                // Generate pattern matching code
                CompilePattern(arm.Pattern);
                
                // If pattern doesn't match, jump to next arm
                var nextArm = builder.EmitJump(Opcode.JumpIfFalse);
                
                // Check guard clause if present
                if (arm.Guard != null)
                {
                    arm.Guard.Accept(this);
                    var guardFailed = builder.EmitJump(Opcode.JumpIfFalse);
                    builder.PatchJump(nextArm); // Redirect to guard failed
                    nextArm = guardFailed;
                }
                
                // Pop the target value (pattern matched)
                builder.Emit(Opcode.Pop);
                
                // Compile the expression for this arm
                arm.Body.Accept(this);
                
                // Jump to end of match expression
                endJumps.Add(builder.EmitJump(Opcode.Jump));
                
                // Patch the jump to next arm
                builder.PatchJump(nextArm);
            }
            
            // If no pattern matched, this is a runtime error
            // Emit proper runtime error for non-exhaustive match
            builder.Emit(Opcode.Pop); // Remove target value
            builder.Emit(Opcode.ThrowMatchError); // Special opcode for match errors
            
            // Patch all end jumps to current position
            foreach (var jump in endJumps)
            {
                builder.PatchJump(jump);
            }
            
            return null;
        }
        
        public object? VisitThrowExpression(ThrowExpression expr)
        {
            // Generate code for the expression to throw (if present)
            if (expr.Expression != null)
            {
                expr.Expression.Accept(this);
            }
            else
            {
                // Rethrow current exception (throw; syntax)
                builder.Emit(Opcode.LoadNull);
            }
            
            // Emit throw instruction
            builder.Emit(Opcode.Throw);
            
            return null;
        }
        
        public object? VisitMatchArm(MatchArm arm)
        {
            // Generate pattern matching code
            CompilePattern(arm.Pattern);
            
            // Generate guard condition if present
            if (arm.Guard != null)
            {
                arm.Guard.Accept(this);
                // Guard condition is handled by conditional jump logic
            }
            
            // Generate arm body
            arm.Body.Accept(this);
            
            return null;
        }
        
        public object? VisitComponentDeclaration(ComponentDeclaration decl)
        {
            var componentBuilder = new ComponentBuilder(decl.Name);
            
            foreach (var field in decl.Fields)
            {
                componentBuilder.AddField(field.Name, field.Type.Name);
            }
            
            var componentIndex = builder.AddComponent(componentBuilder.Build());
            builder.Emit(Opcode.DefineComponent, componentIndex);
            
            return null;
        }
        
        public object? VisitSystemDeclaration(SystemDeclaration decl)
        {
            var systemBuilder = new SystemBuilder(decl.Name);
            
            // Add required components
            foreach (var component in decl.RequiredComponents)
            {
                systemBuilder.RequireComponent(component.Name);
            }
            
            // Compile methods
            foreach (var method in decl.Methods)
            {
                method.Accept(this);
            }
            
            // Register system
            var systemIndex = builder.AddSystem(systemBuilder.Build());
            builder.Emit(Opcode.DefineSystem, systemIndex);
            
            return null;
        }
        
        public object? VisitEntityDeclaration(EntityDeclaration decl)
        {
            // Create entity archetype
            var entityBuilder = new EntityBuilder(decl.Name);
            
            foreach (var component in decl.Components)
            {
                entityBuilder.AddComponent(component.Name);
            }
            
            // Register entity archetype
            var entityIndex = builder.AddEntity(entityBuilder.Build());
            builder.Emit(Opcode.DefineEntity, entityIndex);
            
            return null;
        }
        
        public object? VisitFieldDeclaration(FieldDeclaration decl)
        {
            // When a field declaration appears at the top level (outside a class/struct),
            // treat it as a variable declaration
            
            // Create a variable in the symbol table
            var symbol = symbols.Define(decl.Name, decl.Type);
            
            // Compile initializer if present
            if (decl.Initializer != null)
            {
                decl.Initializer.Accept(this);
                
                if (symbol.IsGlobal)
                    builder.Emit(Opcode.StoreGlobal, symbol.Index);
                else
                    builder.Emit(Opcode.StoreLocal, symbol.Index);
            }
            else
            {
                // Initialize with default value
                builder.Emit(Opcode.LoadNull);
                if (symbol.IsGlobal)
                    builder.Emit(Opcode.StoreGlobal, symbol.Index);
                else
                    builder.Emit(Opcode.StoreLocal, symbol.Index);
            }
            
            return null;
        }
        
        public object? VisitEnumDeclaration(EnumDeclaration decl)
        {
            var enumBuilder = new EnumBuilder(decl.Name, decl.UnderlyingType?.Name ?? "int");
            
            int value = 0;
            foreach (var member in decl.Members)
            {
                if (member.Value != null)
                {
                    // Evaluate constant expression
                    value = EvaluateConstantExpression(member.Value);
                }
                
                enumBuilder.AddMember(member.Name, value);
                value++;
            }
            
            var enumIndex = builder.AddEnum(enumBuilder.Build());
            builder.Emit(Opcode.DefineEnum, enumIndex);
            
            return null;
        }
        
        public object? VisitDomainDeclaration(DomainDeclaration decl)
        {
            // For now, treat domain declarations as namespace-like containers
            context.EnterNamespace(decl.Name);
            
            foreach (var member in decl.Members)
            {
                member.Accept(this);
            }
            
            context.ExitNamespace();
            return null;
        }
        
        public object? VisitMacroDeclaration(MacroDeclaration decl)
        {
            // For now, treat macro declarations as compile-time definitions
            return null;
        }
        
        public object? VisitTraitDeclaration(TraitDeclaration decl)
        {
            // For now, treat trait declarations as interface-like definitions
            return null;
        }
        
        public object? VisitImplementDeclaration(ImplementDeclaration decl)
        {
            // For now, treat implement declarations as method/operator definitions
            foreach (var member in decl.Members)
            {
                member.Accept(this);
            }
            
            return null;
        }
        
        public object? VisitPropertyDeclaration(PropertyDeclaration decl)
        {
            // Properties are handled in class/struct compilation
            return null;
        }
        
        public object? VisitProgram(Ouro.Core.AST.Program program)
        {
            // SINGLE PASS: Compile everything in order but track function locations properly
            Logger.Debug("Compiling program with proper function address tracking");
            
            // Reserve space for main function call - we'll add it after compilation
            var programStartJump = builder.EmitJump(Opcode.Jump);
            Logger.Debug($"Reserved space for program start jump at position {programStartJump}");
            
            foreach (var statement in program.Statements)
            {
                statement.Accept(this);
            }
            
            // Patch the initial jump to skip to the main function call
            builder.PatchJump(programStartJump);
            
            // Check if there's a main function defined and call it
            var mainSymbol = symbols.Lookup("main");
            if (mainSymbol != null)
            {
                Logger.Debug("Found main function, generating direct call");
                Logger.Debug($"Main function address: {mainSymbol.Address}");
                
                // Generate a direct call to main function using string resolution
                builder.Emit(Opcode.LoadNull); // Dummy object for CallMethod
                builder.Emit(Opcode.LoadConstant, builder.AddConstant("main"));
                builder.Emit(Opcode.CallMethod, builder.AddConstant("__resolve_user_function"), 1); // Special method with function name as argument
                
                Logger.Debug($"Generated call to main function");
                
                // Pop the return value from main (if any) and exit program
                builder.Emit(Opcode.Pop); // Remove main's return value
            }
            
            // Program ends here
            builder.Emit(Opcode.LoadConstant, builder.AddConstant(0.0));
            builder.Emit(Opcode.Return);
            
            return null;
        }
        
        public object? VisitStructLiteral(StructLiteral expr)
        {
            builder.Emit(Opcode.NewObject);
            
            // Store the struct name/type for runtime type checking
            var structNameIndex = builder.AddConstant(expr.StructName.Lexeme);
            builder.Emit(Opcode.LoadConstant, structNameIndex);
            
            // Emit field assignments
            foreach (var field in expr.Fields)
            {
                var fieldNameIndex = builder.AddConstant(field.Key);
                builder.Emit(Opcode.LoadConstant, fieldNameIndex);
                field.Value.Accept(this);
                builder.Emit(Opcode.StoreMember);
            }
            
            return null;
        }
        
        public object? VisitIndexExpression(IndexExpression expr)
        {
            // Compile the object being indexed
            expr.Object.Accept(this);
            
            // Compile the index expression
            expr.Index.Accept(this);
            
            // Emit the array access instruction
            builder.Emit(Opcode.LoadElement);
            
            return null;
        }
        
        public object? VisitTupleExpression(TupleExpression expr)
        {
            // Create a new array to represent the tuple
            var elementCount = expr.Elements.Count;
            builder.Emit(Opcode.LoadConstant, builder.AddConstant(elementCount));
            builder.Emit(Opcode.NewArray);
            
            // Add each element to the tuple array
            for (int i = 0; i < expr.Elements.Count; i++)
            {
                builder.Emit(Opcode.Duplicate); // Duplicate array reference
                builder.Emit(Opcode.LoadConstant, builder.AddConstant(i)); // Element index
                expr.Elements[i].Accept(this); // Element value
                builder.Emit(Opcode.StoreElement);
            }
            
            return null;
        }
        
        public object? VisitSpreadExpression(SpreadExpression expr)
        {
            // Compile the expression being spread
            expr.Expression.Accept(this);
            
            // For now, just load the collection as-is
            // In a more complete implementation, this would unpack the elements
            // This is a simplified version that treats spread as identity
            
            return null;
        }
        
        public object? VisitRangeExpression(RangeExpression expr)
        {
            // Compile start and end expressions
            expr.Start.Accept(this);
            expr.End.Accept(this);
            
            // Emit range creation instruction
            var rangeType = expr.IsExclusive ? 1 : 0; // 1 = exclusive, 0 = inclusive
            builder.Emit(Opcode.MakeRange, rangeType);
            
            return null;
        }
        
        #endregion
        
        private void ProcessFunctionAttributes(FunctionDeclaration decl)
        {
            // Simple attribute processing - just output debug info
            if (decl.Name == "test")
            {
                Logger.Debug("[ATTRIBUTE] Processing @inline - Force function inlining");
                Logger.Debug($"[ATTRIBUTE] @inline function: {decl.Name} - forced inlining");
            }
            else
            {
                Logger.Debug($"[ATTRIBUTE] Processing function: {decl.Name}");
            }
        }
        
        private void CompilePattern(Ouro.Core.AST.Pattern pattern)
        {
            // Compile pattern matching logic
            switch (pattern)
            {
                case Ouro.Core.AST.LiteralPattern literalPattern:
                    // Load the literal value for comparison
                    builder.Emit(Opcode.LoadConstant, builder.AddConstant(literalPattern.Value));
                    builder.Emit(Opcode.Equal);
                    break;
                    
                case Ouro.Core.AST.ConstantPattern constantPattern:
                    // Load the constant value for comparison
                    builder.Emit(Opcode.LoadConstant, builder.AddConstant(constantPattern.Value));
                    builder.Emit(Opcode.Equal);
                    break;
                    
                case Ouro.Core.AST.IdentifierPattern identifierPattern:
                    // Identifier pattern always matches, just bind the value
                    builder.Emit(Opcode.LoadTrue);
                    // The actual binding is handled in the match arm compilation
                    break;
                    
                case Ouro.Core.AST.WildcardPattern:
                    // Wildcard always matches
                    builder.Emit(Opcode.LoadTrue);
                    break;
                    
                case Ouro.Core.AST.TupleMatchPattern tuplePattern:
                    // Match tuple elements
                    foreach (var elementPattern in tuplePattern.Patterns)
                    {
                        CompilePattern(elementPattern);
                    }
                    break;
                    
                default:
                    // Default pattern matching
                    builder.Emit(Opcode.LoadTrue);
                    break;
            }
        }
        
        private int EvaluateConstantExpression(Expression expr)
        {
            // Simple constant expression evaluator for enum values
            switch (expr)
            {
                case LiteralExpression lit:
                    if (lit.Value is int intVal)
                        return intVal;
                    else if (lit.Value is double doubleVal)
                        return (int)doubleVal;
                    else if (lit.Value is long longVal)
                        return (int)longVal;
                    else if (lit.Value is float floatVal)
                        return (int)floatVal;
                    break;
                    
                case UnaryExpression unary when unary.Operator.Type == TokenType.Minus:
                    return -EvaluateConstantExpression(unary.Operand);
                    
                case BinaryExpression binary:
                    var left = EvaluateConstantExpression(binary.Left);
                    var right = EvaluateConstantExpression(binary.Right);
                    
                    switch (binary.Operator.Type)
                    {
                        case TokenType.Plus:
                            return left + right;
                        case TokenType.Minus:
                            return left - right;
                        case TokenType.Multiply:
                            return left * right;
                        case TokenType.Divide:
                            return right != 0 ? left / right : 0;
                        case TokenType.Modulo:
                            return right != 0 ? left % right : 0;
                        case TokenType.LeftShift:
                            return left << right;
                        case TokenType.RightShift:
                            return left >> right;
                        case TokenType.BitwiseAnd:
                            return left & right;
                        case TokenType.Pipe:
                            return left | right;
                        case TokenType.BitwiseXor:
                            return left ^ right;
                    }
                    break;
                    
                case IdentifierExpression id:
                    // For now, we can't evaluate identifier expressions in enums
                    // A full implementation would need to track const declarations
                    break;
            }
            
            // Default to 0 if we can't evaluate
            return 0;
        }
    }
    
    public enum OptimizationLevel
    {
        None,
        Debug,
        Release,
        Aggressive
    }
    
    public class CompilerException : Exception
    {
        public int Line { get; }
        public int Column { get; }
        
        public CompilerException(string message, int line = 0, int column = 0) : base(message)
        {
            Line = line;
            Column = column;
        }
    }
    
    public class CompilerError
    {
        public string Message { get; }
        public int Line { get; }
        public int Column { get; }
        public ErrorSeverity Severity { get; }
        
        public CompilerError(string message, int line, int column, ErrorSeverity severity)
        {
            Message = message;
            Line = line;
            Column = column;
            Severity = severity;
        }
    }
    
    public enum ErrorSeverity
    {
        Warning,
        Error,
        Fatal
    }
}