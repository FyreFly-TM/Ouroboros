using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace Ouro.GPU.OpenCL
{
    /// <summary>
    /// OpenCL compute implementation for cross-platform GPU programming
    /// </summary>
    public class OpenCLCompute : IDisposable
    {
        private IntPtr platform;
        private IntPtr device;
        private IntPtr context;
        private IntPtr commandQueue;
        private bool disposed;

        public OpenCLDeviceInfo DeviceInfo { get; private set; }

        public OpenCLCompute(int platformIndex = 0, int deviceIndex = 0)
        {
            Initialize(platformIndex, deviceIndex);
        }

        private void Initialize(int platformIndex, int deviceIndex)
        {
            // Get platforms
            uint platformCount;
            CheckError(OpenCLAPI.clGetPlatformIDs(0, null, out platformCount));
            
            if (platformCount == 0)
                throw new Exception("No OpenCL platforms found");

            var platforms = new IntPtr[platformCount];
            CheckError(OpenCLAPI.clGetPlatformIDs(platformCount, platforms, out platformCount));
            platform = platforms[Math.Min(platformIndex, (int)platformCount - 1)];

            // Get devices
            uint deviceCount;
            CheckError(OpenCLAPI.clGetDeviceIDs(platform, CLDeviceType.GPU, 0, null, out deviceCount));
            
            if (deviceCount == 0)
                throw new Exception("No OpenCL GPU devices found");

            var devices = new IntPtr[deviceCount];
            CheckError(OpenCLAPI.clGetDeviceIDs(platform, CLDeviceType.GPU, deviceCount, devices, out deviceCount));
            device = devices[Math.Min(deviceIndex, (int)deviceCount - 1)];

            // Query device info
            QueryDeviceInfo();

            // Create context
            var contextProperties = new IntPtr[] { (IntPtr)CLContextProperties.Platform, platform, IntPtr.Zero };
            int errorCode;
            context = OpenCLAPI.clCreateContext(contextProperties, 1, new[] { device }, IntPtr.Zero, IntPtr.Zero, out errorCode);
            CheckError((CLError)errorCode);

            // Create command queue
            commandQueue = OpenCLAPI.clCreateCommandQueue(context, device, CLCommandQueueFlags.None, out errorCode);
            CheckError((CLError)errorCode);
        }

        /// <summary>
        /// Compile OpenCL kernel from source
        /// </summary>
        public OpenCLKernel CompileKernel(string source, string kernelName, string buildOptions = "")
        {
            int errorCode;
            
            // Create program
            var program = OpenCLAPI.clCreateProgramWithSource(context, 1, new[] { source }, 
                new[] { (uint)source.Length }, out errorCode);
            CheckError((CLError)errorCode);

            // Build program
            var buildError = OpenCLAPI.clBuildProgram(program, 1, new[] { device }, buildOptions, IntPtr.Zero, IntPtr.Zero);
            
            if (buildError != CLError.Success)
            {
                // Get build log
                uint logSize;
                OpenCLAPI.clGetProgramBuildInfo(program, device, CLProgramBuildInfo.Log, 0, IntPtr.Zero, out logSize);
                
                var logBuffer = new byte[logSize];
                var handle = GCHandle.Alloc(logBuffer, GCHandleType.Pinned);
                try
                {
                    OpenCLAPI.clGetProgramBuildInfo(program, device, CLProgramBuildInfo.Log, logSize, 
                        handle.AddrOfPinnedObject(), out logSize);
                    var log = Encoding.ASCII.GetString(logBuffer, 0, (int)logSize - 1);
                    throw new OpenCLCompilationException($"OpenCL build failed: {log}");
                }
                finally
                {
                    handle.Free();
                }
            }

            // Create kernel
            var kernel = OpenCLAPI.clCreateKernel(program, kernelName, out errorCode);
            CheckError((CLError)errorCode);

            return new OpenCLKernel
            {
                Kernel = kernel,
                Program = program,
                Name = kernelName
            };
        }

        /// <summary>
        /// Create buffer on device
        /// </summary>
        public OpenCLBuffer<T> CreateBuffer<T>(int count, CLMemFlags flags = CLMemFlags.ReadWrite) where T : struct
        {
            var size = Marshal.SizeOf<T>() * count;
            int errorCode;
            
            var buffer = OpenCLAPI.clCreateBuffer(context, flags, (uint)size, IntPtr.Zero, out errorCode);
            CheckError((CLError)errorCode);

            return new OpenCLBuffer<T>
            {
                Buffer = buffer,
                Size = size,
                Count = count,
                Context = context,
                CommandQueue = commandQueue
            };
        }

        /// <summary>
        /// Execute kernel
        /// </summary>
        public void ExecuteKernel(OpenCLKernel kernel, uint[] globalWorkSize, uint[] localWorkSize, params object[] args)
        {
            // Set kernel arguments
            for (int i = 0; i < args.Length; i++)
            {
                var arg = args[i];
                
                if (arg is OpenCLBuffer openCLBuffer)
                {
                    var buffer = openCLBuffer.Buffer;
                    CheckError(OpenCLAPI.clSetKernelArg(kernel.Kernel, (uint)i, (uint)IntPtr.Size, ref buffer));
                }
                else
                {
                    var handle = GCHandle.Alloc(arg, GCHandleType.Pinned);
                    try
                    {
                        var size = (uint)Marshal.SizeOf(arg.GetType());
                        CheckError(OpenCLAPI.clSetKernelArg(kernel.Kernel, (uint)i, size, handle.AddrOfPinnedObject()));
                    }
                    finally
                    {
                        handle.Free();
                    }
                }
            }

            // Execute kernel
            CheckError(OpenCLAPI.clEnqueueNDRangeKernel(commandQueue, kernel.Kernel, 
                (uint)globalWorkSize.Length, null, globalWorkSize, localWorkSize, 
                0, null, IntPtr.Zero));

            // Wait for completion
            CheckError(OpenCLAPI.clFinish(commandQueue));
        }

        private void QueryDeviceInfo()
        {
            var info = new OpenCLDeviceInfo();

            // Device name
            uint nameSize;
            OpenCLAPI.clGetDeviceInfo(device, CLDeviceInfo.Name, 0, IntPtr.Zero, out nameSize);
            var nameBuffer = new byte[nameSize];
            var handle = GCHandle.Alloc(nameBuffer, GCHandleType.Pinned);
            try
            {
                OpenCLAPI.clGetDeviceInfo(device, CLDeviceInfo.Name, nameSize, handle.AddrOfPinnedObject(), out nameSize);
                info.DeviceName = Encoding.ASCII.GetString(nameBuffer, 0, (int)nameSize - 1);
            }
            finally
            {
                handle.Free();
            }

            // Other properties
            ulong globalMemSize;
            OpenCLAPI.clGetDeviceInfo(device, CLDeviceInfo.GlobalMemSize, (uint)sizeof(ulong), 
                out globalMemSize, out _);
            info.GlobalMemorySize = (long)globalMemSize;

            uint maxComputeUnits;
            OpenCLAPI.clGetDeviceInfo(device, CLDeviceInfo.MaxComputeUnits, (uint)sizeof(uint), 
                out maxComputeUnits, out _);
            info.MaxComputeUnits = (int)maxComputeUnits;

            uint maxWorkGroupSize;
            OpenCLAPI.clGetDeviceInfo(device, CLDeviceInfo.MaxWorkGroupSize, (uint)sizeof(uint), 
                out maxWorkGroupSize, out _);
            info.MaxWorkGroupSize = (int)maxWorkGroupSize;

            DeviceInfo = info;
        }

        private void CheckError(CLError error)
        {
            if (error != CLError.Success)
                throw new OpenCLException(error);
        }

        public void Dispose()
        {
            if (!disposed)
            {
                if (commandQueue != IntPtr.Zero)
                    OpenCLAPI.clReleaseCommandQueue(commandQueue);

                if (context != IntPtr.Zero)
                    OpenCLAPI.clReleaseContext(context);

                disposed = true;
            }
        }
    }

    public class OpenCLKernel
    {
        public IntPtr Kernel { get; set; }
        public IntPtr Program { get; set; }
        public string Name { get; set; }
    }

    public abstract class OpenCLBuffer
    {
        public IntPtr Buffer { get; set; }
        public int Size { get; set; }
        public int Count { get; set; }
        public IntPtr Context { get; set; }
        public IntPtr CommandQueue { get; set; }
    }

    public class OpenCLBuffer<T> : OpenCLBuffer where T : struct
    {
        public void WriteData(T[] data)
        {
            var handle = GCHandle.Alloc(data, GCHandleType.Pinned);
            try
            {
                OpenCLAPI.clEnqueueWriteBuffer(CommandQueue, Buffer, CLBool.True, 0, 
                    (uint)Size, handle.AddrOfPinnedObject(), 0, null, IntPtr.Zero);
            }
            finally
            {
                handle.Free();
            }
        }

        public T[] ReadData()
        {
            var data = new T[Count];
            var handle = GCHandle.Alloc(data, GCHandleType.Pinned);
            try
            {
                OpenCLAPI.clEnqueueReadBuffer(CommandQueue, Buffer, CLBool.True, 0, 
                    (uint)Size, handle.AddrOfPinnedObject(), 0, null, IntPtr.Zero);
                return data;
            }
            finally
            {
                handle.Free();
            }
        }
    }

    public class OpenCLDeviceInfo
    {
        public string DeviceName { get; set; }
        public long GlobalMemorySize { get; set; }
        public int MaxComputeUnits { get; set; }
        public int MaxWorkGroupSize { get; set; }
    }

    public class OpenCLException : Exception
    {
        public CLError ErrorCode { get; }

        public OpenCLException(CLError errorCode)
            : base($"OpenCL Error: {errorCode}")
        {
            ErrorCode = errorCode;
        }
    }

    public class OpenCLCompilationException : Exception
    {
        public OpenCLCompilationException(string message) : base(message) { }
    }
} 