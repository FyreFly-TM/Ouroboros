using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Ouro.GPU.Vulkan
{
    /// <summary>
    /// Vulkan compute pipeline for GPU computation
    /// </summary>
    public class VulkanCompute : IDisposable
    {
        private VkInstance instance;
        private VkPhysicalDevice physicalDevice;
        private VkDevice device;
        private VkQueue computeQueue;
        private uint computeQueueFamilyIndex;
        private VkCommandPool commandPool;
        private bool disposed;

        public VulkanDeviceInfo DeviceInfo { get; private set; }

        public VulkanCompute()
        {
            Initialize();
        }

        private void Initialize()
        {
            CreateInstance();
            SelectPhysicalDevice();
            CreateLogicalDevice();
            CreateCommandPool();
        }

        private void CreateInstance()
        {
            var appInfo = new VkApplicationInfo
            {
                sType = VkStructureType.ApplicationInfo,
                pApplicationName = Marshal.StringToHGlobalAnsi("Ouroboros GPU"),
                applicationVersion = VkMakeVersion(1, 0, 0),
                pEngineName = Marshal.StringToHGlobalAnsi("Ouroboros"),
                engineVersion = VkMakeVersion(1, 0, 0),
                apiVersion = VkApiVersion11
            };

            var pAppInfo = Marshal.AllocHGlobal(Marshal.SizeOf<VkApplicationInfo>());
            Marshal.StructureToPtr(appInfo, pAppInfo, false);
            
            var createInfo = new VkInstanceCreateInfo
            {
                sType = VkStructureType.InstanceCreateInfo,
                pApplicationInfo = pAppInfo,
                enabledExtensionCount = 0,
                ppEnabledExtensionNames = IntPtr.Zero,
                enabledLayerCount = 0,
                ppEnabledLayerNames = IntPtr.Zero
            };

            CheckResult(VulkanAPI.vkCreateInstance(ref createInfo, IntPtr.Zero, out instance));
            
            // Clean up allocated memory
            Marshal.FreeHGlobal(pAppInfo);
            Marshal.FreeHGlobal(appInfo.pApplicationName);
            Marshal.FreeHGlobal(appInfo.pEngineName);
        }

        private void SelectPhysicalDevice()
        {
            uint deviceCount = 0;
            VulkanAPI.vkEnumeratePhysicalDevices(instance, ref deviceCount, IntPtr.Zero);

            if (deviceCount == 0)
                throw new Exception("No Vulkan compatible GPU found");

            var devices = new VkPhysicalDevice[deviceCount];
            VulkanAPI.vkEnumeratePhysicalDevices(instance, ref deviceCount, devices);

            // Select first suitable device
            foreach (var device in devices)
            {
                if (IsDeviceSuitable(device))
                {
                    physicalDevice = device;
                    QueryDeviceInfo();
                    return;
                }
            }

            throw new Exception("No suitable GPU found");
        }

        private bool IsDeviceSuitable(VkPhysicalDevice device)
        {
            var properties = new VkPhysicalDeviceProperties();
            VulkanAPI.vkGetPhysicalDeviceProperties(device, ref properties);

            // Check for compute queue support
            uint queueFamilyCount = 0;
            VulkanAPI.vkGetPhysicalDeviceQueueFamilyProperties(device, ref queueFamilyCount, IntPtr.Zero);

            var queueFamilies = new VkQueueFamilyProperties[queueFamilyCount];
            VulkanAPI.vkGetPhysicalDeviceQueueFamilyProperties(device, ref queueFamilyCount, queueFamilies);

            for (uint i = 0; i < queueFamilyCount; i++)
            {
                if ((queueFamilies[i].queueFlags & VkQueueFlags.Compute) != 0)
                {
                    computeQueueFamilyIndex = i;
                    return true;
                }
            }

            return false;
        }

        private void CreateLogicalDevice()
        {
            var queuePriority = 1.0f;
            var queueCreateInfo = new VkDeviceQueueCreateInfo
            {
                sType = VkStructureType.DeviceQueueCreateInfo,
                queueFamilyIndex = computeQueueFamilyIndex,
                queueCount = 1,
                pQueuePriorities = Marshal.AllocHGlobal(sizeof(float))
            };
            Marshal.WriteInt32(queueCreateInfo.pQueuePriorities, BitConverter.ToInt32(BitConverter.GetBytes(queuePriority), 0));

            var deviceCreateInfo = new VkDeviceCreateInfo
            {
                sType = VkStructureType.DeviceCreateInfo,
                pQueueCreateInfos = Marshal.AllocHGlobal(Marshal.SizeOf<VkDeviceQueueCreateInfo>()),
                queueCreateInfoCount = 1,
                pEnabledFeatures = IntPtr.Zero,
                enabledExtensionCount = 0,
                ppEnabledExtensionNames = IntPtr.Zero,
                enabledLayerCount = 0,
                ppEnabledLayerNames = IntPtr.Zero
            };
            Marshal.StructureToPtr(queueCreateInfo, deviceCreateInfo.pQueueCreateInfos, false);

            CheckResult(VulkanAPI.vkCreateDevice(physicalDevice, ref deviceCreateInfo, IntPtr.Zero, out device));

            // Get compute queue
            VulkanAPI.vkGetDeviceQueue(device, computeQueueFamilyIndex, 0, out computeQueue);

            // Cleanup
            Marshal.FreeHGlobal(queueCreateInfo.pQueuePriorities);
            Marshal.FreeHGlobal(deviceCreateInfo.pQueueCreateInfos);
        }

        private void CreateCommandPool()
        {
            var poolInfo = new VkCommandPoolCreateInfo
            {
                sType = VkStructureType.CommandPoolCreateInfo,
                queueFamilyIndex = computeQueueFamilyIndex,
                flags = VkCommandPoolCreateFlags.ResetCommandBuffer
            };

            CheckResult(VulkanAPI.vkCreateCommandPool(device, ref poolInfo, IntPtr.Zero, out commandPool));
        }

        /// <summary>
        /// Create compute pipeline from SPIR-V bytecode
        /// </summary>
        public VulkanComputePipeline CreateComputePipeline(byte[] spirvCode, string entryPoint)
        {
            // Create shader module
            var shaderModule = CreateShaderModule(spirvCode);

            // Create compute pipeline
            var pipelineLayout = CreatePipelineLayout();
            var pipeline = CreatePipeline(shaderModule, entryPoint, pipelineLayout);

            return new VulkanComputePipeline
            {
                Pipeline = pipeline,
                PipelineLayout = pipelineLayout,
                ShaderModule = shaderModule
            };
        }

        private VkShaderModule CreateShaderModule(byte[] code)
        {
            var createInfo = new VkShaderModuleCreateInfo
            {
                sType = VkStructureType.ShaderModuleCreateInfo,
                codeSize = (uint)code.Length,
                pCode = Marshal.AllocHGlobal(code.Length)
            };
            Marshal.Copy(code, 0, createInfo.pCode, code.Length);

            VkShaderModule shaderModule;
            CheckResult(VulkanAPI.vkCreateShaderModule(device, ref createInfo, IntPtr.Zero, out shaderModule));

            Marshal.FreeHGlobal(createInfo.pCode);
            return shaderModule;
        }

        private VkPipelineLayout CreatePipelineLayout()
        {
            var pipelineLayoutInfo = new VkPipelineLayoutCreateInfo
            {
                sType = VkStructureType.PipelineLayoutCreateInfo,
                setLayoutCount = 0,
                pSetLayouts = IntPtr.Zero,
                pushConstantRangeCount = 0,
                pPushConstantRanges = IntPtr.Zero
            };

            VkPipelineLayout pipelineLayout;
            CheckResult(VulkanAPI.vkCreatePipelineLayout(device, ref pipelineLayoutInfo, IntPtr.Zero, out pipelineLayout));
            return pipelineLayout;
        }

        private VkPipeline CreatePipeline(VkShaderModule shaderModule, string entryPoint, VkPipelineLayout layout)
        {
            var shaderStage = new VkPipelineShaderStageCreateInfo
            {
                sType = VkStructureType.PipelineShaderStageCreateInfo,
                stage = VkShaderStageFlags.Compute,
                module = shaderModule,
                pName = Marshal.StringToHGlobalAnsi(entryPoint),
                pSpecializationInfo = IntPtr.Zero
            };

            var pipelineInfo = new VkComputePipelineCreateInfo
            {
                sType = VkStructureType.ComputePipelineCreateInfo,
                stage = shaderStage,
                layout = layout,
                basePipelineHandle = VkPipeline.Null,
                basePipelineIndex = -1
            };

            VkPipeline pipeline;
            CheckResult(VulkanAPI.vkCreateComputePipelines(device, VkPipelineCache.Null, 1, 
                ref pipelineInfo, IntPtr.Zero, out pipeline));

            Marshal.FreeHGlobal(shaderStage.pName);
            return pipeline;
        }

        /// <summary>
        /// Execute compute pipeline
        /// </summary>
        public void ExecuteCompute(VulkanComputePipeline pipeline, uint groupCountX, uint groupCountY, uint groupCountZ)
        {
            var commandBuffer = AllocateCommandBuffer();

            // Begin command buffer recording
            var beginInfo = new VkCommandBufferBeginInfo
            {
                sType = VkStructureType.CommandBufferBeginInfo,
                flags = VkCommandBufferUsageFlags.OneTimeSubmit
            };
            CheckResult(VulkanAPI.vkBeginCommandBuffer(commandBuffer, ref beginInfo));

            // Bind compute pipeline
            VulkanAPI.vkCmdBindPipeline(commandBuffer, VkPipelineBindPoint.Compute, pipeline.Pipeline);

            // Dispatch compute work
            VulkanAPI.vkCmdDispatch(commandBuffer, groupCountX, groupCountY, groupCountZ);

            // End command buffer recording
            CheckResult(VulkanAPI.vkEndCommandBuffer(commandBuffer));

            // Submit to queue
            var submitInfo = new VkSubmitInfo
            {
                sType = VkStructureType.SubmitInfo,
                commandBufferCount = 1,
                pCommandBuffers = Marshal.AllocHGlobal(IntPtr.Size)
            };
            Marshal.WriteIntPtr(submitInfo.pCommandBuffers, commandBuffer.Handle);

            CheckResult(VulkanAPI.vkQueueSubmit(computeQueue, 1, ref submitInfo, VkFence.Null));
            CheckResult(VulkanAPI.vkQueueWaitIdle(computeQueue));

            Marshal.FreeHGlobal(submitInfo.pCommandBuffers);
            FreeCommandBuffer(commandBuffer);
        }

        private VkCommandBuffer AllocateCommandBuffer()
        {
            var allocInfo = new VkCommandBufferAllocateInfo
            {
                sType = VkStructureType.CommandBufferAllocateInfo,
                commandPool = commandPool,
                level = VkCommandBufferLevel.Primary,
                commandBufferCount = 1
            };

            VkCommandBuffer commandBuffer;
            CheckResult(VulkanAPI.vkAllocateCommandBuffers(device, ref allocInfo, out commandBuffer));
            return commandBuffer;
        }

        private void FreeCommandBuffer(VkCommandBuffer commandBuffer)
        {
            VulkanAPI.vkFreeCommandBuffers(device, commandPool, 1, ref commandBuffer);
        }

        private void QueryDeviceInfo()
        {
            var properties = new VkPhysicalDeviceProperties();
            VulkanAPI.vkGetPhysicalDeviceProperties(physicalDevice, ref properties);

            DeviceInfo = new VulkanDeviceInfo
            {
                DeviceName = System.Text.Encoding.ASCII.GetString(properties.deviceName).TrimEnd('\0'),
                VendorID = properties.vendorID,
                DeviceID = properties.deviceID,
                ApiVersion = properties.apiVersion,
                DriverVersion = properties.driverVersion
            };
        }

        private void CheckResult(VkResult result)
        {
            if (result != VkResult.Success)
                throw new VulkanException(result);
        }

        private uint VkMakeVersion(uint major, uint minor, uint patch)
        {
            return (major << 22) | (minor << 12) | patch;
        }

        private const uint VkApiVersion11 = (1u << 22) | (1u << 12) | 0u;

        public void Dispose()
        {
            if (!disposed)
            {
                if (commandPool.Handle != IntPtr.Zero)
                    VulkanAPI.vkDestroyCommandPool(device, commandPool, IntPtr.Zero);

                if (device.Handle != IntPtr.Zero)
                    VulkanAPI.vkDestroyDevice(device, IntPtr.Zero);

                if (instance.Handle != IntPtr.Zero)
                    VulkanAPI.vkDestroyInstance(instance, IntPtr.Zero);

                disposed = true;
            }
        }
    }

    public class VulkanComputePipeline
    {
        public VkPipeline Pipeline { get; set; }
        public VkPipelineLayout PipelineLayout { get; set; }
        public VkShaderModule ShaderModule { get; set; }
    }

    public class VulkanDeviceInfo
    {
        public string DeviceName { get; set; }
        public uint VendorID { get; set; }
        public uint DeviceID { get; set; }
        public uint ApiVersion { get; set; }
        public uint DriverVersion { get; set; }
    }

    public class VulkanException : Exception
    {
        public VkResult ErrorCode { get; }

        public VulkanException(VkResult errorCode)
            : base($"Vulkan Error: {errorCode}")
        {
            ErrorCode = errorCode;
        }
    }
} 