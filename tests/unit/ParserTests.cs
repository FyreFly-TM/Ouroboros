using System;
using System.Collections.Generic;
using System.Linq;
using Ouro.Core.Parser;
using Ouro.Core.Lexer;
using Ouro.Core.AST;
using Ouro.Testing;
using Ouro.Tokens;

namespace Ouro.Tests.Unit
{
    [TestClass]
    public class ParserTests
    {
        private Parser parser;
        
        [Test("Parse variable declaration")]
        public void TestVariableDeclaration()
        {
            var source = "int x = 42;";
            var ast = ParseSource(source);
            
            Assert.AreEqual(1, ast.Statements.Count);
            var varDecl = ast.Statements[0] as VariableDeclaration;
            Assert.IsNotNull(varDecl);
            Assert.AreEqual("x", varDecl.Name);
            Assert.AreEqual("int", varDecl.Type.Name);
            Assert.IsNotNull(varDecl.Initializer);
        }
        
        [Test("Parse function declaration")]
        public void TestFunctionDeclaration()
        {
            var source = @"
                function Add(int a, int b) -> int {
                    return a + b;
                }
            ";
            var ast = ParseSource(source);
            
            Assert.AreEqual(1, ast.Statements.Count);
            var funcDecl = ast.Statements[0] as FunctionDeclaration;
            Assert.IsNotNull(funcDecl);
            Assert.AreEqual("Add", funcDecl.Name);
            Assert.AreEqual(2, funcDecl.Parameters.Count);
            Assert.AreEqual("int", funcDecl.ReturnType.Name);
        }
        
        [Test("Parse class declaration")]
        public void TestClassDeclaration()
        {
            var source = @"
                class Point {
                    private int x;
                    private int y;
                    
                    public function GetX() -> int {
                        return x;
                    }
                }
            ";
            var ast = ParseSource(source);
            
            Assert.AreEqual(1, ast.Statements.Count);
            var classDecl = ast.Statements[0] as ClassDeclaration;
            Assert.IsNotNull(classDecl);
            Assert.AreEqual("Point", classDecl.Name);
            Assert.AreEqual(3, classDecl.Members.Count);
        }
        
        [Test("Parse if statement")]
        public void TestIfStatement()
        {
            var source = @"
                if (x > 10) {
                    y = 20;
                } else {
                    y = 30;
                }
            ";
            var ast = ParseSource(source);
            
            Assert.AreEqual(1, ast.Statements.Count);
            var ifStmt = ast.Statements[0] as IfStatement;
            Assert.IsNotNull(ifStmt);
            Assert.IsNotNull(ifStmt.Condition);
            Assert.IsNotNull(ifStmt.ThenBranch);
            Assert.IsNotNull(ifStmt.ElseBranch);
        }
        
        [Test("Parse while loop")]
        public void TestWhileLoop()
        {
            var source = @"
                while (i < 10) {
                    i = i + 1;
                }
            ";
            var ast = ParseSource(source);
            
            Assert.AreEqual(1, ast.Statements.Count);
            var whileStmt = ast.Statements[0] as WhileStatement;
            Assert.IsNotNull(whileStmt);
            Assert.IsNotNull(whileStmt.Condition);
            Assert.IsNotNull(whileStmt.Body);
        }
        
        [Test("Parse for loop")]
        public void TestForLoop()
        {
            var source = @"
                for (int i = 0; i < 10; i++) {
                    sum += i;
                }
            ";
            var ast = ParseSource(source);
            
            Assert.AreEqual(1, ast.Statements.Count);
            var forStmt = ast.Statements[0] as ForStatement;
            Assert.IsNotNull(forStmt);
            Assert.IsNotNull(forStmt.Initializer);
            Assert.IsNotNull(forStmt.Condition);
            Assert.IsNotNull(forStmt.Update);
            Assert.IsNotNull(forStmt.Body);
        }
        
        [Test("Parse array declaration")]
        public void TestArrayDeclaration()
        {
            var source = "int[] numbers = [1, 2, 3, 4, 5];";
            var ast = ParseSource(source);
            
            Assert.AreEqual(1, ast.Statements.Count);
            var varDecl = ast.Statements[0] as VariableDeclaration;
            Assert.IsNotNull(varDecl);
            Assert.IsTrue(varDecl.Type.IsArray);
            
            var arrayExpr = varDecl.Initializer as ArrayExpression;
            Assert.IsNotNull(arrayExpr);
            Assert.AreEqual(5, arrayExpr.Elements.Count);
        }
        
        [Test("Parse lambda expression")]
        public void TestLambdaExpression()
        {
            var source = "var add = (a, b) => a + b;";
            var ast = ParseSource(source);
            
            Assert.AreEqual(1, ast.Statements.Count);
            var varDecl = ast.Statements[0] as VariableDeclaration;
            Assert.IsNotNull(varDecl);
            
            var lambda = varDecl.Initializer as LambdaExpression;
            Assert.IsNotNull(lambda);
            Assert.AreEqual(2, lambda.Parameters.Count);
        }
        
        [Test("Parse async function")]
        public void TestAsyncFunction()
        {
            var source = @"
                async function FetchData() -> string {
                    return await httpClient.GetString(url);
                }
            ";
            var ast = ParseSource(source);
            
            Assert.AreEqual(1, ast.Statements.Count);
            var funcDecl = ast.Statements[0] as FunctionDeclaration;
            Assert.IsNotNull(funcDecl);
            Assert.IsTrue(funcDecl.IsAsync);
        }
        
