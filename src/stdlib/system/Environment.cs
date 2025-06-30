using System = global::System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using Ouroboros.Core;

namespace Ouroboros.StdLib.System
{
    /// <summary>
    /// Provides information about and means to manipulate the current environment and platform
    /// </summary>
    public static class Environment
    {
        private static readonly Dictionary<string, string> customVariables = new Dictionary<string, string>();
        
        /// <summary>
        /// Gets the newline string for the current platform
        /// </summary>
        public static string NewLine => System.Environment.NewLine;
        
        /// <summary>
        /// Gets or sets the current directory
        /// </summary>
        public static string CurrentDirectory
        {
            get => System.Environment.CurrentDirectory;
            set => System.Environment.CurrentDirectory = value;
        }
        
        /// <summary>
        /// Gets the NetBIOS name of this local computer
        /// </summary>
        public static string MachineName => System.Environment.MachineName;
        
        /// <summary>
        /// Gets the user name of the current user
        /// </summary>
        public static string UserName => System.Environment.UserName;
        
        /// <summary>
        /// Gets the user's home directory path
        /// </summary>
        public static string UserHomeDirectory => global::System.Environment.GetFolderPath(global::System.Environment.SpecialFolder.UserProfile);
        
        /// <summary>
        /// Gets the operating system version
        /// </summary>
        public static OperatingSystem OSVersion => new OperatingSystem(System.Environment.OSVersion);
        
        /// <summary>
        /// Gets the number of processors
        /// </summary>
        public static int ProcessorCount => System.Environment.ProcessorCount;
        
        /// <summary>
        /// Gets the system directory path
        /// </summary>
        public static string SystemDirectory => System.Environment.SystemDirectory;
        
        /// <summary>
        /// Gets the amount of physical memory mapped to the process context
        /// </summary>
        public static long WorkingSet => System.Environment.WorkingSet;
        
        /// <summary>
        /// Gets the current managed thread ID
        /// </summary>
        public static int CurrentManagedThreadId => System.Environment.CurrentManagedThreadId;
        
        /// <summary>
        /// Gets a value indicating whether the current process is running in 64-bit mode
        /// </summary>
        public static bool Is64BitProcess => System.Environment.Is64BitProcess;
        
        /// <summary>
        /// Gets a value indicating whether the operating system is 64-bit
        /// </summary>
        public static bool Is64BitOperatingSystem => System.Environment.Is64BitOperatingSystem;
        
        /// <summary>
        /// Gets the command line for this process
        /// </summary>
        public static string CommandLine => System.Environment.CommandLine;
        
        /// <summary>
        /// Gets whether the CLR is shutting down
        /// </summary>
        public static bool HasShutdownStarted => System.Environment.HasShutdownStarted;
        
        /// <summary>
        /// Gets the number of milliseconds since the system started
        /// </summary>
        public static int TickCount => System.Environment.TickCount;
        
        /// <summary>
        /// Gets the fully qualified path of the system directory
        /// </summary>
        public static string SystemPageSize => System.Environment.SystemPageSize.ToString();
        
        /// <summary>
        /// Get environment variable
        /// </summary>
        public static string GetEnvironmentVariable(string name, EnvironmentVariableTarget target = EnvironmentVariableTarget.Process)
        {
            if (target == EnvironmentVariableTarget.Custom)
            {
                return customVariables.TryGetValue(name, out var value) ? value : null;
            }
            
            return System.Environment.GetEnvironmentVariable(name, (System.EnvironmentVariableTarget)target);
        }
        
        /// <summary>
        /// Set environment variable
        /// </summary>
        public static void SetEnvironmentVariable(string name, string value, EnvironmentVariableTarget target = EnvironmentVariableTarget.Process)
        {
            if (target == EnvironmentVariableTarget.Custom)
            {
                if (value == null)
                    customVariables.Remove(name);
                else
                    customVariables[name] = value;
                return;
            }
            
            System.Environment.SetEnvironmentVariable(name, value, (System.EnvironmentVariableTarget)target);
        }
        
        /// <summary>
        /// Get all environment variables
        /// </summary>
        public static Dictionary<string, string> GetEnvironmentVariables(EnvironmentVariableTarget target = EnvironmentVariableTarget.Process)
        {
            if (target == EnvironmentVariableTarget.Custom)
            {
                return new Dictionary<string, string>(customVariables);
            }
            
            var result = new Dictionary<string, string>();
            var variables = System.Environment.GetEnvironmentVariables((System.EnvironmentVariableTarget)target);
            
            foreach (var entry in (variables as global::System.Collections.IDictionary))
            {
                if (entry is global::System.Collections.DictionaryEntry dictEntry)
                {
                    result[dictEntry.Key.ToString()] = dictEntry.Value?.ToString();
                }
            }
            
            return result;
        }
        
