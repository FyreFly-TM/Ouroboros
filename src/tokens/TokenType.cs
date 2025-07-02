namespace Ouro.Tokens
{
    /// <summary>
    /// Comprehensive token types for the Ouroboros language supporting all three syntax levels
    /// </summary>
    public enum TokenType
    {
        // Special
        Unknown,
        EndOfFile,
        NewLine,
        Whitespace,
        Comment,
        MultiLineComment,
        DocumentationComment,

        // Identifiers and Literals
        Identifier,
        IntegerLiteral,
        FloatLiteral,
        DoubleLiteral,
        DecimalLiteral,
        HexLiteral,
        BinaryLiteral,
        OctalLiteral,
        StringLiteral,
        CharLiteral,
        BooleanLiteral,
        NullLiteral,
        InterpolatedString,
        RawString,
        UnitLiteral,        // Physical unit literals like "120 V" or "60 Hz"
        
        // Keywords - Control Flow
        If,
        Else,
        ElseIf,
        Switch,
        Case,
        Default,
        For,
        ForEach,
        ForIn,
        ForOf,
        While,
        Do,
        Loop,
        Until,
        Break,
        Continue,
        Return,
        Yield,
        Await,
        Async,
        
        // Keywords - Declarations
        Class,
        Interface,
        Struct,
        UnionKeyword,
        Enum,
        Function,
        Namespace,
        Module,
        Package,
        Import,
        Export,
        Using,
        Alias,
        Typedef,
        Domain,
        
        // Keywords - Modifiers
        Public,
        Private,
        Protected,
        Internal,
        Static,
        Final,
        Const,
        Readonly,
        Volatile,
        Abstract,
        Virtual,
        Override,
        Sealed,
        Partial,
        
        // Keywords - Types
        Type,
        Var,
        Let,
        Void,
        Bool,
        Byte,
        SByte,
        Short,
        UShort,
        Int,
        UInt,
        Long,
        ULong,
        Float,
        Double,
        Decimal,
        Char,
        String,
        Object,
        Dynamic,
        Any,
        
        // Keywords - Memory
        New,
        Delete,
        Malloc,
        Free,
        Sizeof,
        Typeof,
        Nameof,
        Stackalloc,
        
        // Keywords - Exception Handling
        Try,
        Catch,
        Finally,
        Throw,
        Throws,
        
        // Keywords - Special
        This,
        Base,
        Super,
        Self,
        Is,
        As,
        In,
        Out,
        Ref,
        Params,
        
        // Operators - Arithmetic
        Plus,               // +
        Minus,              // -
        Multiply,           // *
        Divide,             // /
        Modulo,             // %
        Power,              // **
        IntegerDivide,      // //
        
        // Operators - Assignment
        Assign,             // =
        PlusAssign,         // +=
        MinusAssign,        // -=
        MultiplyAssign,     // *=
        DivideAssign,       // /=
        ModuloAssign,       // %=
        PowerAssign,        // **=
        BitwiseAndAssign,   // &=
        BitwiseOrAssign,    // |=
        BitwiseXorAssign,   // ^=
        LeftShiftAssign,    // <<=
        RightShiftAssign,   // >>=
        LogicalAndAssign,   // &&=
        LogicalOrAssign,    // ||=
        NullCoalesceAssign, // ??=
        
        // Operators - Comparison
        Equal,              // ==
        NotEqual,           // !=
        Less,               // <
        Greater,            // >
        LessEqual,          // <=
        GreaterEqual,       // >=
        Spaceship,          // <=>
        
        // Operators - Logical
        LogicalAnd,         // &&
        LogicalOr,          // ||
        LogicalNot,         // !
        LogicalXor,         // ^^
        
        // Operators - Bitwise
        BitwiseAnd,         // &
        BitwiseOr,          // |
        BitwiseXor,         // ^
        BitwiseNot,         // ~
        LeftShift,          // <<
        RightShift,         // >>
        UnsignedRightShift, // >>>
        
        // Operators - Unary
        Increment,          // ++
        Decrement,          // --
        
        // Operators - Special
        Dot,                // .
        Arrow,              // ->
        DoubleArrow,        // =>
        Question,           // ?
        NullCoalesce,       // ??
        NullConditional,    // ?.
        Range,              // ..
        Spread,             // ...
        Pipe,               // |>
        Compose,            // >>
        ReverseCompose,     // <<
        
        // Delimiters
        LeftParen,          // (
        RightParen,         // )
        LeftBrace,          // {
        RightBrace,         // }
        LeftBracket,        // [
        RightBracket,       // ]
        LeftAngle,          // <
        RightAngle,         // >
        Semicolon,          // ;
        Comma,              // ,
        Colon,              // :
        DoubleColon,        // ::
        At,                 // @
        Hash,               // #
        Dollar,             // $
        Backtick,           // `
        
        // Greek Letters (Math Support)
        Alpha,              // α
        Beta,               // β
        Gamma,              // γ
        Delta,              // δ
        Epsilon,            // ε
        Zeta,               // ζ
        Eta,                // η
        Theta,              // θ
        Iota,               // ι
        Kappa,              // κ
        Lambda,             // λ
        Mu,                 // μ
        Nu,                 // ν
        Xi,                 // ξ
        Omicron,            // ο
        Pi,                 // π
        Rho,                // ρ
        Sigma,              // σ
        Tau,                // τ
        Upsilon,            // υ
        Phi,                // φ
        Chi,                // χ
        Psi,                // ψ
        Omega,              // ω
        
        // Math Symbols
        Infinity,           // ∞
        PlusMinus,          // ±
        MinusPlus,          // ∓
        Times,              // ×
        DivisionSign,       // ÷
        NotEqual2,          // ≠
        LessOrEqual,        // ≤
        GreaterOrEqual,     // ≥
        Almost,             // ≈
        NotAlmost,          // ≉
        Identical,          // ≡
        NotIdentical,       // ≢
        Proportional,       // ∝
        Element,            // ∈
        NotElement,         // ∉
        Subset,             // ⊂
        Superset,           // ⊃
        SubsetEqual,        // ⊆
        SupersetEqual,      // ⊇
        Union,              // ∪
        Intersection,       // ∩
        EmptySet,           // ∅
        Nabla,              // ∇
        PartialDerivative,  // ∂
        Integral,           // ∫
        DoubleIntegral,     // ∬
        TripleIntegral,     // ∭
        ContourIntegral,    // ∮
        Summation,          // Σ
        Product,            // Π
        SquareRoot,         // √
        CubeRoot,           // ∛
        FourthRoot,         // ∜
        
        // Special Math Operations
        Dot3D,              // ⋅ (dot product)
        Cross3D,            // × (cross product)
        Tensor,             // ⊗ (tensor product)
        
        // Syntax Level Markers
        HighLevel,          // @high
        MediumLevel,        // @medium
        LowLevel,           // @low
        Assembly,           // @asm
        SpirvAssembly,      // @asm spirv
        
        // Compilation & Optimization Attributes
        Inline,             // @inline
        CompileTime,        // @compile_time
        Emit,               // @emit
        ZeroCost,           // @zero_cost
        Allocates,          // @allocates
        NoStd,              // @no_std
        NoAlloc,            // @no_alloc
        Cfg,                // @cfg
        Naked,              // @naked
        NoStack,            // @no_stack
        NoMangle,           // @no_mangle
        VolatileAttr,       // @volatile (attribute, different from Volatile keyword)
        Packed,             // @packed
        Section,            // @section
        Repr,               // @repr
        
        // GPU & Parallel Attributes
        Gpu,                // @gpu
        Kernel,             // @kernel
        Shared,             // @shared
        Simd,               // @simd
        Parallel,           // @parallel
        WasmSimd,           // @wasm_simd
        
        // Memory & Security Attributes
        GlobalAllocator,    // @global_allocator
        Secure,             // @secure
        ConstantTime,       // @constant_time
        
        // Database Attributes
        Table,              // @table
        PrimaryKey,         // @primary_key
        Index,              // @index
        ForeignKey,         // @foreign_key
        
        // Real-time System Attributes
        RealTime,           // @real_time
        PriorityCeiling,    // @priority_ceiling
        Periodic,           // @periodic
        Deadline,           // @deadline
        Wcet,               // @wcet
        TimedSection,       // @timed_section
        Sporadic,           // @sporadic
        CyclicExecutive,    // @cyclic_executive
        
        // Verification Attributes
        Verified,           // @verified
        Ghost,              // @ghost
        
        // Machine Learning Attributes
        Differentiable,     // @differentiable
        Model,              // @model
        
        // Web/WASM Attributes
        Wasm,               // @wasm
        Webgl,              // @webgl
        Component,          // @component
        State,              // @state
        ImportAttr,         // @import (attribute, different from Import keyword)
        
        // Concurrency Attributes
        Actor,              // @actor
        Receive,            // @receive
        Supervisor,         // @supervisor
        
        // Smart Contract Attributes
        Contract,           // @contract
        Payable,            // @payable
        View,               // @view
        External,           // @external
        Event,              // @event
        Oracle,             // @oracle
        StateChannel,       // @state_channel
        
        // Scientific Computing Attributes
        Dna,                // @dna
        Genomics,           // @genomics
        MolecularDynamics,  // @molecular_dynamics
        Mpc,                // @mpc
        Zkp,                // @zkp
        SpatialIndex,       // @spatial_index
        FixedPoint,         // @fixed_point
        
        // Graphics Attributes
        Shader,             // @shader
        
        // Assembly Specific
        AsmRegister,
        AsmInstruction,
        AsmLabel,
        AsmDirective,
        
        // Preprocessor
        PreprocessorIf,
        PreprocessorElse,
        PreprocessorEndIf,
        PreprocessorDefine,
        PreprocessorUndef,
        PreprocessorInclude,
        PreprocessorPragma,
        
        // Attributes
        AttributeStart,     // [
        AttributeEnd,       // ]
        
        // Custom Loop Constructs
        Iterate,            // iterate
        Repeat,             // repeat
        Forever,            // forever
        ParallelFor,        // parallel for
        AsyncFor,           // async for
        
        // Natural Language Keywords
        Print,              // print (for high-level syntax)
        Define,             // define
        Taking,             // taking
        Through,            // through
        From,               // from
        To,                 // to
        End,                // end
        Then,               // then
        Otherwise,          // otherwise
        Each,               // each
        All,                // all
        Where,              // where
        Item,               // item
        Numbers,            // numbers
        Even,               // even
        Odd,                // odd
        Multiplied,         // multiplied
        By,                 // by
        Divided,            // divided
        Counter,            // counter
        Than,               // than
        Length,             // length
        Width,              // width
        Area,               // area
        Error,              // error
        Cannot,             // cannot
        End_Repeat,         // end repeat
        End_For,            // end for
        End_If,             // end if
        End_Function,       // end function
        End_Iterate,        // end iterate
        For_Each,           // for each
        Iterate_Counter,    // iterate counter
        Is_Greater_Than,    // is greater than
        Is_Element_Of,      // is element of
        
        // Mathematical Expression Keywords
        Limit,              // limit
        Origin,             // origin
        Means,              // means (for domain operator definitions)
        SetDifference,      // \ (set difference operator)
        Approaches,         // approaches (for limits)
        
        // Pattern Matching
        Match,              // match
        When,               // when
        With,               // with
        Underscore,         // _ (wildcard pattern)
        
        // Data Oriented
        Data,               // data
        System,             // system
        Entity,             // entity
        
        // Memory Management
        Pin,                // pin
        Unpin,              // unpin
        Unsafe,             // unsafe
        Fixed,              // fixed
        
        // Contracts
        Requires,           // requires
        Ensures,            // ensures
        Invariant,          // invariant
        
        // Meta Programming
        Macro,              // macro
        Template,           // template
        Generic,            // generic
        Concept,            // concept
        
        // Concurrency
        Thread,             // thread
        ThreadLocal,        // thread_local
        Lock,               // lock
        Atomic,             // atomic
        Channel,            // channel
        Select,             // select
        Go,                 // go
        
        // SIMD/Vector
        Vector,             // vector
        Matrix,             // matrix
        Quaternion,         // quaternion
        Transform,          // transform
        
        // Additional Mathematical Symbols  
        Therefore,          // ∴
        Because,            // ∵  
        ForAll,             // ∀
        Exists,             // ∃
        NotExists,          // ∄
        Oplus,              // ⊕
        Ominus,             // ⊖
        Odot,               // ⊙
        Boxtimes,           // ⊠
        Boxdot,             // ⊡
        Vdash,              // ⊢
        Dashv,              // ⊣
        Top,                // ⊤
        Bottom,             // ⊥
        Models,             // ⊨
        Forces,             // ⊩
        Forces2,            // ⊪
        NotForces,          // ⊫
        Lceil,              // ⌈
        Rceil,              // ⌉
        Lfloor,             // ⌊
        Rfloor,             // ⌋
        Langle,             // 〈
        Rangle,             // 〉
        
        // Arrow symbols
        Uparrow,            // ↑
        Downarrow,          // ↓
        Leftrightarrow,     // ↔
        Implies,            // ⇒
        Implied,            // ⇐
        Iff,                // ⇔
        Uparrow2,           // ⇑
        Downarrow2,         // ⇓
        Mapsto,             // ↪
        Hookleftarrow,      // ↩
        Circlearrowleft,    // ↺
        Circlearrowright,   // ↻
        
        // Time/Special keywords for natural language syntax  
        // At is already defined above in the Delimiters section
    }
} 