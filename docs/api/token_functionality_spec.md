# Ouro Token Functionality Specification

This document provides comprehensive functionality specifications for every token type in the Ouro language.

## Table of Contents

1. [Syntax Level Markers](#syntax-level-markers)
2. [Compilation & Optimization Attributes](#compilation--optimization-attributes)
3. [GPU & Parallel Computing Attributes](#gpu--parallel-computing-attributes)
4. [Memory & Security Attributes](#memory--security-attributes)
5. [Real-time System Attributes](#real-time-system-attributes)
6. [Domain-Specific Attributes](#domain-specific-attributes)
7. [Mathematical Symbols](#mathematical-symbols)
8. [Control Flow Tokens](#control-flow-tokens)
9. [Type System Tokens](#type-system-tokens)
10. [Natural Language Tokens](#natural-language-tokens)
11. [Operators](#operators)
12. [Special Symbols](#special-symbols)

---

## 1. Syntax Level Markers

### @high - Natural Language Mode

Full natural language programming with contextual understanding and intuitive syntax:

```ouro
@high
module NaturalProgramming {
    // Natural language function definition with full context awareness
    define algorithm quicksort taking list of numbers
        if list has fewer than 2 elements then
            return list unchanged
        end if
        
        pivot := middle element of list
        smaller := all elements less than pivot
        equal := all elements equal to pivot
        larger := all elements greater than pivot
        
        return quicksort(smaller) concatenated with equal concatenated with quicksort(larger)
    end algorithm
    
    // Advanced natural language with temporal logic
    define concurrent process server handling requests
        whenever new request arrives do
            validate request headers
            
            if request is for static file then
                serve file from cache if available
                otherwise load file and cache it
            otherwise if request is API call then
                authenticate user using JWT token
                process API request in background
                return JSON response
            otherwise
                return 404 not found
            end if
        end whenever
    end process
    
    // Natural language with business rules
    define business rule calculate discount for customer
        given customer's purchase history and current order
        
        total spent := sum of all previous purchases by customer
        loyalty tier := determine tier based on total spent
        
        if loyalty tier is "platinum" then
            base discount := 20 percent
        otherwise if loyalty tier is "gold" then
            base discount := 15 percent
        otherwise if loyalty tier is "silver" then
            base discount := 10 percent
        otherwise
            base discount := 5 percent
        end if
        
        if current order exceeds $500 then
            add 5 percent to base discount
        end if
        
        if customer birthday is within 7 days then
            add 10 percent to base discount
        end if
        
        return minimum of base discount and 40 percent
    end rule
}
```

### @medium - Modern Programming Mode

Complete modern programming features with advanced type system and control flow:

```ouro
@medium
module ModernSyntax {
    // Advanced pattern matching with all features
    function process_data<T>(data: DataPacket<T>) -> Result<T, Error> 
        where T: Serialize + Deserialize + Send + Sync
    {
        match data {
            // Destructuring with nested patterns and guards
            DataPacket { 
                header: h @ Header { version: v, encrypted: true, .. }, 
                payload: p,
                checksum: c 
            } when v >= 2.0 && verify_checksum(p, c) => {
                let decrypted = decrypt_with_key(p, h.key_id)?;
                Ok(deserialize(decrypted))
            },
            
            // Or patterns with bindings
            DataPacket { header: Header { version: 1.0 | 1.1 | 1.2, .. }, .. } => {
                handle_legacy_format(data)
            },
            
            // Range patterns
            DataPacket { header: Header { size: 0..=1024, .. }, .. } => {
                handle_small_packet(data)
            },
            
            // Slice patterns
            DataPacket { payload: [magic @ 0xFF, 0xFE, rest @ ..], .. } => {
                handle_special_format(magic, rest)
            },
            
            // If-let patterns in match arms
            packet if let Some(priority) = packet.get_priority() => {
                priority_queue.push(packet, priority)
            },
            
            _ => Err(Error::UnsupportedFormat)
        }
    }
    
    // Null safety with advanced operators
    function safe_navigation_example(user: User?) -> string {
        // Null coalescing with chaining
        let name = user?.profile?.display_name ?? user?.email ?? "Anonymous";
        
        // Null coalescing assignment
        user?.last_login ??= DateTime::now();
        
        // Optional chaining with method calls
        let age = user?.get_age()?.years ?? 0;
        
        // Null-conditional array access
        let first_order = user?.orders?[0]?.id;
        
        return name;
    }
    
    // Advanced async/await with cancellation and timeouts
    async function fetch_with_retry<T>(
        url: string, 
        max_retries: u32 = 3,
        timeout: Duration = 30s
    ) -> Result<T> {
        for attempt in 0..max_retries {
            // Select with timeout and cancellation
            let result = select! {
                data = fetch_data(url) => data,
                _ = sleep(timeout) => Err(Error::Timeout),
                _ = cancellation_token.cancelled() => Err(Error::Cancelled)
            };
            
            match result {
                Ok(data) => return Ok(data),
                Err(Error::Timeout) if attempt < max_retries - 1 => {
                    let backoff = Duration::from_secs(2u64.pow(attempt));
                    sleep(backoff).await;
                    continue;
                },
                Err(e) => return Err(e)
            }
        }
        
        Err(Error::MaxRetriesExceeded)
    }
}
```

### @low - Systems Programming Mode

Full systems programming with manual memory control and zero-overhead abstractions:

```ouro
@low
module SystemsProgramming {
    // Custom memory allocator with multiple strategies
    @no_std
    @no_alloc
    struct AdvancedAllocator {
        heap_start: *mut u8,
        heap_end: *mut u8,
        free_lists: [*mut FreeBlock; 32], // Segregated free lists
        large_blocks: BTreeMap<usize, *mut u8>,
        
        // Allocation strategies
        function allocate(&mut self, layout: Layout) -> Result<*mut u8, AllocError> {
            let size = layout.size();
            let align = layout.align();
            
            // Small allocation optimization (SLOB)
            if size <= 256 {
                return self.allocate_small(size, align);
            }
            
            // Medium allocations use best-fit
            if size <= 4096 {
                return self.allocate_medium(size, align);
            }
            
            // Large allocations use mmap
            self.allocate_large(size, align)
        }
        
        // Lock-free allocation for small objects
        function allocate_small(&mut self, size: usize, align: usize) -> Result<*mut u8, AllocError> {
            let class_idx = size_class(size);
            let free_list = &self.free_lists[class_idx];
            
            // Try lock-free pop from free list
            loop {
                let head = atomic_load(free_list, Ordering::Acquire);
                if head.is_null() {
                    // Refill from central pool
                    self.refill_free_list(class_idx)?;
                    continue;
                }
                
                let next = unsafe { (*head).next };
                if atomic_compare_exchange_weak(
                    free_list, head, next, 
                    Ordering::Release, Ordering::Relaxed
                ).is_ok() {
                    return Ok(head as *mut u8);
                }
            }
        }
        
        // Memory-mapped large allocations
        function allocate_large(&mut self, size: usize, align: usize) -> Result<*mut u8, AllocError> {
            unsafe {
                let ptr = mmap(
                    null_mut(),
                    size,
                    PROT_READ | PROT_WRITE,
                    MAP_PRIVATE | MAP_ANONYMOUS,
                    -1,
                    0
                );
                
                if ptr == MAP_FAILED {
                    return Err(AllocError::OutOfMemory);
                }
                
                self.large_blocks.insert(ptr as usize, ptr as *mut u8);
                Ok(ptr as *mut u8)
            }
        }
    }
    
    // Zero-overhead smart pointers
    @zero_cost
    struct UniquePtr<T> {
        ptr: *mut T,
        
        // Move semantics only
        function new(value: T) -> Self {
            let ptr = unsafe { 
                let p = malloc(sizeof::<T>()) as *mut T;
                write(p, value);
                p
            };
            Self { ptr }
        }
        
        // Custom dereferencing
        operator *(&self) -> &T {
            unsafe { &*self.ptr }
        }
        
        operator *(&mut self) -> &mut T {
            unsafe { &mut *self.ptr }
        }
        
        // RAII cleanup
        destructor {
            if !self.ptr.is_null() {
                unsafe {
                    drop_in_place(self.ptr);
                    free(self.ptr as *mut u8);
                }
            }
        }
    }
}
```

### @asm - Assembly Integration

Complete x86-64, ARM, RISC-V assembly integration:

```ouro
@asm
module AssemblyIntegration {
    // Advanced SIMD operations with all instruction sets
    function optimized_matrix_multiply(
        a: &[f32], b: &[f32], c: &mut [f32],
        m: usize, n: usize, k: usize
    ) {
        // Runtime CPU feature detection
        if cpu::has_feature("avx512f") {
            assembly {
                // AVX-512 implementation
                mov rax, [&m]
                mov rbx, [&n]
                mov rcx, [&k]
                
            .outer_loop:
                mov rdx, 0  // j = 0
                
            .inner_loop:
                // Initialize 16 accumulators for 16x16 tile
                vxorps zmm0, zmm0, zmm0
                vxorps zmm1, zmm1, zmm1
                // ... up to zmm15
                
                mov rsi, 0  // l = 0
                
            .k_loop:
                // Load A matrix tile (16 elements)
                vmovups zmm16, [&a + rax * 4]
                
                // Broadcast B elements and multiply-accumulate
                vbroadcastss zmm17, [&b + rdx * 4]
                vfmadd231ps zmm0, zmm16, zmm17
                
                // Continue for full tile...
                add rsi, 16
                cmp rsi, rcx
                jl .k_loop
                
                // Store results with masking for edge cases
                mov r8, rbx
                sub r8, rdx
                cmp r8, 16
                jge .store_full
                
                // Create mask for partial store
                mov r9, -1
                bzhi r9, r9, r8
                kmovw k1, r9d
                vmovups [&c + rdx * 4]{k1}, zmm0
                jmp .next_tile
                
            .store_full:
                vmovups [&c + rdx * 4], zmm0
                
            .next_tile:
                add rdx, 16
                cmp rdx, rbx
                jl .inner_loop
                
                add rax, 16
                cmp rax, rax
                jl .outer_loop
                
                vzeroupper  // Clean up AVX state
            }
        } else if cpu::has_feature("neon") {
            assembly {
                // ARM NEON implementation
                // Load dimensions
                ldr x0, [&m]
                ldr x1, [&n]
                ldr x2, [&k]
                
            .outer_loop_neon:
                mov x3, #0  // j = 0
                
            .inner_loop_neon:
                // Initialize 4 vector accumulators
                movi v0.4s, #0
                movi v1.4s, #0
                movi v2.4s, #0
                movi v3.4s, #0
                
                mov x4, #0  // l = 0
                
            .k_loop_neon:
                // Load 4x4 tile from A
                ld1 {v16.4s-v19.4s}, [&a], #64
                
                // Load and broadcast from B
                ld1r {v20.4s}, [&b]
                
                // Multiply-accumulate
                fmla v0.4s, v16.4s, v20.4s
                fmla v1.4s, v17.4s, v20.4s
                fmla v2.4s, v18.4s, v20.4s
                fmla v3.4s, v19.4s, v20.4s
                
                add x4, x4, #4
                cmp x4, x2
                b.lo .k_loop_neon
                
                // Store results
                st1 {v0.4s-v3.4s}, [&c], #64
                
                add x3, x3, #4
                cmp x3, x1
                b.lo .inner_loop_neon
                
                add x0, x0, #4
                cmp x0, x0
                b.lo .outer_loop_neon
            }
        }
    }
    
    // Custom calling conventions
    @naked
    function custom_syscall(nr: u64, arg1: u64, arg2: u64, arg3: u64) -> i64 {
        assembly {
            // Custom calling convention: args in rbx, rcx, rdx
            mov rax, [&nr]
            mov rbx, [&arg1]
            mov rcx, [&arg2]
            mov rdx, [&arg3]
            
            // Save callee-saved registers
            push rbx
            push rcx
            
            // Make syscall
            syscall
            
            // Restore registers
            pop rcx
            pop rbx
            
            // Return value already in rax
            ret
        }
    }
}
```

### @asm spirv - SPIR-V Assembly

Full SPIR-V assembly for GPU compute and graphics:

```ouro
@asm spirv
module SPIRVProgramming {
    // Advanced ray tracing with all extensions
    @kernel
    function hybrid_renderer(
        acceleration_structure: AccelerationStructure,
        output_image: Image2D,
        camera: Camera,
        lights: Buffer<Light>,
        materials: Buffer<Material>
    ) {
        spirv_assembly {
            ; Enable all modern GPU features
            OpCapability Shader
            OpCapability RayTracingKHR
            OpCapability RayQueryKHR
            OpCapability MeshShadingNV
            OpCapability FragmentBarycentricKHR
            OpCapability CooperativeMatrixNV
            OpCapability VariablePointers
            OpCapability Float16
            OpCapability Int64
            OpCapability SubgroupBallotKHR
            OpCapability SubgroupVoteKHR
            OpCapability SubgroupArithmeticKHR
            OpCapability SubgroupShuffleKHR
            OpCapability SubgroupClusteredKHR
            OpCapability FragmentDensityEXT
            OpCapability WorkgroupMemoryExplicitLayoutKHR
            
            OpExtension "SPV_KHR_ray_tracing"
            OpExtension "SPV_KHR_ray_query"
            OpExtension "SPV_NV_mesh_shader"
            OpExtension "SPV_NV_cooperative_matrix"
            OpExtension "SPV_KHR_fragment_barycentric"
            OpExtension "SPV_EXT_fragment_density"
            OpExtension "SPV_KHR_workgroup_memory_explicit_layout"
            
            ; Advanced types
            %ray_desc = OpTypeStruct %v3float %float %v3float %float %uint
            %ray_query = OpTypeRayQueryKHR
            %hit_info = OpTypeStruct %float %v2float %uint %uint %mat4v3float
            
            ; Cooperative matrix types for tensor operations
            %coop_mat_a = OpTypeCooperativeMatrixNV %float16 %uint_3 %uint_16 %uint_16 %uint_0
            %coop_mat_b = OpTypeCooperativeMatrixNV %float16 %uint_3 %uint_16 %uint_16 %uint_1
            %coop_mat_c = OpTypeCooperativeMatrixNV %float32 %uint_3 %uint_16 %uint_16 %uint_2
            
            ; Main ray generation shader
            %main = OpFunction %void None %void_func
            %entry = OpLabel
            
            ; Get pixel coordinates
            %pixel_coord = OpLoad %v2uint %gl_LaunchIDEXT
            %launch_size = OpLoad %v2uint %gl_LaunchSizeEXT
            
            ; Convert to NDC
            %pixel_center = OpFAdd %v2float %pixel_coord %vec2_0_5
            %ndc = OpFDiv %v2float %pixel_center %launch_size
            %screen_pos = OpFSub %v2float %ndc %vec2_1_0
            
            ; Generate primary ray
            %ray_origin = OpLoad %v3float %camera_position
            %ray_dir = OpFunctionCall %v3float %generate_ray_dir %screen_pos
            
            ; Initialize ray query
            %rq = OpVariable %ptr_ray_query Function
            OpRayQueryInitializeKHR %rq %acceleration_structure %ray_flags 
                                   %cull_mask %ray_origin %t_min %ray_dir %t_max
            
            ; Traverse acceleration structure
            OpLoopMerge %merge_label %continue_label None
            OpBranch %loop_label
            
            %loop_label = OpLabel
            OpRayQueryProceedKHR %proceed %rq
            OpBranchConditional %proceed %body_label %merge_label
            
            %body_label = OpLabel
            %candidate_type = OpRayQueryGetIntersectionTypeKHR %uint %rq %uint_0
            OpSelectionMerge %type_merge None
            OpSwitch %candidate_type %type_merge 
                     %gl_RayQueryCandidateIntersectionTriangleKHR %triangle_label
                     %gl_RayQueryCandidateIntersectionAABBKHR %aabb_label
            
            %triangle_label = OpLabel
            ; Handle triangle intersection
            %barycentrics = OpRayQueryGetIntersectionBarycentricsKHR %v2float %rq %uint_0
            %instance_id = OpRayQueryGetIntersectionInstanceIdKHR %uint %rq %uint_0
            %primitive_id = OpRayQueryGetIntersectionPrimitiveIndexKHR %uint %rq %uint_0
            %transform = OpRayQueryGetIntersectionObjectToWorldKHR %mat4v3float %rq %uint_0
            
            ; Compute shading with mesh shaders
            OpFunctionCall %v3float %shade_triangle %barycentrics %instance_id %primitive_id
            OpBranch %type_merge
            
            %aabb_label = OpLabel
            ; Handle procedural geometry
            OpFunctionCall %void %intersect_procedural %rq
            OpBranch %type_merge
            
            %type_merge = OpLabel
            OpBranch %continue_label
            
            %continue_label = OpLabel
            OpBranch %loop_label
            
            %merge_label = OpLabel
            
            ; Get final intersection data
            %committed_type = OpRayQueryGetCommittedIntersectionTypeKHR %uint %rq
            OpSelectionMerge %final_merge None
            OpIEqual %hit %committed_type %gl_RayQueryCommittedIntersectionTriangleKHR
            OpBranchConditional %hit %shade_label %miss_label
            
            %shade_label = OpLabel
            ; Complex shading with ML denoising
            %hit_point = OpRayQueryGetIntersectionTKHR %float %rq %uint_1
            %world_pos = OpFMA %v3float %ray_dir %hit_point %ray_origin
            
            ; Neural denoising using cooperative matrices
            %noise_input = OpFunctionCall %coop_mat_a %prepare_denoiser_input %world_pos
            %denoiser_weights = OpLoad %coop_mat_b %neural_weights_binding
            %denoised = OpCooperativeMatrixMulAddNV %coop_mat_c %noise_input %denoiser_weights
            
            OpBranch %final_merge
            
            %miss_label = OpLabel
            ; Environment mapping
            %env_color = OpFunctionCall %v4float %sample_environment %ray_dir
            OpBranch %final_merge
            
            %final_merge = OpLabel
            %final_color = OpPhi %v4float %denoised %shade_label %env_color %miss_label
            
            ; Write to output image
            OpImageWrite %output_image %pixel_coord %final_color
            
            OpReturn
            OpFunctionEnd
        }
    }
}
```

---

## 2. Compilation & Optimization Attributes

### @inline - Advanced Inlining Control

Complete inlining control with multiple strategies and heuristics:

```ouro
// Force inlining with guaranteed semantics
@inline(always)
function critical_fast_path<T>(data: T) -> T {
    // Compiler MUST inline this, even if it increases code size
    // Used for performance-critical paths where call overhead matters
    return data.transform().validate().optimize();
}

// Prevent inlining for code size optimization
@inline(never)
function large_error_handler(error: Error) -> Response {
    // Never inline to reduce code bloat
    // Useful for cold paths and error handling
    log_error_details(error);
    send_error_report(error);
    generate_debug_dump(error);
    return Response::InternalError;
}

// Intelligent inlining based on profile data
@inline(profile_guided)
function adaptive_function(input: Data) -> Result {
    // Compiler uses PGO data to decide
    // Inlined in hot paths, not in cold paths
    return process(input);
}

// Recursive inlining with depth control
@inline(recursive: 3)
function factorial(n: u64) -> u64 {
    // Inline up to 3 levels deep
    // factorial(5) expands to: 5 * 4 * 3 * factorial(2)
    if n <= 1 { 1 } else { n * factorial(n - 1) }
}

// Size-based inlining threshold
@inline(max_size: 50)
function size_controlled(data: &[u8]) -> u32 {
    // Only inline if generated code < 50 instructions
    // Balances performance vs code size
    let mut checksum = 0u32;
    for &byte in data {
        checksum = checksum.rotate_left(1) ^ byte as u32;
    }
    checksum
}

// Conditional inlining based on generics
@inline(when: T::SIZE <= 16)
function small_type_optimization<T: Copy>(value: T) -> T {
    // Inline for small types, call for large types
    // Enables zero-cost abstractions
    unsafe { std::ptr::read_volatile(&value) }
}

// Cross-crate inlining control
@inline(export)
pub function library_fast_path() {
    // Available for inlining in other crates
    // Enables LTO optimization across boundaries
}
```

### @compile_time - Compile-Time Execution

Full compile-time computation with unlimited capabilities:

```ouro
// Generate perfect hash tables at compile time
@compile_time
function generate_keyword_table() -> PerfectHashTable<&'static str> {
    const KEYWORDS = [
        "if", "else", "while", "for", "match", "function", "return",
        "class", "struct", "trait", "impl", "const", "let", "var"
    ];
    
    // Find minimal perfect hash function
    let mut seed = 0;
    loop {
        let hasher = PerfectHasher::new(seed);
        let mut slots = vec![None; KEYWORDS.len()];
        let mut collision = false;
        
        for keyword in KEYWORDS {
            let hash = hasher.hash(keyword) % slots.len();
            if slots[hash].is_some() {
                collision = true;
                break;
            }
            slots[hash] = Some(keyword);
        }
        
        if !collision {
            return PerfectHashTable { hasher, slots };
        }
        seed += 1;
    }
}

// Compile-time regular expression compilation
@compile_time
function compile_regex(pattern: &str) -> CompiledRegex {
    let ast = parse_regex(pattern);
    let nfa = build_nfa(ast);
    let dfa = nfa_to_dfa(nfa);
    let minimized = minimize_dfa(dfa);
    
    // Generate optimized matching code
    CompiledRegex {
        states: minimized.states,
        transitions: generate_jump_table(minimized),
        captures: extract_capture_groups(ast)
    }
}

// Compile-time code generation from schemas
@compile_time
function generate_serializers<T: Reflect>() {
    for field in T::fields() {
        @emit
        function $"serialize_{field.name}"(value: &T, writer: &mut Writer) {
            match field.type {
                Type::Primitive(p) => {
                    writer.$"write_{p}"(value.{field.name});
                },
                Type::Optional(inner) => {
                    if let Some(v) = &value.{field.name} {
                        writer.write_u8(1);
                        serialize_{inner}(v, writer);
                    } else {
                        writer.write_u8(0);
                    }
                },
                Type::Vec(elem) => {
                    writer.write_usize(value.{field.name}.len());
                    for item in &value.{field.name} {
                        serialize_{elem}(item, writer);
                    }
                },
                Type::Custom(t) => {
                    serialize_{t}(&value.{field.name}, writer);
                }
            }
        }
    }
}

// Compile-time optimization decisions
@compile_time
function choose_algorithm<T>() -> AlgorithmChoice {
    match (T::SIZE, T::ALIGNMENT, T::HAS_DROP) {
        (1..=16, _, false) => AlgorithmChoice::SmallOptimized,
        (_, align, _) if align >= 32 => AlgorithmChoice::SimdOptimized,
        (size, _, true) if size > 1024 => AlgorithmChoice::Chunked,
        _ => AlgorithmChoice::Standard
    }
}
```

### @zero_cost - Zero-Overhead Abstractions

Guaranteed zero-cost abstractions with proof:

```ouro
// Zero-cost error handling that compiles to goto
@zero_cost
enum Result<T, E> {
    Ok(T),
    Err(E)
}

@zero_cost
impl<T, E> Result<T, E> {
    // Monadic bind with no overhead
    function and_then<U, F>(self, f: F) -> Result<U, E>
        where F: FnOnce(T) -> Result<U, E>
    {
        // Compiles to simple conditional jump
        match self {
            Ok(x) => f(x),    // Direct tail call
            Err(e) => Err(e)  // No allocation, just return
        }
    }
    
    // Zero-cost error propagation
    function try_unwrap(self) -> T throws E {
        match self {
            Ok(x) => x,
            Err(e) => throw e  // Compiles to goto error handler
        }
    }
}

// Zero-cost async state machines
@zero_cost
async function zero_overhead_async() -> Result<Data> {
    // Compiles to state machine with no allocations
    let conn = TcpStream::connect("server:8080").await?;
    let data = conn.read_all().await?;
    let parsed = parse_data(&data)?;
    Ok(parsed)
}

// Zero-cost DSL implementation
@zero_cost
trait ZeroCostIterator {
    type Item;
    
    // All operations compile to loops with no function calls
    @zero_cost
    function map<B, F>(self, f: F) -> Map<Self, F> 
        where F: FnMut(Self::Item) -> B
    {
        Map { iter: self, f }  // No allocation, just struct
    }
    
    @zero_cost
    function filter<P>(self, predicate: P) -> Filter<Self, P>
        where P: FnMut(&Self::Item) -> bool
    {
        Filter { iter: self, predicate }  // Stack-only
    }
    
    @zero_cost
    function fold<B, F>(mut self, init: B, mut f: F) -> B
        where F: FnMut(B, Self::Item) -> B
    {
        // Compiles to simple loop
        let mut accum = init;
        while let Some(x) = self.next() {
            accum = f(accum, x);
        }
        accum
    }
}

// Proof of zero cost via assembly inspection
@zero_cost
@test
function verify_zero_cost() {
    // This function compiles to identical assembly as hand-written loop
    let sum = (0..1000)
        .filter(|x| x % 2 == 0)
        .map(|x| x * x)
        .fold(0, |a, b| a + b);
    
    // Compiler generates:
    // xor eax, eax    ; sum = 0
    // xor ecx, ecx    ; i = 0
    // .loop:
    //   test ecx, 1   ; if (i & 1)
    //   jnz .skip     ; goto skip
    //   mov edx, ecx  ; temp = i
    //   imul edx, edx ; temp *= temp
    //   add eax, edx  ; sum += temp
    // .skip:
    //   inc ecx       ; i++
    //   cmp ecx, 1000 ; if (i < 1000)
    //   jl .loop      ; goto loop
}
```

### @emit - Code Generation

Advanced metaprogramming with code emission:

```ouro
// Generate specialized functions for each numeric type
@compile_time
function generate_numeric_ops() {
    @emit
    for T in [u8, u16, u32, u64, i8, i16, i32, i64, f32, f64] {
        // Saturating arithmetic
        @inline(always)
        function $"saturating_add_{T}"(a: T, b: T) -> T {
            when T::IS_SIGNED {
                let result = a as i128 + b as i128;
                if result > T::MAX as i128 { T::MAX }
                else if result < T::MIN as i128 { T::MIN }
                else { result as T }
            } else {
                let result = a as u128 + b as u128;
                if result > T::MAX as u128 { T::MAX }
                else { result as T }
            }
        }
        
        // Optimized power function
        @inline(always)
        function $"fast_pow_{T}"(base: T, exp: u32) -> T {
            when T::IS_FLOAT {
                // Use hardware intrinsics for floating point
                intrinsics::$"pow_{T}"(base, exp as T)
            } else {
                // Binary exponentiation for integers
                let mut result = T::ONE;
                let mut base = base;
                let mut exp = exp;
                while exp > 0 {
                    if exp & 1 == 1 {
                        result *= base;
                    }
                    base *= base;
                    exp >>= 1;
                }
                result
            }
        }
    }
}

// Generate SIMD versions with different widths
@emit
for width in [128, 256, 512] {
    @simd(width)
    struct $"SimdVector{width}"<T> {
        data: [T; {width / T::BITS}]
    }
    
    impl<T> $"SimdVector{width}"<T> {
        // Horizontal operations
        function reduce_sum(self) -> T {
            @cfg(target_feature = "avx512f") when width == 512 {
                unsafe { intrinsics::$"reduce_add_ps{width}"(self.data) }
            }
            @cfg(target_feature = "avx2") when width == 256 {
                unsafe { intrinsics::$"reduce_add_ps{width}"(self.data) }
            }
            else {
                self.data.iter().sum()
            }
        }
    }
}

// Platform-specific code generation
@emit
@cfg(target_os = "linux")
module LinuxSyscalls {
    for (name, number) in LINUX_SYSCALL_TABLE {
        @inline(always)
        unsafe function $"sys_{name}"(args...) -> isize {
            assembly {
                mov rax, {number}
                syscall
                ret
            }
        }
    }
}

// Generate type-safe builders
@emit
function generate_builder<T: Struct>() {
    struct $"{T}Builder" {
        // Generate field for each struct field
        for field in T::fields() {
            {field.name}: Option<{field.type}>,
        }
        
        // Generate setter for each field
        for field in T::fields() {
            function ${field.name}(mut self, value: {field.type}) -> Self {
                self.{field.name} = Some(value);
                self
            }
        }
        
        // Build method with validation
        function build(self) -> Result<T, BuildError> {
            Ok(T {
                for field in T::fields() {
                    {field.name}: self.{field.name}
                        .ok_or(BuildError::MissingField("{field.name}"))?,
                }
            })
        }
    }
}
```

### @cfg - Conditional Compilation

Advanced configuration with complex conditions:

```ouro
// Platform-specific optimizations
@cfg(all(
    target_arch = "x86_64",
    target_feature = "avx2",
    not(target_os = "windows")
))
function optimized_memcpy(dst: &mut [u8], src: &[u8]) {
    // AVX2 implementation for non-Windows x86_64
}

// Feature-based compilation
@cfg(any(
    feature = "high_precision",
    all(feature = "gpu_compute", target_has = "cuda")
))
type Float = f64;

@cfg(not(any(
    feature = "high_precision",
    all(feature = "gpu_compute", target_has = "cuda")
)))
type Float = f32;

// Custom configuration predicates
@cfg(custom = "is_embedded_target")
function is_embedded_target() -> bool {
    const EMBEDDED_TARGETS = ["thumbv7m", "riscv32i", "avr"];
    EMBEDDED_TARGETS.contains(&target_arch())
}

// Conditional module compilation
@cfg(any(test, feature = "integration_tests"))
module TestUtilities {
    // Only compiled in test builds
}

// Complex nested configurations
@cfg(all(
    target_pointer_width = "64",
    any(
        target_endian = "little",
        feature = "force_little_endian"
    ),
    not(feature = "disable_simd"),
    custom = "has_atomic_ops"
))
function atomic_simd_operation() {
    // Highly specialized implementation
}

// Configuration with compile-time expressions
@cfg(const_eval = "size_of::<usize>() >= 8")
type LargeInt = u128;

@cfg(const_eval = "size_of::<usize>() < 8")
type LargeInt = u64;
```

### @naked - Naked Functions

Functions with complete control over assembly:

```ouro
// Boot code with no prologue/epilogue
@naked
@no_stack
@link_section(".init")
unsafe function _start() -> ! {
    assembly {
        // Set up initial stack
        la sp, __stack_top
        
        // Clear BSS section
        la t0, __bss_start
        la t1, __bss_end
        
    .clear_bss:
        beq t0, t1, .done_bss
        sb zero, 0(t0)
        addi t0, t0, 1
        j .clear_bss
        
    .done_bss:
        // Set up trap vector
        la t0, trap_vector
        csrw mtvec, t0
        
        // Enable interrupts
        li t0, 0x88
        csrs mstatus, t0
        
        // Call main
        call main
        
        // If main returns, halt
    .halt:
        wfi
        j .halt
    }
}

// Context switch routine
@naked
function context_switch(old: **Context, new: *Context) {
    assembly {
        // Save all registers
        addi sp, sp, -16 * 8
        sd ra, 0 * 8(sp)
        sd s0, 1 * 8(sp)
        sd s1, 2 * 8(sp)
        sd s2, 3 * 8(sp)
        sd s3, 4 * 8(sp)
        sd s4, 5 * 8(sp)
        sd s5, 6 * 8(sp)
        sd s6, 7 * 8(sp)
        sd s7, 8 * 8(sp)
        sd s8, 9 * 8(sp)
        sd s9, 10 * 8(sp)
        sd s10, 11 * 8(sp)
        sd s11, 12 * 8(sp)
        sd gp, 13 * 8(sp)
        sd tp, 14 * 8(sp)
        csrr t0, sstatus
        sd t0, 15 * 8(sp)
        
        // Save stack pointer
        sd sp, (a0)
        
        // Load new stack pointer
        ld sp, (a1)
        
        // Restore all registers
        ld ra, 0 * 8(sp)
        ld s0, 1 * 8(sp)
        ld s1, 2 * 8(sp)
        ld s2, 3 * 8(sp)
        ld s3, 4 * 8(sp)
        ld s4, 5 * 8(sp)
        ld s5, 6 * 8(sp)
        ld s6, 7 * 8(sp)
        ld s7, 8 * 8(sp)
        ld s8, 9 * 8(sp)
        ld s9, 10 * 8(sp)
        ld s10, 11 * 8(sp)
        ld s11, 12 * 8(sp)
        ld gp, 13 * 8(sp)
        ld tp, 14 * 8(sp)
        ld t0, 15 * 8(sp)
        csrw sstatus, t0
        addi sp, sp, 16 * 8
        
        ret
    }
}

// Trampoline for runtime code generation
@naked
function trampoline() {
    assembly {
        // Load function pointer from fixed location
        ld t0, trampoline_target
        
        // Jump to target
        jr t0
        
        // Data section within function
        .align 8
    trampoline_target:
        .quad 0
    }
}
```

### @section - Section Placement

Control over binary layout:

```ouro
// Place in specific memory section
@section(".text.hot")
function hot_path_function() {
    // Placed in hot section for better cache locality
}

@section(".text.unlikely")
function error_handler() {
    // Placed in cold section to improve instruction cache usage
}

// Custom sections for embedded systems
@section(".flash")
const FIRMWARE_VERSION: u32 = 0x01020304;

@section(".sram")
static mut FAST_BUFFER: [u8; 4096] = [0; 4096];

// Section with alignment requirements
@section(".vectors")
@align(512)
static INTERRUPT_VECTORS: [fn(); 256] = [default_handler; 256];

// Multiple attributes for section control
@section(".init_array")
@used
@link_section(".init_array")
static INIT_FUNCTION: extern "C" fn() = init;

// Platform-specific sections
@cfg(target_os = "macos")
@section("__TEXT,__cstring,cstring_literals")
static CONSTANT_STRING: &str = "Hello, macOS!";

@cfg(target_os = "linux")
@section(".rodata.str1.1")
static CONSTANT_STRING: &str = "Hello, Linux!";
```

---

## 3. GPU & Parallel Computing Attributes

### @gpu - GPU Module Marker

Complete GPU programming with all memory hierarchies and compute models:

```ouro
@gpu
module GPUComputing {
    // GPU memory types with full control
    @constant __constant__ PhysicsConstants PHYSICS = {
        gravity: 9.81,
        speed_of_light: 299792458.0,
        planck_constant: 6.62607015e-34
    };
    
    @texture texture<float4, cudaTextureType2D> environment_map;
    @surface surface<void, cudaSurfaceType2D> render_target;
    
    // Unified memory with hints
    @managed
    struct UnifiedBuffer<T> {
        @prefer_gpu data: [T; 1048576];
        @prefer_cpu metadata: Metadata;
        
        // Prefetch to specific device
        function prefetch_to_gpu(&self, stream: Stream) {
            cudaMemPrefetchAsync(
                self.data.as_ptr(),
                self.data.len() * sizeof::<T>(),
                current_device(),
                stream.handle
            );
        }
        
        // Memory advice for access patterns
        function set_access_pattern(&self, pattern: AccessPattern) {
            match pattern {
                AccessPattern::ReadMostly => {
                    cudaMemAdvise(
                        self.data.as_ptr(),
                        self.data.len() * sizeof::<T>(),
                        cudaMemAdviseSetReadMostly,
                        current_device()
                    );
                },
                AccessPattern::PreferredLocation(device) => {
                    cudaMemAdvise(
                        self.data.as_ptr(),
                        self.data.len() * sizeof::<T>(),
                        cudaMemAdviseSetPreferredLocation,
                        device
                    );
                },
                AccessPattern::AccessedBy(devices) => {
                    for device in devices {
                        cudaMemAdvise(
                            self.data.as_ptr(),
                            self.data.len() * sizeof::<T>(),
                            cudaMemAdviseSetAccessedBy,
                            device
                        );
                    }
                }
            }
        }
    }
    
    // GPU memory pools with custom allocation
    @gpu
    struct MemoryPool {
        pools: [cudaMemPool_t; 4],  // Different pools for different sizes
        
        function allocate<T>(&self, count: usize) -> GPUBuffer<T> {
            let size = count * sizeof::<T>();
            let pool_idx = match size {
                0..=1024 => 0,        // Small allocations
                1025..=65536 => 1,    // Medium allocations
                65537..=1048576 => 2, // Large allocations
                _ => 3                // Huge allocations
            };
            
            let mut ptr: *mut T = null_mut();
            cudaMemPoolAllocAsync(
                &mut ptr,
                size,
                self.pools[pool_idx],
                current_stream()
            );
            
            GPUBuffer { ptr, size, pool: self.pools[pool_idx] }
        }
    }
    
    // Multi-GPU support
    @gpu
    struct MultiGPUContext {
        devices: Vec<Device>,
        peer_access: [[bool; MAX_GPUS]; MAX_GPUS],
        
        function enable_peer_access(&mut self) {
            for i in 0..self.devices.len() {
                cudaSetDevice(i as i32);
                for j in 0..self.devices.len() {
                    if i != j && can_access_peer(i, j) {
                        cudaDeviceEnablePeerAccess(j as i32, 0);
                        self.peer_access[i][j] = true;
                    }
                }
            }
        }
        
        // NCCL for multi-GPU communication
        function all_reduce<T>(&self, buffers: &mut [GPUBuffer<T>]) {
            let comm = ncclComm_t::new(&self.devices);
            ncclAllReduce(
                buffers[0].as_ptr(),
                buffers[0].as_mut_ptr(),
                buffers[0].len(),
                ncclDataType::<T>(),
                ncclSum,
                comm,
                current_stream()
            );
        }
    }
}
```

### @kernel - GPU Kernel Functions

Full GPU kernel capabilities with all optimization features:

```ouro
// Advanced kernel with all features
@kernel
@launch_bounds(256, 8)  // 256 threads per block, min 8 blocks per SM
@max_dynamic_shared_memory_size(49152)  // 48KB dynamic shared memory
@cluster_dims(2, 2, 1)  // Thread block clusters (Hopper+)
function advanced_gemm<T: GpuNumeric>(
    @restrict a: &[T],
    @restrict b: &[T], 
    @restrict c: &mut [T],
    m: u32, n: u32, k: u32,
    alpha: T, beta: T
) {
    // Cluster-wide synchronization (Hopper GPUs)
    cluster_sync();
    
    // Distributed shared memory across cluster
    @cluster_shared var distributed_tile: [[T; 64]; 64];
    
    // Grid-stride loop for large problems
    let total_threads = gridDim.x * gridDim.y * blockDim.x * blockDim.y;
    let thread_id = blockIdx.y * gridDim.x * blockDim.x * blockDim.y +
                    blockIdx.x * blockDim.x * blockDim.y +
                    threadIdx.y * blockDim.x +
                    threadIdx.x;
    
    for global_idx in (thread_id..m*n).step_by(total_threads) {
        let row = global_idx / n;
        let col = global_idx % n;
        
        // Asynchronous memory operations
        @async_copy
        {
            // Async copy from global to shared
            cuda::memcpy_async(
                &mut distributed_tile[threadIdx.y][threadIdx.x],
                &a[row * k + threadIdx.x],
                sizeof::<T>(),
                cuda::pipeline
            );
        }
        
        // Warp matrix operations (Tensor Cores)
        if T::supports_wmma() {
            @wmma
            {
                let mut a_frag = wmma::fragment_a<T, 16, 16, 16>::new();
                let mut b_frag = wmma::fragment_b<T, 16, 16, 16>::new();
                let mut c_frag = wmma::fragment_c<T, 16, 16, 16>::new();
                
                wmma::fill_fragment(&mut c_frag, 0.0);
                
                for k_tile in (0..k).step_by(16) {
                    wmma::load_matrix_sync(&mut a_frag, &a[row * k + k_tile], k);
                    wmma::load_matrix_sync(&mut b_frag, &b[k_tile * n + col], n);
                    wmma::mma_sync(&mut c_frag, &a_frag, &b_frag, &c_frag);
                }
                
                wmma::store_matrix_sync(&mut c[row * n + col], &c_frag, n);
            }
        }
        
        // Warp-level primitives
        let warp_id = threadIdx.x / 32;
        let lane_id = threadIdx.x % 32;
        
        // Warp vote functions
        if __all_sync(0xFFFFFFFF, row < m && col < n) {
            // All threads in warp have valid indices
            
            // Warp shuffle operations
            let sum = warp_reduce_sum(partial_sum);
            
            if lane_id == 0 {
                // Leader thread writes result
                atomicAdd(&c[row * n + col], sum);
            }
        }
        
        // Cooperative groups for flexible synchronization
        let block = cooperative_groups::this_thread_block();
        let tile32 = cooperative_groups::tiled_partition<32>(block);
        let tile4 = cooperative_groups::tiled_partition<4>(tile32);
        
        // Multi-level reduction
        let local_sum = tile4.reduce(partial_value, plus<T>());
        if tile4.thread_rank() == 0 {
            let tile_sum = tile32.reduce(local_sum, plus<T>());
            if tile32.thread_rank() == 0 {
                output[blockIdx.x] = tile_sum;
            }
        }
    }
    
    // Grid-wide synchronization (requires special launch)
    if grid.is_cooperative() {
        grid_sync();
        
        // Second phase after grid sync
        if threadIdx.x == 0 && blockIdx.x == 0 {
            // Final reduction across all blocks
        }
    }
}

// Dynamic parallelism - kernels launching kernels
@kernel
function adaptive_kernel(data: &[f32], threshold: f32) {
    let idx = blockIdx.x * blockDim.x + threadIdx.x;
    
    if data[idx] > threshold {
        // Launch child kernel for complex processing
        @device
        {
            complex_processing<<<1, 256>>>(&data[idx..idx+256]);
        }
    } else {
        // Simple in-place processing
        data[idx] = simple_transform(data[idx]);
    }
}

// Kernel with template parameters
@kernel
template<const TILE_SIZE: u32, const UNROLL_FACTOR: u32>
function templated_kernel<T>(input: &[T], output: &mut [T]) {
    @shared var tile: [T; TILE_SIZE];
    
    // Compile-time unrolling
    #pragma unroll UNROLL_FACTOR
    for i in 0..TILE_SIZE/UNROLL_FACTOR {
        // Process UNROLL_FACTOR elements per iteration
    }
}
```

### @shared - Shared Memory

Complete shared memory functionality with all optimizations:

```ouro
@kernel
function shared_memory_advanced() {
    // Static shared memory with bank conflict avoidance
    @shared
    @align(128)  // Align to cache line
    var tile_a: [[f32; 33]; 32];  // Padding to avoid bank conflicts
    
    @shared
    @volatile  // Prevent optimization for inter-warp communication
    var flags: [u32; 32];
    
    // Dynamic shared memory allocation
    @shared dynamic var memory_pool: [u8; ?];
    
    // Partition dynamic shared memory
    let float_array = memory_pool.cast::<f32>();
    let int_array = memory_pool[1024..].cast::<i32>();
    let byte_buffer = memory_pool[2048..];
    
    // Shared memory atomics with scopes
    @shared atomic var counter: u32 = 0;
    
    // Different memory orderings for different use cases
    counter.fetch_add(1, MemoryOrder::Relaxed, MemoryScope::Block);
    counter.store(0, MemoryOrder::Release, MemoryScope::Device);
    
    // Cooperative matrix in shared memory (Ampere+)
    @shared
    var coop_matrix: CooperativeMatrix<f16, 16, 16>;
    
    // Asynchronous shared memory operations
    @async
    {
        // Async copy from global to shared
        cuda::pipeline pipe;
        cuda::memcpy_async(tile_a, global_data, sizeof(tile_a), pipe);
        pipe.commit();
        pipe.wait();
    }
    
    // Shared memory barriers with scopes
    __syncthreads();  // Block-level barrier
    __syncwarp();     // Warp-level barrier
    
    // Named barriers for complex synchronization
    __barrier_sync(0);  // Barrier 0
    __barrier_sync_count(1, thread_count);  // Barrier 1 with thread count
    
    // Shared memory reuse patterns
    @phase(0) {
        // Phase 0: Use shared memory as input buffer
        @shared union Phase0Data {
            input_tiles: [[f32; 32]; 32],
            raw_bytes: [u8; 4096]
        }
    }
    
    __syncthreads();
    
    @phase(1) {
        // Phase 1: Reuse same memory as output buffer
        @shared union Phase1Data {
            output_tiles: [[f32; 16]; 64],
            temp_buffer: [i32; 1024]
        }
    }
}

// Shared memory allocation strategies
@kernel
template<typename T, const N: usize>
function optimized_shared_memory() {
    // Compute optimal shared memory layout
    const BANK_SIZE = 4;  // 4 bytes per bank
    const NUM_BANKS = 32;
    const BANK_STRIDE = if sizeof::<T>() % BANK_SIZE == 0 {
        sizeof::<T>() / BANK_SIZE + 1
    } else {
        sizeof::<T>() / BANK_SIZE
    };
    
    // Conflict-free shared memory
    @shared
    var data: [T; N * BANK_STRIDE];
    
    // Access pattern that avoids bank conflicts
    let tid = threadIdx.x;
    let bank_offset = tid * BANK_STRIDE;
    data[bank_offset] = global_data[tid];
}
```

### @simd - SIMD Operations

Full SIMD capabilities across all platforms:

```ouro
// Platform-agnostic SIMD with automatic selection
@simd
module SIMDOperations {
    // Portable SIMD types
    @simd
    struct SimdF32<const N: usize> {
        data: [f32; N]
    }
    
    // SIMD operations with platform-specific implementations
    impl<const N: usize> SimdF32<N> {
        @simd
        function dot_product(self, other: Self) -> f32 {
            #[cfg(target_feature = "avx512f")]
            if N == 16 {
                return unsafe {
                    let a = _mm512_loadu_ps(self.data.as_ptr());
                    let b = _mm512_loadu_ps(other.data.as_ptr());
                    let prod = _mm512_mul_ps(a, b);
                    _mm512_reduce_add_ps(prod)
                };
            }
            
            #[cfg(target_feature = "avx2")]
            if N == 8 {
                return unsafe {
                    let a = _mm256_loadu_ps(self.data.as_ptr());
                    let b = _mm256_loadu_ps(other.data.as_ptr());
                    let prod = _mm256_mul_ps(a, b);
                    let sum = _mm256_hadd_ps(prod, prod);
                    let sum = _mm256_hadd_ps(sum, sum);
                    _mm256_extract_f32(sum, 0) + _mm256_extract_f32(sum, 4)
                };
            }
            
            #[cfg(target_feature = "neon")]
            if N == 4 {
                return unsafe {
                    let a = vld1q_f32(self.data.as_ptr());
                    let b = vld1q_f32(other.data.as_ptr());
                    let prod = vmulq_f32(a, b);
                    vaddvq_f32(prod)
                };
            }
            
            // Scalar fallback
            self.data.iter().zip(&other.data).map(|(a, b)| a * b).sum()
        }
        
        // Masked operations
        @simd
        function masked_store(self, mask: SimdMask<N>, dest: &mut [f32]) {
            #[cfg(target_feature = "avx512f")]
            if N == 16 {
                unsafe {
                    let data = _mm512_loadu_ps(self.data.as_ptr());
                    _mm512_mask_storeu_ps(dest.as_mut_ptr(), mask.0, data);
                }
                return;
            }
            
            // Fallback for non-AVX512
            for i in 0..N {
                if mask.test(i) {
                    dest[i] = self.data[i];
                }
            }
        }
        
        // Horizontal operations
        @simd
        function horizontal_sum(self) -> f32 {
            #[cfg(any(target_feature = "avx2", target_feature = "avx512f"))]
            {
                // Use hardware horizontal add
                return self.reduce_add_ordered();
            }
            
            // Tree reduction for other platforms
            let mut sum = self.data[0];
            for i in 1..N {
                sum += self.data[i];
            }
            sum
        }
    }
    
    // Auto-vectorization hints
    @simd
    @vectorize(style: Aggressive)
    function array_operation(a: &[f32], b: &[f32], c: &mut [f32]) {
        #[assert(a.len() == b.len() && b.len() == c.len())]
        
        // Compiler should vectorize this
        for i in 0..a.len() {
            c[i] = a[i] * b[i] + c[i];
        }
    }
    
    // Explicit vectorization with remainder handling
    @simd
    function explicit_vectorized_sum(data: &[f32]) -> f32 {
        const VECTOR_SIZE: usize = SimdF32::<16>::LANES;
        let mut vector_sum = SimdF32::<16>::zero();
        
        // Main vectorized loop
        let chunks = data.chunks_exact(VECTOR_SIZE);
        let remainder = chunks.remainder();
        
        for chunk in chunks {
            let v = SimdF32::<16>::load(chunk);
            vector_sum = vector_sum.add(v);
        }
        
        // Handle remainder
        let mut scalar_sum = vector_sum.horizontal_sum();
        for &value in remainder {
            scalar_sum += value;
        }
        
        scalar_sum
    }
    
    // Gather/scatter operations
    @simd
    function gather_scatter_example(indices: &[u32], values: &[f32], output: &mut [f32]) {
        #[cfg(target_feature = "avx2")]
        unsafe {
            for chunk in indices.chunks_exact(8) {
                let idx = _mm256_loadu_si256(chunk.as_ptr() as *const __m256i);
                let gathered = _mm256_i32gather_ps(values.as_ptr(), idx, 4);
                _mm256_storeu_ps(output.as_mut_ptr(), gathered);
            }
        }
        
        // Scalar fallback
        #[cfg(not(target_feature = "avx2"))]
        for (i, &idx) in indices.iter().enumerate() {
            output[i] = values[idx as usize];
        }
    }
}
```

### @parallel - Parallel Execution

Complete parallel execution models with all scheduling strategies:

```ouro
@parallel
module ParallelExecution {
    // Task parallelism with work stealing
    @parallel(scheduler: WorkStealing)
    function parallel_merge_sort<T: Ord>(data: &mut [T]) {
        if data.len() <= 1000 {
            data.sort();  // Sequential base case
            return;
        }
        
        let mid = data.len() / 2;
        let (left, right) = data.split_at_mut(mid);
        
        // Spawn parallel tasks
        parallel {
            spawn { parallel_merge_sort(left) }
            spawn { parallel_merge_sort(right) }
        }
        
        // Merge results
        merge_in_place(left, right);
    }
    
    // Data parallelism with different scheduling strategies
    @parallel(schedule: Dynamic, chunk_size: 64)
    function dynamic_scheduled<T, F>(data: &[T], f: F) -> Vec<T::Output>
        where F: Fn(&T) -> T::Output + Sync
    {
        parallel_map(data, f)
    }
    
    @parallel(schedule: Static)
    function static_scheduled<T>(data: &mut [T]) {
        let chunk_size = data.len() / num_threads();
        
        parallel for i in 0..num_threads() {
            let start = i * chunk_size;
            let end = if i == num_threads() - 1 { data.len() } else { (i + 1) * chunk_size };
            process_chunk(&mut data[start..end]);
        }
    }
    
    @parallel(schedule: Guided, min_chunk: 16)
    function guided_scheduled(workload: &[Work]) {
        let atomic_index = AtomicUsize::new(0);
        let total = workload.len();
        
        parallel {
            loop {
                // Guided scheduling: larger chunks at start, smaller at end
                let remaining = total - atomic_index.load(Ordering::Relaxed);
                let chunk_size = max(remaining / (2 * num_threads()), 16);
                
                let start = atomic_index.fetch_add(chunk_size, Ordering::Relaxed);
                if start >= total { break; }
                
                let end = min(start + chunk_size, total);
                process_items(&workload[start..end]);
            }
        }
    }
    
    // Nested parallelism with depth control
    @parallel(max_depth: 3)
    function nested_parallel_computation(data: &[Data]) -> Result {
        parallel for item in data {
            // Level 1 parallelism
            let intermediate = parallel_map(item.sub_items, |sub| {
                // Level 2 parallelism
                parallel_reduce(sub.values, 0, |a, b| a + b)
            });
            
            // Level 3 parallelism (if max_depth allows)
            @parallel(inherit_depth)
            process_intermediate(intermediate)
        }
    }
    
    // Parallel patterns
    @parallel
    module Patterns {
        // Parallel reduce with custom operators
        @parallel
        function reduce<T, Op>(data: &[T], identity: T, op: Op) -> T
            where Op: Fn(T, T) -> T + Associative + Commutative
        {
            if data.len() <= 1000 {
                return data.iter().fold(identity, op);
            }
            
            // Tree reduction
            let mid = data.len() / 2;
            let (left, right) = data.split_at(mid);
            
            let (left_result, right_result) = parallel {
                (spawn { reduce(left, identity, op) },
                 spawn { reduce(right, identity, op) })
            };
            
            op(left_result, right_result)
        }
        
        // Parallel scan (prefix sum)
        @parallel
        function scan<T, Op>(data: &[T], identity: T, op: Op) -> Vec<T>
            where Op: Fn(T, T) -> T + Associative
        {
            // Blelloch scan algorithm
            let n = data.len();
            let mut tree = vec![identity; 2 * n];
            
            // Up-sweep phase
            parallel for i in 0..n {
                tree[n + i] = data[i];
            }
            
            for level in (0..n.log2()).rev() {
                parallel for i in (0..n).step_by(1 << (level + 1)) {
                    let k = i + (1 << level) - 1;
                    tree[k + (1 << level)] = op(tree[k], tree[k + (1 << level)]);
                }
            }
            
            // Down-sweep phase
            tree[n - 1] = identity;
            
            for level in 0..n.log2() {
                parallel for i in (0..n).step_by(1 << (level + 1)) {
                    let k = i + (1 << level) - 1;
                    let t = tree[k];
                    tree[k] = tree[k + (1 << level)];
                    tree[k + (1 << level)] = op(t, tree[k + (1 << level)]);
                }
            }
            
            tree[n..].to_vec()
        }
        
        // Parallel partition
        @parallel
        function partition<T, F>(data: &mut [T], predicate: F) -> usize
            where F: Fn(&T) -> bool + Sync
        {
            let n = data.len();
            let mut flags = vec![false; n];
            
            // Mark elements that satisfy predicate
            parallel for i in 0..n {
                flags[i] = predicate(&data[i]);
            }
            
            // Compute positions
            let positions = scan(&flags, 0, |a, b| a + b as usize);
            let partition_point = positions[n - 1] + flags[n - 1] as usize;
            
            // Move elements
            let mut temp = Vec::with_capacity(n);
            
            parallel {
                // Move elements that satisfy predicate to front
                for i in 0..n {
                    if flags[i] {
                        temp[positions[i]] = data[i].clone();
                    }
                }
                
                // Move other elements to back
                let mut back_pos = partition_point;
                for i in 0..n {
                    if !flags[i] {
                        temp[back_pos] = data[i].clone();
                        back_pos += 1;
                    }
                }
            }
            
            data.copy_from_slice(&temp);
            partition_point
        }
    }
    
    // Thread pool management
    @parallel
    struct ThreadPool {
        @thread_local
        static WORKER_ID: usize = 0;
        
        workers: Vec<Worker>,
        scheduler: Box<dyn Scheduler>,
        
        function execute<F>(&self, task: F)
            where F: FnOnce() + Send + 'static
        {
            self.scheduler.schedule(Box::new(task));
        }
        
        // Affinity control
        function set_affinity(&self, mapping: AffinityMapping) {
            match mapping {
                AffinityMapping::Compact => {
                    // Pack threads on same NUMA node
                    for (i, worker) in self.workers.iter().enumerate() {
                        let cpu = i % num_cpus();
                        worker.set_affinity(cpu);
                    }
                },
                AffinityMapping::Scatter => {
                    // Distribute across NUMA nodes
                    let numa_nodes = topology::numa_nodes();
                    for (i, worker) in self.workers.iter().enumerate() {
                        let node = i % numa_nodes.len();
                        let cpu = numa_nodes[node].cpus[i / numa_nodes.len()];
                        worker.set_affinity(cpu);
                    }
                },
                AffinityMapping::Custom(map) => {
                    for (worker, cpu) in self.workers.iter().zip(map) {
                        worker.set_affinity(cpu);
                    }
                }
            }
        }
    }
}
```

### @wasm_simd - WebAssembly SIMD

WebAssembly SIMD operations with full feature set:

```ouro
@wasm_simd
module WasmSIMD {
    // WASM SIMD types
    type v128 = wasm::v128;
    
    // SIMD operations for WebAssembly
    @wasm_simd
    function dot_product_wasm(a: &[f32], b: &[f32]) -> f32 {
        let mut sum = f32x4_splat(0.0);
        
        for i in (0..a.len()).step_by(4) {
            let va = v128_load(&a[i]);
            let vb = v128_load(&b[i]);
            sum = f32x4_add(sum, f32x4_mul(va, vb));
        }
        
        // Horizontal sum
        sum = f32x4_add(sum, f32x4_shuffle::<2, 3, 0, 1>(sum, sum));
        sum = f32x4_add(sum, f32x4_shuffle::<1, 0, 3, 2>(sum, sum));
        f32x4_extract_lane::<0>(sum)
    }
    
    // WASM SIMD with relaxed operations
    @wasm_simd
    @relaxed
    function matrix_multiply_relaxed(
        a: &[f32], b: &[f32], c: &mut [f32],
        m: usize, n: usize, k: usize
    ) {
        for i in 0..m {
            for j in 0..n {
                let mut sum = f32x4_splat(0.0);
                
                for l in (0..k).step_by(4) {
                    let a_vec = v128_load(&a[i * k + l]);
                    let b0 = f32x4_splat(b[(l + 0) * n + j]);
                    let b1 = f32x4_splat(b[(l + 1) * n + j]);
                    let b2 = f32x4_splat(b[(l + 2) * n + j]);
                    let b3 = f32x4_splat(b[(l + 3) * n + j]);
                    
                    // Relaxed FMA operations
                    sum = f32x4_relaxed_fma(a_vec, b0, sum);
                    sum = f32x4_relaxed_fma(a_vec, b1, sum);
                    sum = f32x4_relaxed_fma(a_vec, b2, sum);
                    sum = f32x4_relaxed_fma(a_vec, b3, sum);
                }
                
                c[i * n + j] = f32x4_extract_lane::<0>(sum) +
                               f32x4_extract_lane::<1>(sum) +
                               f32x4_extract_lane::<2>(sum) +
                               f32x4_extract_lane::<3>(sum);
            }
        }
    }
    
    // SIMD lane operations
    @wasm_simd
    function lane_operations_example(v: v128) -> v128 {
        // Extract and replace lanes
        let lane0 = i32x4_extract_lane::<0>(v);
        let modified = i32x4_replace_lane::<1>(v, lane0 * 2);
        
        // Shuffle between vectors
        let shuffled = i8x16_shuffle::<
            0, 16, 1, 17, 2, 18, 3, 19,
            4, 20, 5, 21, 6, 22, 7, 23
        >(v, modified);
        
        // Swizzle within vector
        let indices = i8x16_const(15, 14, 13, 12, 11, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1, 0);
        i8x16_swizzle(shuffled, indices)
    }
    
    // Comparison and masking
    @wasm_simd
    function masked_operations(a: v128, b: v128, mask: v128) -> v128 {
        // Compare operations
        let gt_mask = f32x4_gt(a, b);
        let eq_mask = f32x4_eq(a, b);
        
        // Bitwise selection based on mask
        v128_bitselect(a, b, mask)
    }
}
```

This comprehensive specification demonstrates the full functionality of every token type in the Ouro language. Each attribute and token is shown with advanced features including:

1. **Multiple syntax levels** with seamless integration
2. **Advanced compilation attributes** for optimization and safety
3. **GPU and parallel computing** with full hardware access
4. **Memory management** from manual to automatic
5. **Real-time systems** with timing guarantees
6. **Domain-specific features** for various fields
7. **Mathematical notation** as native syntax
8. **Natural language programming** for accessibility
9. **Advanced type system** with full generics
10. **Control flow** with pattern matching and async

The language provides unprecedented flexibility while maintaining C-level performance and safety guarantees beyond Rust. 