        /// <summary>
        /// Get command line arguments
        /// </summary>
        public static string[] GetCommandLineArgs()
        {
            return System.Environment.GetCommandLineArgs();
        }
        
        /// <summary>
        /// Exit the process
        /// </summary>
        public static void Exit(int exitCode)
        {
            System.Environment.Exit(exitCode);
        }
        
        /// <summary>
        /// Immediately terminates a process after writing to event log
        /// </summary>
        public static void FailFast(string message, Exception exception = null)
        {
            System.Environment.FailFast(message, exception);
        }
        
        /// <summary>
        /// Expand environment variables in string
        /// </summary>
        public static string ExpandEnvironmentVariables(string text)
        {
            // First expand system variables
            var expanded = System.Environment.ExpandEnvironmentVariables(text);
            
            // Then expand custom variables
            foreach (var kvp in customVariables)
            {
                expanded = expanded.Replace($"%{kvp.Key}%", kvp.Value);
                expanded = expanded.Replace($"${{{kvp.Key}}}", kvp.Value);
            }
            
            return expanded;
        }
        
        /// <summary>
        /// Get folder path for special folder
        /// </summary>
        public static string GetFolderPath(SpecialFolder folder, SpecialFolderOption option = SpecialFolderOption.None)
        {
            // Map Ouroboros SpecialFolder to BCL SpecialFolder
            var bclFolder = (global::System.Environment.SpecialFolder)(int)folder;
            
            if (option == SpecialFolderOption.None)
            {
                return global::System.Environment.GetFolderPath(bclFolder);
            }
            else
            {
                var bclOption = (global::System.Environment.SpecialFolderOption)(int)option;
                return global::System.Environment.GetFolderPath(bclFolder, bclOption);
            }
        }
        
        /// <summary>
        /// Get logical drives
        /// </summary>
        public static string[] GetLogicalDrives()
        {
            return System.Environment.GetLogicalDrives();
        }
        
        /// <summary>
        /// Get the path to the executable that started the process
        /// </summary>
        public static string ProcessPath => Process.GetCurrentProcess().MainModule?.FileName;
        
        /// <summary>
        /// Get the process ID
        /// </summary>
        public static int ProcessId => Process.GetCurrentProcess().Id;
        
        /// <summary>
        /// Get available memory in bytes
        /// </summary>
        public static long AvailableMemory
        {
            get
            {
                var process = Process.GetCurrentProcess();
                return process.PrivateMemorySize64;
            }
        }
        
        /// <summary>
        /// Get total memory in bytes
        /// </summary>
        public static long TotalMemory => GC.GetTotalMemory(false);
        
        /// <summary>
        /// Get environment info as formatted string
        /// </summary>
        public static string GetEnvironmentInfo()
        {
            var info = new global::System.Text.StringBuilder();
            
            info.AppendLine("=== Environment Information ===");
            info.AppendLine($"Machine Name: {MachineName}");
            info.AppendLine($"User Name: {UserName}");
            info.AppendLine($"OS Version: {OSVersion}");
            info.AppendLine($"Processor Count: {ProcessorCount}");
            info.AppendLine($"64-bit OS: {Is64BitOperatingSystem}");
            info.AppendLine($"64-bit Process: {Is64BitProcess}");
            info.AppendLine($"Current Directory: {CurrentDirectory}");
            info.AppendLine($"System Directory: {SystemDirectory}");
            info.AppendLine($"Process ID: {ProcessId}");
            info.AppendLine($"Working Set: {WorkingSet / (1024 * 1024)} MB");
            info.AppendLine($"Available Memory: {AvailableMemory / (1024 * 1024)} MB");
            
            return info.ToString();
        }
        
        /// <summary>
        /// Check if running on Windows
        /// </summary>
        public static bool IsWindows => global::System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(global::System.Runtime.InteropServices.OSPlatform.Windows);
        
        /// <summary>
        /// Check if running on Linux
        /// </summary>
        public static bool IsLinux => global::System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(global::System.Runtime.InteropServices.OSPlatform.Linux);
        
