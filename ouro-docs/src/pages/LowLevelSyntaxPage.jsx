import CodeBlock from '../components/CodeBlock'
import Callout from '../components/Callout'

export default function LowLevelSyntaxPage() {
  return (
    <div className="max-w-4xl">
      <h1 className="text-4xl font-bold text-gray-900 dark:text-white mb-4">
        Low-Level Systems Programming Syntax
      </h1>
      <p className="text-xl text-gray-600 dark:text-gray-300 mb-8">
        Direct hardware control and memory management. Ouroboros provides low-level capabilities for systems programming, 
        performance-critical code, and hardware interfacing while maintaining safety through explicit unsafe blocks.
      </p>

      <Callout type="warning" title="Use With Caution">
        Low-level features bypass many safety guarantees. Use the <code className="bg-gray-100 dark:bg-gray-700 px-2 py-1 rounded">@low</code> 
        decorator and <code className="bg-gray-100 dark:bg-gray-700 px-2 py-1 rounded">unsafe</code> blocks when you need direct memory control.
      </Callout>

      {/* Unsafe Code */}
      <section className="mb-12">
        <h2 id="unsafe" className="text-3xl font-bold text-gray-900 dark:text-white mb-6">
          Unsafe Code Blocks
        </h2>
        <p className="text-gray-600 dark:text-gray-300 mb-4">
          Unsafe blocks allow pointer manipulation and direct memory access.
        </p>
        <CodeBlock
          code={`@low
// Basic unsafe block
unsafe
{
    int number = 42;
    int* ptr = &number;  // Get pointer to number
    *ptr = 100;          // Dereference and modify
    Console.WriteLine($"Number is now: {number}");  // 100
}

// Pointer arithmetic
unsafe
{
    int[] array = { 1, 2, 3, 4, 5 };
    fixed (int* ptr = array)  // Pin array in memory
    {
        int* current = ptr;
        for (int i = 0; i < 5; i++)
        {
            Console.WriteLine($"Value: {*current}, Address: {(long)current:X}");
            current++;  // Move to next int
        }
    }
}

// Unsafe method
public unsafe void ProcessBuffer(byte* buffer, int length)
{
    for (int i = 0; i < length; i++)
    {
        buffer[i] = (byte)(buffer[i] ^ 0xFF);  // XOR each byte
    }
}

// Unsafe structs
public unsafe struct RawData
{
    public fixed byte Data[256];  // Fixed-size buffer
    public int Length;
    
    public void Clear()
    {
        fixed (byte* ptr = Data)
        {
            for (int i = 0; i < 256; i++)
            {
                ptr[i] = 0;
            }
        }
        Length = 0;
    }
}

// Multiple pointer levels
unsafe
{
    int value = 42;
    int* ptr1 = &value;
    int** ptr2 = &ptr1;  // Pointer to pointer
    
    Console.WriteLine($"Value: {**ptr2}");
    **ptr2 = 100;  // Modify through double indirection
}

// Function pointers
unsafe
{
    delegate*<int, int, int> operation = &Add;
    int result = operation(5, 3);  // Call through function pointer
    
    static int Add(int a, int b) => a + b;
    static int Multiply(int a, int b) => a * b;
    
    // Switch operation
    operation = &Multiply;
    result = operation(5, 3);  // Now multiplies
}`}
        />
      </section>

      {/* Stack Allocation */}
      <section className="mb-12">
        <h2 id="stackalloc" className="text-3xl font-bold text-gray-900 dark:text-white mb-6">
          Stack Memory Allocation
        </h2>
        <p className="text-gray-600 dark:text-gray-300 mb-4">
          Allocate memory on the stack for better performance and automatic cleanup.
        </p>
        <CodeBlock
          code={`@low
// Basic stackalloc
unsafe
{
    byte* buffer = stackalloc byte[256];
    
    // Initialize buffer
    for (int i = 0; i < 256; i++)
    {
        buffer[i] = (byte)i;
    }
    
    // Process buffer
    ProcessData(buffer, 256);
}  // Memory automatically freed when leaving scope

// Stackalloc with span (safer alternative)
Span<int> numbers = stackalloc int[10];
for (int i = 0; i < numbers.Length; i++)
{
    numbers[i] = i * i;
}

// Large stack allocations
unsafe
{
    const int Size = 1024 * 4;  // 4KB
    byte* largeBuffer = stackalloc byte[Size];
    
    // Zero-initialize
    new Span<byte>(largeBuffer, Size).Clear();
    
    // Use for temporary operations
    ReadDataIntoBuffer(largeBuffer, Size);
    ProcessLargeData(largeBuffer, Size);
}

// Stack-allocated strings (hypothetical)
unsafe
{
    char* str = stackalloc char[50];
    int length = 0;
    
    // Build string manually
    str[length++] = 'H';
    str[length++] = 'e';
    str[length++] = 'l';
    str[length++] = 'l';
    str[length] = '\0';
    
    // Convert to managed string
    string managed = new string(str);
}

// Performance-critical buffer operations
public unsafe void FastCopy(byte* source, byte* dest, int count)
{
    // Copy 8 bytes at a time for speed
    long* src64 = (long*)source;
    long* dst64 = (long*)dest;
    
    while (count >= 8)
    {
        *dst64++ = *src64++;
        count -= 8;
    }
    
    // Copy remaining bytes
    byte* src8 = (byte*)src64;
    byte* dst8 = (byte*)dst64;
    while (count-- > 0)
    {
        *dst8++ = *src8++;
    }
}`}
        />
      </section>

      {/* Pointer Arithmetic */}
      <section className="mb-12">
        <h2 id="pointers" className="text-3xl font-bold text-gray-900 dark:text-white mb-6">
          Advanced Pointer Operations
        </h2>
        <p className="text-gray-600 dark:text-gray-300 mb-4">
          Direct memory manipulation through pointer arithmetic and casting.
        </p>
        <CodeBlock
          code={`@low
// Pointer casting
unsafe
{
    long value = 0x123456789ABCDEF0;
    byte* bytes = (byte*)&value;
    
    // Read individual bytes
    for (int i = 0; i < sizeof(long); i++)
    {
        Console.WriteLine($"Byte {i}: {bytes[i]:X2}");
    }
    
    // Modify through different pointer type
    int* ints = (int*)&value;
    ints[0] = 0xAAAAAAAA;  // Modify lower 32 bits
    ints[1] = 0xBBBBBBBB;  // Modify upper 32 bits
}

// Struct manipulation through pointers
public struct Vector3
{
    public float X, Y, Z;
}

unsafe
{
    Vector3 vec = new Vector3 { X = 1.0f, Y = 2.0f, Z = 3.0f };
    float* components = (float*)&vec;
    
    // Access as array
    for (int i = 0; i < 3; i++)
    {
        components[i] *= 2.0f;  // Double each component
    }
}

// Memory scanning
public unsafe int FindPattern(byte* haystack, int haystackLen, byte* needle, int needleLen)
{
    for (int i = 0; i <= haystackLen - needleLen; i++)
    {
        bool found = true;
        for (int j = 0; j < needleLen; j++)
        {
            if (haystack[i + j] != needle[j])
            {
                found = false;
                break;
            }
        }
        if (found) return i;
    }
    return -1;
}

// Aligned memory access
unsafe
{
    // Ensure 16-byte alignment for SIMD
    byte* unaligned = stackalloc byte[128 + 15];
    byte* aligned = (byte*)(((long)unaligned + 15) & ~15);
    
    // Now aligned can be used for SIMD operations
    float* vectors = (float*)aligned;
}

// Memory barriers and volatile
public unsafe class LockFreeQueue
{
    private struct Node
    {
        public volatile Node* Next;
        public int Data;
    }
    
    private volatile Node* _head;
    private volatile Node* _tail;
    
    public void Enqueue(int value)
    {
        Node* newNode = (Node*)Marshal.AllocHGlobal(sizeof(Node));
        newNode->Data = value;
        newNode->Next = null;
        
        // Memory barrier to ensure writes are visible
        Thread.MemoryBarrier();
        
        // Compare-and-swap operations...
    }
}`}
        />
      </section>

      {/* Bit Manipulation */}
      <section className="mb-12">
        <h2 id="bits" className="text-3xl font-bold text-gray-900 dark:text-white mb-6">
          Bit Manipulation
        </h2>
        <p className="text-gray-600 dark:text-gray-300 mb-4">
          Low-level bit operations for flags, encoding, and optimization.
        </p>
        <CodeBlock
          code={`@low
// Basic bit operations
uint flags = 0b0000_0000;

// Set bits
flags |= (1u << 3);     // Set bit 3
flags |= 0b0000_1010;   // Set multiple bits

// Clear bits
flags &= ~(1u << 3);    // Clear bit 3
flags &= ~0b0000_1010;  // Clear multiple bits

// Toggle bits
flags ^= (1u << 5);     // Toggle bit 5

// Check bits
bool isSet = (flags & (1u << 3)) != 0;
bool hasAny = (flags & 0b1111_0000) != 0;
bool hasAll = (flags & 0b1111_0000) == 0b1111_0000;

// Bit manipulation utilities
public static class BitOps
{
    // Count set bits (population count)
    public static int PopCount(uint value)
    {
        value = value - ((value >> 1) & 0x55555555);
        value = (value & 0x33333333) + ((value >> 2) & 0x33333333);
        return (int)((((value + (value >> 4)) & 0x0F0F0F0F) * 0x01010101) >> 24);
    }
    
    // Find first set bit
    public static int FindFirstSet(uint value)
    {
        if (value == 0) return -1;
        int pos = 0;
        while ((value & 1) == 0)
        {
            value >>= 1;
            pos++;
        }
        return pos;
    }
    
    // Reverse bits
    public static uint ReverseBits(uint value)
    {
        value = ((value & 0xAAAAAAAA) >> 1) | ((value & 0x55555555) << 1);
        value = ((value & 0xCCCCCCCC) >> 2) | ((value & 0x33333333) << 2);
        value = ((value & 0xF0F0F0F0) >> 4) | ((value & 0x0F0F0F0F) << 4);
        value = ((value & 0xFF00FF00) >> 8) | ((value & 0x00FF00FF) << 8);
        return (value >> 16) | (value << 16);
    }
}

// Bit fields in structs
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct PackedData
{
    private uint _data;
    
    public uint Field1
    {
        get => _data & 0x7;              // Bits 0-2
        set => _data = (_data & ~0x7u) | (value & 0x7);
    }
    
    public uint Field2
    {
        get => (_data >> 3) & 0x1F;      // Bits 3-7
        set => _data = (_data & ~(0x1Fu << 3)) | ((value & 0x1F) << 3);
    }
    
    public bool Flag
    {
        get => (_data & (1u << 8)) != 0; // Bit 8
        set => _data = value ? (_data | (1u << 8)) : (_data & ~(1u << 8));
    }
}

// Fast bit hacks
public static class BitHacks
{
    // Check if power of 2
    public static bool IsPowerOfTwo(uint n) => n != 0 && (n & (n - 1)) == 0;
    
    // Round up to next power of 2
    public static uint NextPowerOfTwo(uint n)
    {
        n--;
        n |= n >> 1;
        n |= n >> 2;
        n |= n >> 4;
        n |= n >> 8;
        n |= n >> 16;
        return n + 1;
    }
    
    // Swap without temp variable
    public static void SwapXor(ref int a, ref int b)
    {
        a ^= b;
        b ^= a;
        a ^= b;
    }
}`}
        />
      </section>

      {/* SIMD Operations */}
      <section className="mb-12">
        <h2 id="simd" className="text-3xl font-bold text-gray-900 dark:text-white mb-6">
          SIMD (Single Instruction, Multiple Data)
        </h2>
        <p className="text-gray-600 dark:text-gray-300 mb-4">
          Hardware-accelerated vector operations for maximum performance.
        </p>
        <CodeBlock
          code={`@low
using System.Numerics;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

// Basic Vector operations
public void VectorAdd(float[] a, float[] b, float[] result)
{
    int vectorSize = Vector<float>.Count;
    int i = 0;
    
    // Process vectors
    for (; i <= a.Length - vectorSize; i += vectorSize)
    {
        var va = new Vector<float>(a, i);
        var vb = new Vector<float>(b, i);
        var vr = va + vb;
        vr.CopyTo(result, i);
    }
    
    // Process remaining elements
    for (; i < a.Length; i++)
    {
        result[i] = a[i] + b[i];
    }
}

// SIMD dot product
public float DotProduct(float[] a, float[] b)
{
    if (!Vector.IsHardwareAccelerated)
        return ScalarDotProduct(a, b);
    
    var sum = Vector<float>.Zero;
    int vectorSize = Vector<float>.Count;
    int i = 0;
    
    for (; i <= a.Length - vectorSize; i += vectorSize)
    {
        var va = new Vector<float>(a, i);
        var vb = new Vector<float>(b, i);
        sum += va * vb;
    }
    
    float result = 0;
    for (int j = 0; j < vectorSize; j++)
    {
        result += sum[j];
    }
    
    // Add remaining elements
    for (; i < a.Length; i++)
    {
        result += a[i] * b[i];
    }
    
    return result;
}

// AVX2 intrinsics (x86/x64 specific)
public unsafe void ProcessImageAVX2(byte* pixels, int count)
{
    if (!Avx2.IsSupported) 
    {
        ProcessImageScalar(pixels, count);
        return;
    }
    
    int i = 0;
    for (; i <= count - 32; i += 32)
    {
        // Load 32 bytes
        var data = Avx.LoadVector256(pixels + i);
        
        // Apply brightness adjustment
        var adjusted = Avx2.Add(data, Vector256.Create((byte)10));
        
        // Ensure saturation
        adjusted = Avx2.Min(adjusted, Vector256.Create((byte)255));
        
        // Store back
        Avx.Store(pixels + i, adjusted);
    }
    
    // Handle remaining pixels
    for (; i < count; i++)
    {
        pixels[i] = (byte)Math.Min(pixels[i] + 10, 255);
    }
}

// Matrix multiplication with SIMD
public void MatrixMultiplySimd(float[,] a, float[,] b, float[,] result)
{
    int n = a.GetLength(0);
    int vectorSize = Vector<float>.Count;
    
    for (int i = 0; i < n; i++)
    {
        for (int j = 0; j < n; j++)
        {
            var sum = Vector<float>.Zero;
            int k = 0;
            
            // Vectorized multiplication
            for (; k <= n - vectorSize; k += vectorSize)
            {
                var va = new Vector<float>(GetRow(a, i, k));
                var vb = new Vector<float>(GetColumn(b, k, j));
                sum += va * vb;
            }
            
            // Sum vector elements
            float total = 0;
            for (int vi = 0; vi < vectorSize; vi++)
            {
                total += sum[vi];
            }
            
            // Add remaining elements
            for (; k < n; k++)
            {
                total += a[i, k] * b[k, j];
            }
            
            result[i, j] = total;
        }
    }
}

// Custom SIMD operations
public struct Vector4
{
    private Vector128<float> _data;
    
    public Vector4(float x, float y, float z, float w)
    {
        _data = Vector128.Create(x, y, z, w);
    }
    
    public static Vector4 operator +(Vector4 a, Vector4 b)
    {
        return new Vector4 { _data = Sse.Add(a._data, b._data) };
    }
    
    public float Length()
    {
        var squared = Sse.Multiply(_data, _data);
        var sum = Sse.HorizontalAdd(squared, squared);
        sum = Sse.HorizontalAdd(sum, sum);
        return MathF.Sqrt(sum.GetElement(0));
    }
}`}
        />
      </section>

      {/* Fixed Arrays */}
      <section className="mb-12">
        <h2 id="fixed" className="text-3xl font-bold text-gray-900 dark:text-white mb-6">
          Fixed Arrays and Memory Pinning
        </h2>
        <p className="text-gray-600 dark:text-gray-300 mb-4">
          Pin managed memory for interop and prevent garbage collection movement.
        </p>
        <CodeBlock
          code={`@low
// Fixed statement to pin arrays
unsafe
{
    byte[] managedArray = new byte[1024];
    
    // Pin the array
    fixed (byte* ptr = managedArray)
    {
        // Array won't move during this block
        NativeMethod(ptr, managedArray.Length);
        
        // Direct manipulation
        for (int i = 0; i < 1024; i++)
        {
            ptr[i] = (byte)(i & 0xFF);
        }
    }
}

// Fixed-size buffers in structs
public unsafe struct NetworkPacket
{
    public fixed byte Header[16];    // 16-byte header
    public fixed byte Payload[1024]; // 1KB payload
    public int PayloadLength;
    
    public void SetHeader(byte[] headerData)
    {
        fixed (byte* src = headerData)
        fixed (byte* dst = Header)
        {
            for (int i = 0; i < 16 && i < headerData.Length; i++)
            {
                dst[i] = src[i];
            }
        }
    }
    
    public byte[] GetPayload()
    {
        byte[] result = new byte[PayloadLength];
        fixed (byte* src = Payload)
        fixed (byte* dst = result)
        {
            Buffer.MemoryCopy(src, dst, PayloadLength, PayloadLength);
        }
        return result;
    }
}

// GC handle for long-term pinning
public class PinnedBuffer : IDisposable
{
    private GCHandle _handle;
    private byte[] _buffer;
    
    public unsafe byte* Pointer { get; private set; }
    
    public PinnedBuffer(int size)
    {
        _buffer = new byte[size];
        _handle = GCHandle.Alloc(_buffer, GCHandleType.Pinned);
        Pointer = (byte*)_handle.AddrOfPinnedObject();
    }
    
    public void Dispose()
    {
        if (_handle.IsAllocated)
        {
            _handle.Free();
            Pointer = null;
        }
    }
}

// Memory mapped files for large data
public unsafe void ProcessLargeFile(string path)
{
    using var mmf = MemoryMappedFile.CreateFromFile(path);
    using var accessor = mmf.CreateViewAccessor();
    
    byte* ptr = null;
    accessor.SafeMemoryMappedViewHandle.AcquirePointer(ref ptr);
    
    try
    {
        // Direct access to file contents
        long fileSize = new FileInfo(path).Length;
        ProcessRawData(ptr, fileSize);
    }
    finally
    {
        accessor.SafeMemoryMappedViewHandle.ReleasePointer();
    }
}

// Span and Memory for safer operations
public void SaferLowLevel()
{
    // Stack allocated span
    Span<byte> stackBuffer = stackalloc byte[256];
    stackBuffer.Fill(0);
    
    // Slicing
    Span<byte> firstHalf = stackBuffer.Slice(0, 128);
    Span<byte> secondHalf = stackBuffer.Slice(128);
    
    // Type reinterpretation
    Span<int> intView = MemoryMarshal.Cast<byte, int>(stackBuffer);
    
    // Memory<T> for async operations
    Memory<byte> memory = new byte[1024];
    ProcessAsync(memory);
}`}
        />
      </section>

      {/* Union Types */}
      <section className="mb-12">
        <h2 className="text-3xl font-bold text-gray-900 dark:text-white mb-6">
          Unions and Type Punning
        </h2>
        <p className="text-gray-600 dark:text-gray-300 mb-4">
          Overlapping memory layouts for efficient type conversions.
        </p>
        <CodeBlock
          code={`@low
// Union using StructLayout
[StructLayout(LayoutKind.Explicit)]
public struct FloatIntUnion
{
    [FieldOffset(0)] public float FloatValue;
    [FieldOffset(0)] public int IntValue;
    [FieldOffset(0)] public uint UIntValue;
    
    // Get bit representation of float
    public string GetFloatBits()
    {
        return Convert.ToString(IntValue, 2).PadLeft(32, '0');
    }
    
    // Check special float values
    public bool IsNaN => (UIntValue & 0x7F800000) == 0x7F800000 && 
                         (UIntValue & 0x007FFFFF) != 0;
    
    public bool IsInfinity => (UIntValue & 0x7FFFFFFF) == 0x7F800000;
}

// Color union for graphics
[StructLayout(LayoutKind.Explicit)]
public struct Color32
{
    [FieldOffset(0)] public uint RGBA;
    [FieldOffset(0)] public byte R;
    [FieldOffset(1)] public byte G;
    [FieldOffset(2)] public byte B;
    [FieldOffset(3)] public byte A;
    
    public Color32(byte r, byte g, byte b, byte a = 255)
    {
        RGBA = 0;  // Initialize all fields
        R = r;
        G = g;
        B = b;
        A = a;
    }
    
    // Fast color operations
    public static Color32 Blend(Color32 a, Color32 b, byte alpha)
    {
        int invAlpha = 255 - alpha;
        return new Color32(
            (byte)((a.R * invAlpha + b.R * alpha) / 255),
            (byte)((a.G * invAlpha + b.G * alpha) / 255),
            (byte)((a.B * invAlpha + b.B * alpha) / 255),
            (byte)((a.A * invAlpha + b.A * alpha) / 255)
        );
    }
}

// Vector union for 3D math
[StructLayout(LayoutKind.Explicit, Size = 16)]
public struct Vector4Union
{
    [FieldOffset(0)] public Vector128<float> Simd;
    [FieldOffset(0)] public float X;
    [FieldOffset(4)] public float Y;
    [FieldOffset(8)] public float Z;
    [FieldOffset(12)] public float W;
    
    public float this[int index]
    {
        get
        {
            unsafe
            {
                fixed (float* ptr = &X)
                {
                    return ptr[index];
                }
            }
        }
        set
        {
            unsafe
            {
                fixed (float* ptr = &X)
                {
                    ptr[index] = value;
                }
            }
        }
    }
}

// Tagged union (discriminated union)
[StructLayout(LayoutKind.Explicit)]
public struct Variant
{
    [FieldOffset(0)] public VariantType Type;
    [FieldOffset(4)] public int IntValue;
    [FieldOffset(4)] public float FloatValue;
    [FieldOffset(4)] public bool BoolValue;
    [FieldOffset(4)] public IntPtr PointerValue;
    
    public static Variant FromInt(int value) =>
        new Variant { Type = VariantType.Int, IntValue = value };
    
    public static Variant FromFloat(float value) =>
        new Variant { Type = VariantType.Float, FloatValue = value };
    
    public T GetValue<T>()
    {
        return Type switch
        {
            VariantType.Int when typeof(T) == typeof(int) => (T)(object)IntValue,
            VariantType.Float when typeof(T) == typeof(float) => (T)(object)FloatValue,
            VariantType.Bool when typeof(T) == typeof(bool) => (T)(object)BoolValue,
            _ => throw new InvalidCastException()
        };
    }
}

public enum VariantType : int
{
    None = 0,
    Int = 1,
    Float = 2,
    Bool = 3,
    Pointer = 4
}`}
        />
      </section>

      {/* Real-World Example */}
      <section className="mb-12">
        <h2 className="text-3xl font-bold text-gray-900 dark:text-white mb-6">
          Real-World Example: High-Performance Buffer Pool
        </h2>
        <CodeBlock
          code={`@low
// High-performance buffer pool for network operations
public unsafe class NativeBufferPool : IDisposable
{
    private readonly int _bufferSize;
    private readonly int _poolSize;
    private readonly Stack<IntPtr> _available;
    private readonly HashSet<IntPtr> _all;
    private readonly object _lock = new object();
    private IntPtr _memory;
    private bool _disposed;
    
    public NativeBufferPool(int bufferSize, int poolSize)
    {
        _bufferSize = bufferSize;
        _poolSize = poolSize;
        _available = new Stack<IntPtr>(poolSize);
        _all = new HashSet<IntPtr>(poolSize);
        
        // Allocate all buffers in one block
        _memory = Marshal.AllocHGlobal(bufferSize * poolSize);
        
        // Initialize pool
        byte* ptr = (byte*)_memory;
        for (int i = 0; i < poolSize; i++)
        {
            IntPtr buffer = (IntPtr)(ptr + i * bufferSize);
            _available.Push(buffer);
            _all.Add(buffer);
        }
    }
    
    public BufferHandle Rent()
    {
        lock (_lock)
        {
            if (_available.Count == 0)
                throw new InvalidOperationException("No buffers available");
            
            var buffer = _available.Pop();
            return new BufferHandle(this, buffer, _bufferSize);
        }
    }
    
    private void Return(IntPtr buffer)
    {
        lock (_lock)
        {
            if (!_all.Contains(buffer))
                throw new ArgumentException("Buffer not from this pool");
            
            // Clear buffer before returning to pool
            new Span<byte>((void*)buffer, _bufferSize).Clear();
            _available.Push(buffer);
        }
    }
    
    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
            if (_memory != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(_memory);
                _memory = IntPtr.Zero;
            }
        }
    }
    
    public struct BufferHandle : IDisposable
    {
        private NativeBufferPool _pool;
        private IntPtr _buffer;
        private int _size;
        
        internal BufferHandle(NativeBufferPool pool, IntPtr buffer, int size)
        {
            _pool = pool;
            _buffer = buffer;
            _size = size;
        }
        
        public byte* Pointer => (byte*)_buffer;
        public int Size => _size;
        public Span<byte> Span => new Span<byte>(Pointer, Size);
        
        public void Dispose()
        {
            if (_buffer != IntPtr.Zero)
            {
                _pool?.Return(_buffer);
                _buffer = IntPtr.Zero;
            }
        }
    }
}

// Usage example
public class NetworkProcessor
{
    private readonly NativeBufferPool _pool;
    
    public NetworkProcessor()
    {
        // 64KB buffers, 100 in pool
        _pool = new NativeBufferPool(65536, 100);
    }
    
    public async Task ProcessPacketAsync(Socket socket)
    {
        using var buffer = _pool.Rent();
        
        // Receive data directly into native buffer
        int received = await ReceiveAsync(socket, buffer.Pointer, buffer.Size);
        
        // Process packet with SIMD
        if (Vector.IsHardwareAccelerated)
        {
            ProcessPacketSimd(buffer.Pointer, received);
        }
        else
        {
            ProcessPacketScalar(buffer.Pointer, received);
        }
    }
    
    private unsafe void ProcessPacketSimd(byte* data, int length)
    {
        int vectorSize = Vector<byte>.Count;
        int i = 0;
        
        // XOR decrypt with SIMD
        byte key = 0xAB;
        var keyVector = new Vector<byte>(key);
        
        for (; i <= length - vectorSize; i += vectorSize)
        {
            var dataVector = Unsafe.Read<Vector<byte>>(data + i);
            var decrypted = dataVector ^ keyVector;
            Unsafe.Write(data + i, decrypted);
        }
        
        // Handle remaining bytes
        for (; i < length; i++)
        {
            data[i] ^= key;
        }
    }
    
    private unsafe async Task<int> ReceiveAsync(Socket socket, byte* buffer, int size)
    {
        // Pin buffer for async operation
        var handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
        try
        {
            var segment = new ArraySegment<byte>(new byte[size]);
            return await socket.ReceiveAsync(segment, SocketFlags.None);
        }
        finally
        {
            handle.Free();
        }
    }
}`}
        />
      </section>

      {/* Best Practices */}
      <section className="mb-12">
        <h2 className="text-3xl font-bold text-gray-900 dark:text-white mb-6">
          Best Practices
        </h2>
        
        <Callout type="danger" title="Safety First">
          Low-level code can easily cause crashes, memory corruption, and security vulnerabilities:
          <ul className="list-disc list-inside mt-2">
            <li>Always validate bounds when using pointers</li>
            <li>Use <code>fixed</code> to prevent GC movement</li>
            <li>Prefer <code>Span&lt;T&gt;</code> over raw pointers when possible</li>
            <li>Clear sensitive data from memory after use</li>
            <li>Test thoroughly with tools like AddressSanitizer</li>
          </ul>
        </Callout>

        <Callout type="tip" title="Performance Guidelines">
          Maximize performance with low-level features:
          <ul className="list-disc list-inside mt-2">
            <li>Use SIMD for data-parallel operations</li>
            <li>Align data for optimal cache usage</li>
            <li>Minimize allocations with stack memory</li>
            <li>Pool native memory for reuse</li>
            <li>Profile before and after optimization</li>
          </ul>
        </Callout>

        <CodeBlock
          code={`@low
// Good: Bounds checking
unsafe void ProcessBuffer(byte* buffer, int length)
{
    if (buffer == null) throw new ArgumentNullException(nameof(buffer));
    if (length < 0) throw new ArgumentOutOfRangeException(nameof(length));
    
    // Process safely...
}

// Good: Use Span for safety
void SaferProcessing(ReadOnlySpan<byte> data)
{
    // Automatic bounds checking
    for (int i = 0; i < data.Length; i++)
    {
        ProcessByte(data[i]);
    }
}

// Good: Clear sensitive data
unsafe void ClearSensitiveData(byte* data, int length)
{
    // Use volatile writes to prevent optimization
    byte* volatile ptr = data;
    while (length-- > 0)
    {
        *ptr++ = 0;
    }
}

// Good: SIMD with fallback
public void OptimizedProcess(float[] data)
{
    if (Vector.IsHardwareAccelerated && data.Length >= Vector<float>.Count)
    {
        ProcessSimd(data);
    }
    else
    {
        ProcessScalar(data);
    }
}

// Consider: When to use low-level
// ✓ Interop with native libraries
// ✓ Performance-critical hot paths  
// ✓ Custom memory management
// ✓ Hardware interfacing
// ✗ General application logic
// ✗ Business rules
// ✗ UI code`}
        />
      </section>
    </div>
  )
} 