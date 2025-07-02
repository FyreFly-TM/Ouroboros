using System;
using System.Runtime.InteropServices;

namespace Ouro.GPU.CUDA
{
    /// <summary>
    /// P/Invoke bindings for CUDA Driver API
    /// </summary>
    public static class CudaAPI
    {
        private const string CUDA_DLL = "nvcuda.dll";

        // Initialization
        [DllImport(CUDA_DLL)]
        public static extern CudaResult cuInit(uint Flags);

        // Device Management
        [DllImport(CUDA_DLL)]
        public static extern CudaResult cuDeviceGet(out IntPtr device, int ordinal);

        [DllImport(CUDA_DLL)]
        public static extern CudaResult cuDeviceGetCount(out int count);

        [DllImport(CUDA_DLL)]
        public static extern CudaResult cuDeviceGetName(byte[] name, int len, IntPtr dev);

        [DllImport(CUDA_DLL)]
        public static extern CudaResult cuDeviceComputeCapability(out int major, out int minor, IntPtr dev);

        [DllImport(CUDA_DLL)]
        public static extern CudaResult cuDeviceTotalMem(out long bytes, IntPtr dev);

        [DllImport(CUDA_DLL)]
        public static extern CudaResult cuDeviceGetAttribute(out int value, CudaDeviceAttribute attrib, IntPtr dev);

        // Context Management
        [DllImport(CUDA_DLL)]
        public static extern CudaResult cuCtxCreate(out IntPtr pctx, uint flags, IntPtr dev);

        [DllImport(CUDA_DLL)]
        public static extern CudaResult cuCtxDestroy(IntPtr ctx);

        [DllImport(CUDA_DLL)]
        public static extern CudaResult cuCtxSetCurrent(IntPtr ctx);

        [DllImport(CUDA_DLL)]
        public static extern CudaResult cuCtxGetCurrent(out IntPtr pctx);

        [DllImport(CUDA_DLL)]
        public static extern CudaResult cuCtxSynchronize();

        // Module Management
        [DllImport(CUDA_DLL)]
        public static extern CudaResult cuModuleLoad(out IntPtr module, string fname);

        [DllImport(CUDA_DLL)]
        public static extern CudaResult cuModuleLoadData(out IntPtr module, byte[] image);

        [DllImport(CUDA_DLL)]
        public static extern CudaResult cuModuleUnload(IntPtr hmod);

        [DllImport(CUDA_DLL)]
        public static extern CudaResult cuModuleGetFunction(out IntPtr hfunc, IntPtr hmod, string name);

        // Memory Management
        [DllImport(CUDA_DLL)]
        public static extern CudaResult cuMemAlloc(out IntPtr dptr, long bytesize);

        [DllImport(CUDA_DLL)]
        public static extern CudaResult cuMemFree(IntPtr dptr);

        [DllImport(CUDA_DLL)]
        public static extern CudaResult cuMemcpyHtoD(IntPtr dstDevice, IntPtr srcHost, long ByteCount);

        [DllImport(CUDA_DLL)]
        public static extern CudaResult cuMemcpyDtoH(IntPtr dstHost, IntPtr srcDevice, long ByteCount);

        [DllImport(CUDA_DLL)]
        public static extern CudaResult cuMemcpyDtoD(IntPtr dstDevice, IntPtr srcDevice, long ByteCount);

        [DllImport(CUDA_DLL)]
        public static extern CudaResult cuMemsetD8(IntPtr dstDevice, byte uc, long N);

        [DllImport(CUDA_DLL)]
        public static extern CudaResult cuMemsetD32(IntPtr dstDevice, uint ui, long N);

        // Kernel Execution
        [DllImport(CUDA_DLL)]
        public static extern CudaResult cuLaunchKernel(
            IntPtr f,
            uint gridDimX, uint gridDimY, uint gridDimZ,
            uint blockDimX, uint blockDimY, uint blockDimZ,
            uint sharedMemBytes, IntPtr hStream,
            IntPtr kernelParams, IntPtr extra);

        // Stream Management
        [DllImport(CUDA_DLL)]
        public static extern CudaResult cuStreamCreate(out IntPtr phStream, uint Flags);

        [DllImport(CUDA_DLL)]
        public static extern CudaResult cuStreamDestroy(IntPtr hStream);

        [DllImport(CUDA_DLL)]
        public static extern CudaResult cuStreamSynchronize(IntPtr hStream);

        [DllImport(CUDA_DLL)]
        public static extern CudaResult cuStreamWaitEvent(IntPtr hStream, IntPtr hEvent, uint Flags);

