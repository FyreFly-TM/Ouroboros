using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Ouroboros.Core
{
    /// <summary>
    /// Comprehensive diagnostic engine for error reporting, warnings, and analysis
    /// </summary>
    public class DiagnosticEngine
    {
        private readonly List<Diagnostic> diagnostics = new();
        private readonly DiagnosticOptions options;
        
        public DiagnosticEngine(DiagnosticOptions? options = null)
        {
            this.options = options ?? new DiagnosticOptions();
        }
        
        public bool HasErrors => diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error);
        public bool HasWarnings => diagnostics.Any(d => d.Severity == DiagnosticSeverity.Warning);
        public int ErrorCount => diagnostics.Count(d => d.Severity == DiagnosticSeverity.Error);
        public int WarningCount => diagnostics.Count(d => d.Severity == DiagnosticSeverity.Warning);
        
        public List<Diagnostic> GetDiagnostics() => new List<Diagnostic>(diagnostics);
        public List<Diagnostic> GetErrors() => diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ToList();
        public List<Diagnostic> GetWarnings() => diagnostics.Where(d => d.Severity == DiagnosticSeverity.Warning).ToList();
        
        #region Error Reporting Methods
        
        /// <summary>
        /// Report a syntax error
        /// </summary>
        public void ReportSyntaxError(string message, SourceLocation location)
        {
            ReportDiagnostic(DiagnosticSeverity.Error, DiagnosticCode.SyntaxError, message, location);
        }
        
        /// <summary>
        /// Report a semantic error
        /// </summary>
        public void ReportSemanticError(string message, SourceLocation location)
        {
            ReportDiagnostic(DiagnosticSeverity.Error, DiagnosticCode.SemanticError, message, location);
        }
        
        /// <summary>
        /// Report a type error
        /// </summary>
        public void ReportTypeError(string message, SourceLocation location)
        {
            ReportDiagnostic(DiagnosticSeverity.Error, DiagnosticCode.TypeError, message, location);
        }
        
        /// <summary>
        /// Report mathematical notation error
        /// </summary>
        public void ReportMathematicalError(string message, SourceLocation location)
        {
            ReportDiagnostic(DiagnosticSeverity.Error, DiagnosticCode.MathematicalError, message, location);
        }
        
        /// <summary>
        /// Report units/dimensional analysis error
        /// </summary>
        public void ReportUnitsError(string message, SourceLocation location)
        {
            ReportDiagnostic(DiagnosticSeverity.Error, DiagnosticCode.UnitsError, message, location);
        }
        
        /// <summary>
        /// Report memory safety error
        /// </summary>
        public void ReportMemorySafetyError(string message, SourceLocation location)
        {
            ReportDiagnostic(DiagnosticSeverity.Error, DiagnosticCode.MemorySafetyError, message, location);
        }
        
        /// <summary>
        /// Report assembly integration error
        /// </summary>
        public void ReportAssemblyError(string message, SourceLocation location)
        {
            ReportDiagnostic(DiagnosticSeverity.Error, DiagnosticCode.AssemblyError, message, location);
        }
        
        /// <summary>
        /// Report concurrency error
        /// </summary>
        public void ReportConcurrencyError(string message, SourceLocation location)
        {
            ReportDiagnostic(DiagnosticSeverity.Error, DiagnosticCode.ConcurrencyError, message, location);
        }
        
        /// <summary>
        /// Report real-time constraint violation
        /// </summary>
        public void ReportRealTimeError(string message, SourceLocation location)
        {
            ReportDiagnostic(DiagnosticSeverity.Error, DiagnosticCode.RealTimeError, message, location);
        }
        
        /// <summary>
        /// Report contract violation
        /// </summary>
        public void ReportContractViolation(string message, SourceLocation location)
        {
            ReportDiagnostic(DiagnosticSeverity.Error, DiagnosticCode.ContractViolation, message, location);
        }
        
        /// <summary>
        /// Report security vulnerability
        /// </summary>
        public void ReportSecurityError(string message, SourceLocation location)
        {
            ReportDiagnostic(DiagnosticSeverity.Error, DiagnosticCode.SecurityError, message, location);
        }
        
        /// <summary>
        /// Report internal compiler error
        /// </summary>
        public void ReportInternalError(string message, Exception? exception = null)
        {
            var diagnostic = new Diagnostic
            {
                Severity = DiagnosticSeverity.Error,
                Code = DiagnosticCode.InternalError,
                Message = $"Internal compiler error: {message}",
                Location = SourceLocation.Unknown,
                Exception = exception
            };
            
            diagnostics.Add(diagnostic);
            
            if (options.ThrowOnInternalError)
                throw new InternalCompilerException(message, exception);
        }
        
        #endregion
        
        #region Warning Methods
        
        /// <summary>
        /// Report performance warning
        /// </summary>
        public void ReportPerformanceWarning(string message, SourceLocation location)
        {
            ReportDiagnostic(DiagnosticSeverity.Warning, DiagnosticCode.PerformanceWarning, message, location);
        }
        
        /// <summary>
        /// Report deprecated feature usage
        /// </summary>
        public void ReportDeprecatedWarning(string message, SourceLocation location)
        {
            ReportDiagnostic(DiagnosticSeverity.Warning, DiagnosticCode.DeprecatedWarning, message, location);
        }
        
        /// <summary>
        /// Report unused variable/function
        /// </summary>
        public void ReportUnusedWarning(string message, SourceLocation location)
        {
            ReportDiagnostic(DiagnosticSeverity.Warning, DiagnosticCode.UnusedWarning, message, location);
        }
        
        /// <summary>
        /// Report style/formatting warning
        /// </summary>
        public void ReportStyleWarning(string message, SourceLocation location)
        {
            ReportDiagnostic(DiagnosticSeverity.Warning, DiagnosticCode.StyleWarning, message, location);
        }
        
        #endregion
        
        #region Info/Hint Methods
        
        /// <summary>
        /// Report informational message
        /// </summary>
        public void ReportInfo(string message, SourceLocation location)
        {
            ReportDiagnostic(DiagnosticSeverity.Info, DiagnosticCode.Information, message, location);
        }
        
        /// <summary>
        /// Report optimization hint
        /// </summary>
        public void ReportOptimizationHint(string message, SourceLocation location)
        {
            ReportDiagnostic(DiagnosticSeverity.Hint, DiagnosticCode.OptimizationHint, message, location);
        }
        
        #endregion
        
        /// <summary>
        /// Core method to report diagnostic
        /// </summary>
        private void ReportDiagnostic(DiagnosticSeverity severity, DiagnosticCode code, string message, SourceLocation location)
        {
            var diagnostic = new Diagnostic
            {
                Severity = severity,
                Code = code,
                Message = message,
                Location = location
            };
            
            diagnostics.Add(diagnostic);
            
            // Optional: Report to console immediately if configured
            if (options.ReportImmediately)
            {
                Console.WriteLine(FormatDiagnostic(diagnostic));
            }
        }
        
        /// <summary>
        /// Format diagnostic for display
        /// </summary>
        public string FormatDiagnostic(Diagnostic diagnostic)
        {
            var sb = new StringBuilder();
            
            // Format: filename(line,col): severity code: message
            if (!diagnostic.Location.Equals(SourceLocation.Unknown))
            {
                sb.Append($"{diagnostic.Location.FileName}({diagnostic.Location.Line},{diagnostic.Location.Column}): ");
            }
            
            sb.Append($"{diagnostic.Severity.ToString().ToLower()} ");
            sb.Append($"{diagnostic.Code}: ");
            sb.Append(diagnostic.Message);
            
            return sb.ToString();
        }
        
        /// <summary>
        /// Format all diagnostics for display
        /// </summary>
        public string FormatDiagnostics()
        {
            var sb = new StringBuilder();
            
            foreach (var diagnostic in diagnostics)
            {
                sb.AppendLine(FormatDiagnostic(diagnostic));
            }
            
            if (HasErrors || HasWarnings)
            {
                sb.AppendLine();
                sb.AppendLine($"Build {(HasErrors ? "FAILED" : "succeeded")}.");
                sb.AppendLine($"    {WarningCount} Warning(s)");
                sb.AppendLine($"    {ErrorCount} Error(s)");
            }
            
            return sb.ToString();
        }
        
        /// <summary>
        /// Clear all diagnostics
        /// </summary>
        public void Clear()
        {
            diagnostics.Clear();
        }
    }
    
    /// <summary>
    /// Individual diagnostic message
    /// </summary>
    public class Diagnostic
    {
        public DiagnosticSeverity Severity { get; set; }
        public DiagnosticCode Code { get; set; }
        public string Message { get; set; } = "";
        public SourceLocation Location { get; set; }
        public Exception? Exception { get; set; }
        public string? HelpText { get; set; }
        public List<SourceLocation> RelatedLocations { get; set; } = new();
    }
    
    /// <summary>
    /// Source location information
    /// </summary>
    public struct SourceLocation
    {
        public string FileName { get; set; }
        public int Line { get; set; }
        public int Column { get; set; }
        public int Position { get; set; }
        public int Length { get; set; }
        
        public static readonly SourceLocation Unknown = new SourceLocation
        {
            FileName = "<unknown>",
            Line = 0,
            Column = 0,
            Position = 0,
            Length = 0
        };
        
        public SourceLocation(string fileName, int line, int column, int position = 0, int length = 0)
        {
            FileName = fileName;
            Line = line;
            Column = column;
            Position = position;
            Length = length;
        }
    }
    
    /// <summary>
    /// Diagnostic severity levels
    /// </summary>
    public enum DiagnosticSeverity
    {
        Hidden,
        Info,
        Hint,
        Warning,
        Error
    }
    
    /// <summary>
    /// Diagnostic codes for categorization
    /// </summary>
    public enum DiagnosticCode
    {
        // Syntax errors
        SyntaxError = 1000,
        UnexpectedToken = 1001,
        MissingToken = 1002,
        InvalidCharacter = 1003,
        
        // Semantic errors
        SemanticError = 2000,
        UndefinedIdentifier = 2001,
        DuplicateIdentifier = 2002,
        InvalidOperation = 2003,
        
        // Type errors
        TypeError = 3000,
        TypeMismatch = 3001,
        InvalidCast = 3002,
        GenericConstraintViolation = 3003,
        
        // Mathematical errors
        MathematicalError = 4000,
        InvalidMathematicalExpression = 4001,
        DimensionalAnalysisError = 4002,
        SymbolicMathError = 4003,
        
        // Units errors
        UnitsError = 5000,
        IncompatibleUnits = 5001,
        MissingUnits = 5002,
        
        // Memory safety
        MemorySafetyError = 6000,
        UseAfterFree = 6001,
        BufferOverflow = 6002,
        NullPointerDereference = 6003,
        
        // Assembly integration
        AssemblyError = 7000,
        InvalidAssemblyInstruction = 7001,
        RegisterConflict = 7002,
        AssemblyTypeMismatch = 7003,
        
        // Concurrency
        ConcurrencyError = 8000,
        RaceCondition = 8001,
        DeadlockDetected = 8002,
        InvalidAtomicOperation = 8003,
        
        // Real-time
        RealTimeError = 9000,
        DeadlineMissed = 9001,
        TimingConstraintViolation = 9002,
        
        // Contracts
        ContractViolation = 10000,
        PreconditionViolation = 10001,
        PostconditionViolation = 10002,
        InvariantViolation = 10003,
        
        // Security
        SecurityError = 11000,
        SecurityVulnerability = 11001,
        CryptographicError = 11002,
        
        // Warnings
        PerformanceWarning = 20000,
        DeprecatedWarning = 20001,
        UnusedWarning = 20002,
        StyleWarning = 20003,
        
        // Info/Hints
        Information = 30000,
        OptimizationHint = 30001,
        
        // Internal
        InternalError = 90000
    }
    
    /// <summary>
    /// Options for diagnostic engine behavior
    /// </summary>
    public class DiagnosticOptions
    {
        public bool ReportImmediately { get; set; } = false;
        public bool ThrowOnInternalError { get; set; } = false;
        public DiagnosticSeverity MinimumSeverity { get; set; } = DiagnosticSeverity.Info;
        public bool WarningsAsErrors { get; set; } = false;
        public HashSet<DiagnosticCode> SuppressedCodes { get; set; } = new();
        public bool EnablePerformanceAnalysis { get; set; } = true;
        public bool EnableSecurityAnalysis { get; set; } = true;
    }
    
    /// <summary>
    /// Exception for internal compiler errors
    /// </summary>
    public class InternalCompilerException : Exception
    {
        public InternalCompilerException(string message) : base(message) { }
        public InternalCompilerException(string message, Exception? innerException) : base(message, innerException) { }
    }
} 