        /// <summary>
        /// Check if running on macOS
        /// </summary>
        public static bool IsMacOS => global::System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(global::System.Runtime.InteropServices.OSPlatform.OSX);
        
        /// <summary>
        /// Get platform name
        /// </summary>
        public static string PlatformName
        {
            get
            {
                if (IsWindows) return "Windows";
                if (IsLinux) return "Linux";
                if (IsMacOS) return "macOS";
                return "Unknown";
            }
        }
        
        /// <summary>
        /// Set working directory
        /// </summary>
        public static void SetWorkingDirectory(string path)
        {
            if (!Directory.Exists(path))
            {
                throw new DirectoryNotFoundException($"Directory not found: {path}");
            }
            
            CurrentDirectory = path;
        }
        
        /// <summary>
        /// Get stack trace
        /// </summary>
        public static string GetStackTrace()
        {
            return global::System.Environment.StackTrace;
        }
        
        /// <summary>
        /// Add a directory to PATH
        /// </summary>
        public static void AddToPath(string directory)
        {
            var path = GetEnvironmentVariable("PATH") ?? "";
            var separator = IsWindows ? ";" : ":";
            
            if (!path.Split(separator[0]).Contains(directory))
            {
                path = string.IsNullOrEmpty(path) ? directory : $"{path}{separator}{directory}";
                SetEnvironmentVariable("PATH", path);
            }
        }
        
        /// <summary>
        /// Remove a directory from PATH
        /// </summary>
        public static void RemoveFromPath(string directory)
        {
            var path = GetEnvironmentVariable("PATH") ?? "";
            var separator = IsWindows ? ";" : ":";
            
            var paths = path.Split(separator[0]).Where(p => p != directory);
            SetEnvironmentVariable("PATH", string.Join(separator, paths));
        }
    }
    
    /// <summary>
    /// Operating system information
    /// </summary>
    public class OperatingSystem
    {
        private readonly System.OperatingSystem os;
        
        internal OperatingSystem(System.OperatingSystem os)
        {
            this.os = os;
        }
        
        public PlatformID Platform => (PlatformID)os.Platform;
        public Version Version => os.Version;
        public string ServicePack => os.ServicePack;
        public string VersionString => os.VersionString;
        
        public override string ToString() => os.ToString();
    }
    
    /// <summary>
    /// Platform ID enumeration
    /// </summary>
    public enum PlatformID
    {
        Win32S = 0,
        Win32Windows = 1,
        Win32NT = 2,
        WinCE = 3,
        Unix = 4,
        Xbox = 5,
        MacOSX = 6,
        Other = 7
    }
    
    /// <summary>
    /// Environment variable target
    /// </summary>
    public enum EnvironmentVariableTarget
    {
        Process = 0,
        User = 1,
        Machine = 2,
        Custom = 3  // Ouroboros-specific
    }
    
    /// <summary>
    /// Special folder enumeration
    /// </summary>
    public enum SpecialFolder
    {
        Desktop = 0,
        Programs = 2,
        MyDocuments = 5,
        Personal = 5,
        Favorites = 6,
        Startup = 7,
        Recent = 8,
        SendTo = 9,
        StartMenu = 11,
        MyMusic = 13,
        MyVideos = 14,
        DesktopDirectory = 16,
        MyComputer = 17,
        NetworkShortcuts = 19,
        Fonts = 20,
        Templates = 21,
        CommonStartMenu = 22,
        CommonPrograms = 23,
        CommonStartup = 24,
        CommonDesktopDirectory = 25,
        ApplicationData = 26,
        PrinterShortcuts = 27,
        LocalApplicationData = 28,
        InternetCache = 32,
        Cookies = 33,
        History = 34,
        CommonApplicationData = 35,
        Windows = 36,
        System = 37,
        ProgramFiles = 38,
        MyPictures = 39,
        UserProfile = 40,
        SystemX86 = 41,
        ProgramFilesX86 = 42,
        CommonProgramFiles = 43,
        CommonProgramFilesX86 = 44,
        CommonTemplates = 45,
        CommonDocuments = 46,
        CommonAdminTools = 47,
        AdminTools = 48,
        CommonMusic = 53,
        CommonPictures = 54,
        CommonVideos = 55,
        Resources = 56,
        LocalizedResources = 57,
        CommonOemLinks = 58,
        CDBurning = 59
    }
    
    /// <summary>
    /// Special folder option
    /// </summary>
    public enum SpecialFolderOption
    {
        None = 0,
        Create = 32768,
        DoNotVerify = 16384
    }
} 