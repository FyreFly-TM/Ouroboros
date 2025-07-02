using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace Ouro.GPU.CUDA
{
    /// <summary>
    /// CUDA kernel compiler and PTX generation
    /// </summary>
    public class CudaCompiler
    {
        private readonly string nvccPath;
        private readonly CompilerOptions defaultOptions;

        public CudaCompiler(string nvccPath = null)
        {
            this.nvccPath = nvccPath ?? FindNvcc();
            this.defaultOptions = new CompilerOptions
            {
                Architecture = "sm_75",
                OptimizationLevel = OptimizationLevel.O2,
                GenerateDebugInfo = false
            };
        }

        /// <summary>
        /// Compile Ouroboros GPU kernel to PTX
        /// </summary>
        public CompiledPTX CompileKernel(string source, string entryPoint, CompilerOptions options = null)
        {
            options ??= defaultOptions;

            // Transform Ouroboros kernel to CUDA C++
            var cudaSource = TransformToCuda(source, entryPoint);

            // Write to temporary file
            var tempDir = Path.GetTempPath();
            var sourceFile = Path.Combine(tempDir, $"kernel_{Guid.NewGuid()}.cu");
            var ptxFile = Path.Combine(tempDir, $"kernel_{Guid.NewGuid()}.ptx");

            try
            {
                File.WriteAllText(sourceFile, cudaSource);

                // Compile to PTX
                var ptx = CompileToPTX(sourceFile, ptxFile, options);

                return new CompiledPTX
                {
                    PTXCode = ptx,
                    EntryPoint = entryPoint,
                    CompileTime = DateTime.Now,
                    Options = options
                };
            }
            finally
            {
                // Cleanup temp files
                if (File.Exists(sourceFile)) File.Delete(sourceFile);
                if (File.Exists(ptxFile)) File.Delete(ptxFile);
            }
        }

        /// <summary>
        /// Transform Ouroboros kernel syntax to CUDA C++
        /// </summary>
        private string TransformToCuda(string source, string entryPoint)
        {
            var sb = new StringBuilder();

            // Add CUDA headers
            sb.AppendLine("#include <cuda_runtime.h>");
            sb.AppendLine("#include <device_launch_parameters.h>");
            sb.AppendLine();

            // Parse and transform kernel
            var lines = source.Split('\n');
            bool inKernel = false;
            var kernelSignature = "";

            foreach (var line in lines)
            {
                var trimmed = line.Trim();

                // Detect kernel declaration
                if (trimmed.StartsWith("@gpu") || trimmed.StartsWith("kernel"))
                {
                    inKernel = true;
                    continue;
                }

                if (inKernel && trimmed.Contains(entryPoint))
                {
                    // Transform kernel signature
                    kernelSignature = TransformKernelSignature(trimmed);
                    sb.AppendLine($"extern \"C\" __global__ {kernelSignature}");
                    sb.AppendLine("{");
                    continue;
                }

                if (inKernel)
                {
                    // Transform kernel body
                    var transformed = TransformKernelLine(trimmed);
                    sb.AppendLine($"    {transformed}");

                    if (trimmed == "}")
                    {
                        inKernel = false;
                    }
                }
            }

            return sb.ToString();
        }

        private string TransformKernelSignature(string signature)
        {
            // Transform Ouroboros kernel signature to CUDA
            // Example: "function vectorAdd(a: float*, b: float*, c: float*, n: int)"
            // To: "void vectorAdd(float* a, float* b, float* c, int n)"

            var match = Regex.Match(signature, @"function\s+(\w+)\s*\((.*?)\)");
            if (!match.Success) return signature;

            var name = match.Groups[1].Value;
            var params_ = match.Groups[2].Value;

            var transformedParams = TransformParameters(params_);
            return $"void {name}({transformedParams})";
        }

        private string TransformParameters(string params_)
        {
            var parameters = params_.Split(',');
            var transformed = new List<string>();

            foreach (var param in parameters)
            {
                var parts = param.Trim().Split(':');
                if (parts.Length == 2)
                {
                    var name = parts[0].Trim();
                    var type = TransformType(parts[1].Trim());
                    transformed.Add($"{type} {name}");
                }
            }

            return string.Join(", ", transformed);
        }

        private string TransformType(string ouroborosType)
        {
            return ouroborosType switch
            {
                "int" => "int",
                "float" => "float",
                "double" => "double",
                "bool" => "bool",
                "int*" => "int*",
                "float*" => "float*",
                "double*" => "double*",
                "global float*" => "float*",
                "global int*" => "int*",
                "shared float*" => "__shared__ float*",
                "shared int*" => "__shared__ int*",
                _ => ouroborosType
            };
        }

        private string TransformKernelLine(string line)
        {
            // Transform Ouroboros kernel syntax to CUDA
            line = Regex.Replace(line, @"let\s+(\w+)\s*=", "$1 =");
            line = Regex.Replace(line, @"getGlobalId\((\d+)\)", "blockIdx.$1 * blockDim.$1 + threadIdx.$1");
            line = Regex.Replace(line, @"getLocalId\((\d+)\)", "threadIdx.$1");
            line = Regex.Replace(line, @"getGroupId\((\d+)\)", "blockIdx.$1");
            line = Regex.Replace(line, @"getGlobalSize\((\d+)\)", "gridDim.$1 * blockDim.$1");
            line = Regex.Replace(line, @"getLocalSize\((\d+)\)", "blockDim.$1");
            line = Regex.Replace(line, @"barrier\(\)", "__syncthreads()");

            return line;
        }

        private string CompileToPTX(string sourceFile, string ptxFile, CompilerOptions options)
        {
            var args = new List<string>
            {
                "-ptx",
                $"-arch={options.Architecture}",
                $"-{options.OptimizationLevel.ToString().ToLower()}",
                "-o", ptxFile,
                sourceFile
            };

            if (options.GenerateDebugInfo)
            {
                args.Add("-g");
                args.Add("-G");
            }

            if (options.FastMath)
            {
                args.Add("-use_fast_math");
            }

            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = nvccPath,
                    Arguments = string.Join(" ", args),
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                }
            };

            process.Start();
            var output = process.StandardOutput.ReadToEnd();
            var error = process.StandardError.ReadToEnd();
            process.WaitForExit();

            if (process.ExitCode != 0)
            {
                throw new CudaCompilationException($"NVCC compilation failed: {error}");
            }

            return File.ReadAllText(ptxFile);
        }

        private string FindNvcc()
        {
            // Try common CUDA installation paths
            var paths = new[]
            {
                @"C:\Program Files\NVIDIA GPU Computing Toolkit\CUDA\v11.0\bin\nvcc.exe",
                @"C:\Program Files\NVIDIA GPU Computing Toolkit\CUDA\v12.0\bin\nvcc.exe",
                "/usr/local/cuda/bin/nvcc",
                "/opt/cuda/bin/nvcc"
            };

            foreach (var path in paths)
            {
                if (File.Exists(path))
                    return path;
            }

            // Try PATH environment variable
            var envPath = Environment.GetEnvironmentVariable("PATH");
            if (envPath != null)
            {
                foreach (var dir in envPath.Split(Path.PathSeparator))
                {
                    var nvcc = Path.Combine(dir, "nvcc.exe");
                    if (File.Exists(nvcc))
                        return nvcc;
                }
            }

            throw new FileNotFoundException("NVCC compiler not found. Please install CUDA toolkit.");
        }
    }

    public class CompiledPTX
    {
        public string PTXCode { get; set; }
        public string EntryPoint { get; set; }
        public DateTime CompileTime { get; set; }
        public CompilerOptions Options { get; set; }
    }

    public class CompilerOptions
    {
        public string Architecture { get; set; } = "sm_75";
        public OptimizationLevel OptimizationLevel { get; set; } = OptimizationLevel.O2;
        public bool GenerateDebugInfo { get; set; } = false;
        public bool FastMath { get; set; } = true;
        public List<string> IncludePaths { get; set; } = new();
        public List<string> Defines { get; set; } = new();
    }

    public enum OptimizationLevel
    {
        O0, // No optimization
        O1, // Basic optimization
        O2, // Default optimization
        O3  // Aggressive optimization
    }

    public class CudaCompilationException : Exception
    {
        public CudaCompilationException(string message) : base(message) { }
    }
} 