using System;
using System.Runtime.InteropServices;

namespace Ouroboros.GPU.OpenCL
{
    /// <summary>
    /// P/Invoke bindings for OpenCL API
    /// </summary>
    public static class OpenCLAPI
    {
        private const string OPENCL_DLL = "OpenCL.dll";

        // Platform API
        [DllImport(OPENCL_DLL)]
        public static extern CLError clGetPlatformIDs(uint num_entries, IntPtr[] platforms, out uint num_platforms);

        [DllImport(OPENCL_DLL)]
        public static extern CLError clGetPlatformInfo(IntPtr platform, CLPlatformInfo param_name, 
            uint param_value_size, IntPtr param_value, out uint param_value_size_ret);

        // Device API
        [DllImport(OPENCL_DLL)]
        public static extern CLError clGetDeviceIDs(IntPtr platform, CLDeviceType device_type, 
            uint num_entries, IntPtr[] devices, out uint num_devices);

        [DllImport(OPENCL_DLL)]
        public static extern CLError clGetDeviceInfo(IntPtr device, CLDeviceInfo param_name, 
            uint param_value_size, IntPtr param_value, out uint param_value_size_ret);

        [DllImport(OPENCL_DLL)]
        public static extern CLError clGetDeviceInfo(IntPtr device, CLDeviceInfo param_name, 
            uint param_value_size, out uint param_value, out uint param_value_size_ret);

        [DllImport(OPENCL_DLL)]
        public static extern CLError clGetDeviceInfo(IntPtr device, CLDeviceInfo param_name, 
            uint param_value_size, out ulong param_value, out uint param_value_size_ret);

        // Context API
        [DllImport(OPENCL_DLL)]
        public static extern IntPtr clCreateContext(IntPtr[] properties, uint num_devices, IntPtr[] devices, 
            IntPtr pfn_notify, IntPtr user_data, out int errcode_ret);

        [DllImport(OPENCL_DLL)]
        public static extern CLError clReleaseContext(IntPtr context);

        // Command Queue API
        [DllImport(OPENCL_DLL)]
        public static extern IntPtr clCreateCommandQueue(IntPtr context, IntPtr device, 
            CLCommandQueueFlags properties, out int errcode_ret);

        [DllImport(OPENCL_DLL)]
        public static extern CLError clReleaseCommandQueue(IntPtr command_queue);

        [DllImport(OPENCL_DLL)]
        public static extern CLError clFinish(IntPtr command_queue);

        // Memory Object API
        [DllImport(OPENCL_DLL)]
        public static extern IntPtr clCreateBuffer(IntPtr context, CLMemFlags flags, 
            uint size, IntPtr host_ptr, out int errcode_ret);

        [DllImport(OPENCL_DLL)]
        public static extern CLError clReleaseMemObject(IntPtr memobj);

        [DllImport(OPENCL_DLL)]
        public static extern CLError clEnqueueReadBuffer(IntPtr command_queue, IntPtr buffer, 
            CLBool blocking_read, uint offset, uint size, IntPtr ptr, 
            uint num_events_in_wait_list, IntPtr[] event_wait_list, IntPtr event_);

        [DllImport(OPENCL_DLL)]
        public static extern CLError clEnqueueWriteBuffer(IntPtr command_queue, IntPtr buffer, 
            CLBool blocking_write, uint offset, uint size, IntPtr ptr, 
            uint num_events_in_wait_list, IntPtr[] event_wait_list, IntPtr event_);

        // Program API
        [DllImport(OPENCL_DLL)]
        public static extern IntPtr clCreateProgramWithSource(IntPtr context, uint count, 
            string[] strings, uint[] lengths, out int errcode_ret);

        [DllImport(OPENCL_DLL)]
        public static extern CLError clBuildProgram(IntPtr program, uint num_devices, IntPtr[] device_list, 
            string options, IntPtr pfn_notify, IntPtr user_data);

        [DllImport(OPENCL_DLL)]
        public static extern CLError clReleaseProgram(IntPtr program);

        [DllImport(OPENCL_DLL)]
        public static extern CLError clGetProgramBuildInfo(IntPtr program, IntPtr device, 
            CLProgramBuildInfo param_name, uint param_value_size, IntPtr param_value, out uint param_value_size_ret);

        // Kernel API
        [DllImport(OPENCL_DLL)]
        public static extern IntPtr clCreateKernel(IntPtr program, string kernel_name, out int errcode_ret);

        [DllImport(OPENCL_DLL)]
        public static extern CLError clReleaseKernel(IntPtr kernel);

        [DllImport(OPENCL_DLL)]
        public static extern CLError clSetKernelArg(IntPtr kernel, uint arg_index, uint arg_size, ref IntPtr arg_value);

        [DllImport(OPENCL_DLL)]
        public static extern CLError clSetKernelArg(IntPtr kernel, uint arg_index, uint arg_size, IntPtr arg_value);

        [DllImport(OPENCL_DLL)]
        public static extern CLError clEnqueueNDRangeKernel(IntPtr command_queue, IntPtr kernel, 
            uint work_dim, uint[] global_work_offset, uint[] global_work_size, uint[] local_work_size, 
            uint num_events_in_wait_list, IntPtr[] event_wait_list, IntPtr event_);
    }

