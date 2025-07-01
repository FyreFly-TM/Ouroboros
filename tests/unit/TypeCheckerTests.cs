using System;
using System.Collections.Generic;
using Ouroboros.Core.Compiler;
using Ouroboros.Core.Parser;
using Ouroboros.Core.Lexer;
using Ouroboros.Core.AST;
using Ouroboros.Testing;

namespace Ouroboros.Tests.Unit
{
    [TestClass]
    public class TypeCheckerTests
    {
        private TypeChecker typeChecker;
        
        [Test("Type check basic types")]
        public void TestBasicTypes()
        {
            var source = @"
                int x = 42;
                string s = ""hello"";
                bool b = true;
                float f = 3.14;
            ";
            
            var ast = ParseSource(source);
            typeChecker = new TypeChecker();
            typeChecker.Check(ast);
            
            // Should complete without errors
            Assert.AreEqual(0, typeChecker.Errors.Count);
        }
        
        [Test("Type mismatch error")]
        public void TestTypeMismatch()
        {
            var source = "int x = \"not a number\";";
            var ast = ParseSource(source);
            
            typeChecker = new TypeChecker();
            typeChecker.Check(ast);
            
            Assert.Greater(typeChecker.Errors.Count, 0);
            Assert.Contains(typeChecker.Errors[0].Message.ToLower(), "type mismatch");
        }
        
        [Test("Undefined variable error")]
        public void TestUndefinedVariable()
        {
            var source = "int x = y + 1;";
            var ast = ParseSource(source);
            
            typeChecker = new TypeChecker();
            typeChecker.Check(ast);
            
            Assert.Greater(typeChecker.Errors.Count, 0);
            Assert.Contains(typeChecker.Errors[0].Message.ToLower(), "undefined");
        }
        
        [Test("Function parameter types")]
        public void TestFunctionParameterTypes()
        {
            var source = @"
                function Add(int a, int b) -> int {
                    return a + b;
                }
                
                int result = Add(5, 10);
            ";
            
            var ast = ParseSource(source);
            typeChecker = new TypeChecker();
            typeChecker.Check(ast);
            
            Assert.AreEqual(0, typeChecker.Errors.Count);
        }
        
        [Test("Function argument mismatch")]
        public void TestFunctionArgumentMismatch()
        {
            var source = @"
                function Add(int a, int b) -> int {
                    return a + b;
                }
                
                int result = Add(""five"", ""ten"");
            ";
            
            var ast = ParseSource(source);
            typeChecker = new TypeChecker();
            typeChecker.Check(ast);
            
            Assert.Greater(typeChecker.Errors.Count, 0);
        }
        
        [Test("Array type checking")]
        public void TestArrayTypes()
        {
            var source = @"
                int[] numbers = [1, 2, 3, 4, 5];
                string[] words = [""hello"", ""world""];
                numbers[0] = 42;
                words[1] = ""test"";
            ";
            
            var ast = ParseSource(source);
            typeChecker = new TypeChecker();
            typeChecker.Check(ast);
            
            Assert.AreEqual(0, typeChecker.Errors.Count);
        }
        
        [Test("Generic type checking")]
        public void TestGenericTypes()
        {
            var source = @"
                class List<T> {
                    private T[] items;
                    
                    public function Add(T item) -> void {
                        // Implementation
                    }
                    
                    public function Get(int index) -> T {
                        return items[index];
                    }
                }
                
                List<int> numbers = new List<int>();
                numbers.Add(42);
                int value = numbers.Get(0);
            ";
            
            var ast = ParseSource(source);
            typeChecker = new TypeChecker();
            typeChecker.Check(ast);
            
            Assert.AreEqual(0, typeChecker.Errors.Count);
        }
        
        [Test("Class inheritance type checking")]
        public void TestInheritance()
        {
            var source = @"
                class Animal {
                    public function Speak() -> void { }
                }
                
                class Dog : Animal {
                    public override function Speak() -> void { }
                }
                
                Animal animal = new Dog();
                animal.Speak();
            ";
            
            var ast = ParseSource(source);
            typeChecker = new TypeChecker();
            typeChecker.Check(ast);
            
            Assert.AreEqual(0, typeChecker.Errors.Count);
        }
        