        // Event Management
        [DllImport(CUDA_DLL)]
        public static extern CudaResult cuEventCreate(out IntPtr phEvent, uint Flags);

        [DllImport(CUDA_DLL)]
        public static extern CudaResult cuEventDestroy(IntPtr hEvent);

        [DllImport(CUDA_DLL)]
        public static extern CudaResult cuEventRecord(IntPtr hEvent, IntPtr hStream);

        [DllImport(CUDA_DLL)]
        public static extern CudaResult cuEventSynchronize(IntPtr hEvent);

        [DllImport(CUDA_DLL)]
        public static extern CudaResult cuEventElapsedTime(out float pMilliseconds, IntPtr hStart, IntPtr hEnd);
    }

    public enum CudaResult
    {
        Success = 0,
        ErrorInvalidValue = 1,
        ErrorOutOfMemory = 2,
        ErrorNotInitialized = 3,
        ErrorDeinitialized = 4,
        ErrorNoDevice = 100,
        ErrorInvalidDevice = 101,
        ErrorInvalidImage = 200,
        ErrorInvalidContext = 201,
        ErrorContextAlreadyCurrent = 202,
        ErrorMapFailed = 205,
        ErrorUnmapFailed = 206,
        ErrorArrayIsMapped = 207,
        ErrorAlreadyMapped = 208,
        ErrorNoBinaryForGPU = 209,
        ErrorAlreadyAcquired = 210,
        ErrorNotMapped = 211,
        ErrorNotMappedAsArray = 212,
        ErrorNotMappedAsPointer = 213,
        ErrorECCUncorrectable = 214,
        ErrorUnsupportedLimit = 215,
        ErrorContextAlreadyInUse = 216,
        ErrorPeerAccessUnsupported = 217,
        ErrorInvalidPTX = 218,
        ErrorInvalidGraphicsContext = 219,
        ErrorNvlinkUncorrectable = 220,
        ErrorInvalidSource = 300,
        ErrorFileNotFound = 301,
        ErrorSharedObjectSymbolNotFound = 302,
        ErrorSharedObjectInitFailed = 303,
        ErrorOperatingSystem = 304,
        ErrorInvalidHandle = 400,
        ErrorNotFound = 500,
        ErrorNotReady = 600,
        ErrorIllegalAddress = 700,
        ErrorLaunchOutOfResources = 701,
        ErrorLaunchTimeout = 702,
        ErrorLaunchIncompatibleTexturing = 703,
        ErrorPeerAccessAlreadyEnabled = 704,
        ErrorPeerAccessNotEnabled = 705,
        ErrorPrimaryContextActive = 708,
        ErrorContextIsDestroyed = 709,
        ErrorAssert = 710,
        ErrorTooManyPeers = 711,
        ErrorHostMemoryAlreadyRegistered = 712,
        ErrorHostMemoryNotRegistered = 713,
        ErrorHardwareStackError = 714,
        ErrorIllegalInstruction = 715,
        ErrorMisalignedAddress = 716,
        ErrorInvalidAddressSpace = 717,
        ErrorInvalidPC = 718,
        ErrorLaunchFailed = 719,
        ErrorNotPermitted = 800,
        ErrorNotSupported = 801,
        ErrorUnknown = 999
    }

    public enum CudaDeviceAttribute
    {
        MaxThreadsPerBlock = 1,
        MaxBlockDimX = 2,
        MaxBlockDimY = 3,
        MaxBlockDimZ = 4,
        MaxGridDimX = 5,
        MaxGridDimY = 6,
        MaxGridDimZ = 7,
        SharedMemoryPerBlock = 8,
        TotalConstantMemory = 9,
        WarpSize = 10,
        MaxPitch = 11,
        RegistersPerBlock = 12,
        ClockRate = 13,
        TextureAlignment = 14,
        MultiprocessorCount = 16,
        KernelExecTimeout = 17,
        Integrated = 18,
        CanMapHostMemory = 19,
        ComputeMode = 20,
        MaxTexture1DWidth = 21,
        MaxTexture2DWidth = 22,
        MaxTexture2DHeight = 23,
        MaxTexture3DWidth = 24,
        MaxTexture3DHeight = 25,
        MaxTexture3DDepth = 26,
        ConcurrentKernels = 31,
        ECCEnabled = 32,
        PCIBusID = 33,
        PCIDeviceID = 34,
        MemoryClockRate = 36,
        GlobalMemoryBusWidth = 37,
        L2CacheSize = 38,
        MaxThreadsPerMultiProcessor = 39,
        ComputeCapabilityMajor = 75,
        ComputeCapabilityMinor = 76
    }
} 