    public enum CLError
    {
        Success = 0,
        DeviceNotFound = -1,
        DeviceNotAvailable = -2,
        CompilerNotAvailable = -3,
        MemObjectAllocationFailure = -4,
        OutOfResources = -5,
        OutOfHostMemory = -6,
        ProfilingInfoNotAvailable = -7,
        MemCopyOverlap = -8,
        ImageFormatMismatch = -9,
        ImageFormatNotSupported = -10,
        BuildProgramFailure = -11,
        MapFailure = -12,
        InvalidValue = -30,
        InvalidDeviceType = -31,
        InvalidPlatform = -32,
        InvalidDevice = -33,
        InvalidContext = -34,
        InvalidQueueProperties = -35,
        InvalidCommandQueue = -36,
        InvalidHostPtr = -37,
        InvalidMemObject = -38,
        InvalidImageFormatDescriptor = -39,
        InvalidImageSize = -40,
        InvalidSampler = -41,
        InvalidBinary = -42,
        InvalidBuildOptions = -43,
        InvalidProgram = -44,
        InvalidProgramExecutable = -45,
        InvalidKernelName = -46,
        InvalidKernelDefinition = -47,
        InvalidKernel = -48,
        InvalidArgIndex = -49,
        InvalidArgValue = -50,
        InvalidArgSize = -51,
        InvalidKernelArgs = -52,
        InvalidWorkDimension = -53,
        InvalidWorkGroupSize = -54,
        InvalidWorkItemSize = -55,
        InvalidGlobalOffset = -56,
        InvalidEventWaitList = -57,
        InvalidEvent = -58,
        InvalidOperation = -59,
        InvalidGLObject = -60,
        InvalidBufferSize = -61,
        InvalidMipLevel = -62,
        InvalidGlobalWorkSize = -63
    }

    public enum CLDeviceType : ulong
    {
        Default = (1 << 0),
        CPU = (1 << 1),
        GPU = (1 << 2),
        Accelerator = (1 << 3),
        Custom = (1 << 4),
        All = 0xFFFFFFFF
    }

    public enum CLDeviceInfo : uint
    {
        Type = 0x1000,
        VendorID = 0x1001,
        MaxComputeUnits = 0x1002,
        MaxWorkItemDimensions = 0x1003,
        MaxWorkGroupSize = 0x1004,
        MaxWorkItemSizes = 0x1005,
        PreferredVectorWidthChar = 0x1006,
        PreferredVectorWidthShort = 0x1007,
        PreferredVectorWidthInt = 0x1008,
        PreferredVectorWidthLong = 0x1009,
        PreferredVectorWidthFloat = 0x100A,
        PreferredVectorWidthDouble = 0x100B,
        MaxClockFrequency = 0x100C,
        AddressBits = 0x100D,
        MaxReadImageArgs = 0x100E,
        MaxWriteImageArgs = 0x100F,
        MaxMemAllocSize = 0x1010,
        Image2DMaxWidth = 0x1011,
        Image2DMaxHeight = 0x1012,
        Image3DMaxWidth = 0x1013,
        Image3DMaxHeight = 0x1014,
        Image3DMaxDepth = 0x1015,
        ImageSupport = 0x1016,
        MaxParameterSize = 0x1017,
        MaxSamplers = 0x1018,
        MemBaseAddrAlign = 0x1019,
        MinDataTypeAlignSize = 0x101A,
        SingleFPConfig = 0x101B,
        GlobalMemCacheType = 0x101C,
        GlobalMemCachelineSize = 0x101D,
        GlobalMemCacheSize = 0x101E,
        GlobalMemSize = 0x101F,
        MaxConstantBufferSize = 0x1020,
        MaxConstantArgs = 0x1021,
        LocalMemType = 0x1022,
        LocalMemSize = 0x1023,
        ErrorCorrectionSupport = 0x1024,
        ProfilingTimerResolution = 0x1025,
        EndianLittle = 0x1026,
        Available = 0x1027,
        CompilerAvailable = 0x1028,
        ExecutionCapabilities = 0x1029,
        QueueProperties = 0x102A,
        Name = 0x102B,
        Vendor = 0x102C,
        DriverVersion = 0x102D,
        Profile = 0x102E,
        Version = 0x102F,
        Extensions = 0x1030,
        Platform = 0x1031
    }

    public enum CLPlatformInfo : uint
    {
        Profile = 0x0900,
        Version = 0x0901,
        Name = 0x0902,
        Vendor = 0x0903,
        Extensions = 0x0904
    }

    public enum CLContextProperties : int
    {
        Platform = 0x1084
    }

    public enum CLMemFlags : ulong
    {
        ReadWrite = (1 << 0),
        WriteOnly = (1 << 1),
        ReadOnly = (1 << 2),
        UseHostPtr = (1 << 3),
        AllocHostPtr = (1 << 4),
        CopyHostPtr = (1 << 5)
    }

    public enum CLCommandQueueFlags : ulong
    {
        None = 0,
        OutOfOrderExecModeEnable = (1 << 0),
        ProfilingEnable = (1 << 1)
    }

    public enum CLProgramBuildInfo : uint
    {
        Status = 0x1181,
        Options = 0x1182,
        Log = 0x1183
    }

    public enum CLBool : uint
    {
        False = 0,
        True = 1
    }
} 