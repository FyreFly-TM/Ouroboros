using System;
using System.Runtime.InteropServices;

namespace Ouroboros.GPU.CUDA
{
    /// <summary>
    /// CUDA context management for GPU operations
    /// </summary>
    public class CudaContext : IDisposable
    {
        private IntPtr context;
        private IntPtr device;
        private bool disposed;

        public int DeviceId { get; private set; }
        public CudaDeviceProperties Properties { get; private set; }

        public CudaContext(int deviceId = 0)
        {
            DeviceId = deviceId;
            Initialize();
        }

        private void Initialize()
        {
            // Initialize CUDA
            CheckError(CudaAPI.cuInit(0));

            // Get device
            CheckError(CudaAPI.cuDeviceGet(out device, DeviceId));

            // Create context
            CheckError(CudaAPI.cuCtxCreate(out context, 0, device));

            // Get device properties
            Properties = QueryDeviceProperties();
        }

        private CudaDeviceProperties QueryDeviceProperties()
        {
            var props = new CudaDeviceProperties();
            var nameBuffer = new byte[256];

            CudaAPI.cuDeviceGetName(nameBuffer, nameBuffer.Length, device);
            props.Name = Marshal.PtrToStringAnsi(Marshal.UnsafeAddrOfPinnedArrayElement(nameBuffer, 0));

            CudaAPI.cuDeviceComputeCapability(out props.Major, out props.Minor, device);
            CudaAPI.cuDeviceTotalMem(out props.TotalMemory, device);
            CudaAPI.cuDeviceGetAttribute(out props.SharedMemoryPerBlock, CudaDeviceAttribute.SharedMemoryPerBlock, device);
            CudaAPI.cuDeviceGetAttribute(out props.MaxThreadsPerBlock, CudaDeviceAttribute.MaxThreadsPerBlock, device);
            CudaAPI.cuDeviceGetAttribute(out props.WarpSize, CudaDeviceAttribute.WarpSize, device);
            CudaAPI.cuDeviceGetAttribute(out props.MultiprocessorCount, CudaDeviceAttribute.MultiprocessorCount, device);

            return props;
        }

        public void MakeCurrent()
        {
            CheckError(CudaAPI.cuCtxSetCurrent(context));
        }

        public void Synchronize()
        {
            CheckError(CudaAPI.cuCtxSynchronize());
        }

        private void CheckError(CudaResult result)
        {
            if (result != CudaResult.Success)
            {
                throw new CudaException(result);
            }
        }

        public void Dispose()
        {
            if (!disposed)
            {
                if (context != IntPtr.Zero)
                {
                    CudaAPI.cuCtxDestroy(context);
                }
                disposed = true;
            }
        }
    }

    public class CudaDeviceProperties
    {
        public string Name { get; set; }
        public int Major { get; set; }
        public int Minor { get; set; }
        public long TotalMemory { get; set; }
        public int SharedMemoryPerBlock { get; set; }
        public int MaxThreadsPerBlock { get; set; }
        public int WarpSize { get; set; }
        public int MultiprocessorCount { get; set; }
    }

    public class CudaException : Exception
    {
        public CudaResult ErrorCode { get; }

        public CudaException(CudaResult errorCode) 
            : base($"CUDA Error: {errorCode}")
        {
            ErrorCode = errorCode;
        }
    }
} 