        [Test("Parse try-catch statement")]
        public void TestTryCatchStatement()
        {
            var source = @"
                try {
                    result = riskyOperation();
                } catch (Exception e) {
                    handleError(e);
                } finally {
                    cleanup();
                }
            ";
            var ast = ParseSource(source);
            
            Assert.AreEqual(1, ast.Statements.Count);
            var tryStmt = ast.Statements[0] as TryStatement;
            Assert.IsNotNull(tryStmt);
            Assert.IsNotNull(tryStmt.TryBlock);
            Assert.AreEqual(1, tryStmt.CatchClauses.Count);
            Assert.IsNotNull(tryStmt.FinallyBlock);
        }
        
        [Test("Parse interface declaration")]
        public void TestInterfaceDeclaration()
        {
            var source = @"
                interface IDrawable {
                    function Draw() -> void;
                    property Color { get; set; }
                }
            ";
            var ast = ParseSource(source);
            
            Assert.AreEqual(1, ast.Statements.Count);
            var interfaceDecl = ast.Statements[0] as InterfaceDeclaration;
            Assert.IsNotNull(interfaceDecl);
            Assert.AreEqual("IDrawable", interfaceDecl.Name);
            Assert.AreEqual(2, interfaceDecl.Members.Count);
        }
        
        [Test("Parse enum declaration")]
        public void TestEnumDeclaration()
        {
            var source = @"
                enum Color {
                    Red = 1,
                    Green = 2,
                    Blue = 4
                }
            ";
            var ast = ParseSource(source);
            
            Assert.AreEqual(1, ast.Statements.Count);
            var enumDecl = ast.Statements[0] as EnumDeclaration;
            Assert.IsNotNull(enumDecl);
            Assert.AreEqual("Color", enumDecl.Name);
            Assert.AreEqual(3, enumDecl.Members.Count);
        }
        
        [Test("Parse generic class")]
        public void TestGenericClass()
        {
            var source = @"
                class List<T> {
                    private T[] items;
                    
                    public function Add(T item) -> void {
                        // Add implementation
                    }
                }
            ";
            var ast = ParseSource(source);
            
            Assert.AreEqual(1, ast.Statements.Count);
            var classDecl = ast.Statements[0] as ClassDeclaration;
            Assert.IsNotNull(classDecl);
            Assert.AreEqual(1, classDecl.TypeParameters.Count);
            Assert.AreEqual("T", classDecl.TypeParameters[0].Name);
        }
        
        [Test("Parse operator overloading")]
        public void TestOperatorOverloading()
        {
            var source = @"
                class Vector {
                    public operator +(Vector a, Vector b) -> Vector {
                        return new Vector(a.x + b.x, a.y + b.y);
                    }
                }
            ";
            var ast = ParseSource(source);
            
            Assert.AreEqual(1, ast.Statements.Count);
            var classDecl = ast.Statements[0] as ClassDeclaration;
            Assert.IsNotNull(classDecl);
            
            var opDecl = classDecl.Members[0] as FunctionDeclaration;
            Assert.IsNotNull(opDecl);
            Assert.Contains(opDecl.Modifiers, Modifier.Operator);
        }
        
        [Test("Parse pattern matching")]
        public void TestPatternMatching()
        {
            var source = @"
                var result = value switch {
                    0 => ""zero"",
                    1 => ""one"",
                    _ => ""many""
                };
            ";
            var ast = ParseSource(source);
            
            Assert.AreEqual(1, ast.Statements.Count);
            var varDecl = ast.Statements[0] as VariableDeclaration;
            Assert.IsNotNull(varDecl);
            
            var matchExpr = varDecl.Initializer as MatchExpression;
            Assert.IsNotNull(matchExpr);
            Assert.AreEqual(3, matchExpr.Arms.Count);
        }
        
        [Test("Parse null coalescing")]
        public void TestNullCoalescing()
        {
            var source = "string name = user?.Name ?? \"Anonymous\";";
            var ast = ParseSource(source);
            
            Assert.AreEqual(1, ast.Statements.Count);
            var varDecl = ast.Statements[0] as VariableDeclaration;
            Assert.IsNotNull(varDecl);
            
            var coalescingExpr = varDecl.Initializer as BinaryExpression;
            Assert.IsNotNull(coalescingExpr);
            Assert.AreEqual(TokenType.NullCoalescing, coalescingExpr.Operator.Type);
        }
        
        [Test("Parse string interpolation")]
        public void TestStringInterpolation()
        {
            var source = "string message = $\"Hello, {name}! You have {count} messages.\";";
            var ast = ParseSource(source);
            
            Assert.AreEqual(1, ast.Statements.Count);
            var varDecl = ast.Statements[0] as VariableDeclaration;
            Assert.IsNotNull(varDecl);
            
            var interpExpr = varDecl.Initializer as InterpolatedStringExpression;
            Assert.IsNotNull(interpExpr);
            Assert.Greater(interpExpr.Parts.Count, 1);
        }
        
        [Test("Parse error handling")]
        public void TestParseError()
        {
            var source = "int x = ;"; // Invalid syntax
            
            Assert.Throws<ParseException>(() => ParseSource(source));
        }
        
        private Program ParseSource(string source)
        {
            var lexer = new Lexer(source);
            var tokens = lexer.ScanTokens();
            parser = new Parser(tokens);
            return parser.Parse();
        }
    }
} 