        [Test("Interface implementation checking")]
        public void TestInterfaceImplementation()
        {
            var source = @"
                interface IDrawable {
                    function Draw() -> void;
                }
                
                class Circle : IDrawable {
                    public function Draw() -> void {
                        // Implementation
                    }
                }
                
                IDrawable drawable = new Circle();
                drawable.Draw();
            ";
            
            var ast = ParseSource(source);
            typeChecker = new TypeChecker();
            typeChecker.Check(ast);
            
            Assert.AreEqual(0, typeChecker.Errors.Count);
        }
        
        [Test("Null safety checking")]
        public void TestNullSafety()
        {
            var source = @"
                string? nullableName = null;
                string name = nullableName ?? ""default"";
                
                if (nullableName != null) {
                    int length = nullableName.Length;
                }
            ";
            
            var ast = ParseSource(source);
            typeChecker = new TypeChecker();
            typeChecker.Check(ast);
            
            Assert.AreEqual(0, typeChecker.Errors.Count);
        }
        
        [Test("Operator type checking")]
        public void TestOperatorTypes()
        {
            var source = @"
                int a = 5 + 3;
                string s = ""hello"" + "" world"";
                bool b = 5 > 3;
                float f = 3.14 * 2.0;
            ";
            
            var ast = ParseSource(source);
            typeChecker = new TypeChecker();
            typeChecker.Check(ast);
            
            Assert.AreEqual(0, typeChecker.Errors.Count);
        }
        
        [Test("Lambda type inference")]
        public void TestLambdaTypeInference()
        {
            var source = @"
                var add = (int a, int b) => a + b;
                int result = add(5, 3);
                
                var numbers = [1, 2, 3, 4, 5];
                var doubled = numbers.Map(x => x * 2);
            ";
            
            var ast = ParseSource(source);
            typeChecker = new TypeChecker();
            typeChecker.Check(ast);
            
            Assert.AreEqual(0, typeChecker.Errors.Count);
        }
        
        [Test("Pattern matching type checking")]
        public void TestPatternMatchingTypes()
        {
            var source = @"
                object value = 42;
                
                string result = value switch {
                    int n => ""number: "" + n,
                    string s => ""string: "" + s,
                    _ => ""unknown""
                };
            ";
            
            var ast = ParseSource(source);
            typeChecker = new TypeChecker();
            typeChecker.Check(ast);
            
            Assert.AreEqual(0, typeChecker.Errors.Count);
        }
        
        [Test("Async/await type checking")]
        public void TestAsyncAwaitTypes()
        {
            var source = @"
                async function GetDataAsync() -> Task<string> {
                    return await FetchFromServerAsync();
                }
                
                async function UseDataAsync() -> Task<void> {
                    string data = await GetDataAsync();
                    Console.WriteLine(data);
                }
            ";
            
            var ast = ParseSource(source);
            typeChecker = new TypeChecker();
            typeChecker.Check(ast);
            
            Assert.AreEqual(0, typeChecker.Errors.Count);
        }
        
        [Test("Unit literal type checking")]
        public void TestUnitLiterals()
        {
            var source = @"
                var distance = 10<m>;
                var time = 5<s>;
                var velocity = distance / time; // Should be m/s
                
                var mass = 2<kg>;
                var acceleration = 9.8<m/s^2>;
                var force = mass * acceleration; // Should be N (newtons)
            ";
            
            var ast = ParseSource(source);
            typeChecker = new TypeChecker();
            typeChecker.Check(ast);
            
            Assert.AreEqual(0, typeChecker.Errors.Count);
        }
        
        private Program ParseSource(string source)
        {
            var lexer = new Lexer(source);
            var tokens = lexer.ScanTokens();
            var parser = new Parser(tokens);
            return parser.Parse();
        }
    }
} 