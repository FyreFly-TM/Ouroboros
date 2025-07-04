using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Ouro.StdLib.IO
{
    /// <summary>
    /// File system operations for Ouroboros
    /// </summary>
    public static class FileSystem
    {
        /// <summary>
        /// Read entire file contents as text
        /// </summary>
        public static string ReadText(string path)
        {
            return File.ReadAllText(path);
        }
        
        /// <summary>
        /// Read entire file contents as text asynchronously
        /// </summary>
        public static async Task<string> ReadTextAsync(string path)
        {
            using (var reader = new StreamReader(path))
            {
                return await reader.ReadToEndAsync();
            }
        }
        
        /// <summary>
        /// Write text to file
        /// </summary>
        public static void WriteText(string path, string contents)
        {
            File.WriteAllText(path, contents);
        }
        
        /// <summary>
        /// Write text to file asynchronously
        /// </summary>
        public static async Task WriteTextAsync(string path, string contents)
        {
            using (var writer = new StreamWriter(path))
            {
                await writer.WriteAsync(contents);
            }
        }
        
        /// <summary>
        /// Read file lines
        /// </summary>
        public static string[] ReadLines(string path)
        {
            return File.ReadAllLines(path);
        }
        
        /// <summary>
        /// Read file lines asynchronously
        /// </summary>
        public static async Task<List<string>> ReadLinesAsync(string path)
        {
            var lines = new List<string>();
            using (var reader = new StreamReader(path))
            {
                string? line;
                while ((line = await reader.ReadLineAsync()) != null)
                {
                    lines.Add(line);
                }
            }
            return lines;
        }
        
        /// <summary>
        /// Read file as bytes
        /// </summary>
        public static byte[] ReadBytes(string path)
        {
            return File.ReadAllBytes(path);
        }
        
        /// <summary>
        /// Write bytes to file
        /// </summary>
        public static void WriteBytes(string path, byte[] bytes)
        {
            File.WriteAllBytes(path, bytes);
        }
        
        /// <summary>
        /// Check if file exists
        /// </summary>
        public static bool FileExists(string path)
        {
            return File.Exists(path);
        }
        
        /// <summary>
        /// Check if directory exists
        /// </summary>
        public static bool DirectoryExists(string path)
        {
            return Directory.Exists(path);
        }
        
        /// <summary>
        /// Create directory
        /// </summary>
        public static void CreateDirectory(string path)
        {
            Directory.CreateDirectory(path);
        }
        
        /// <summary>
        /// Delete file
        /// </summary>
        public static void DeleteFile(string path)
        {
            File.Delete(path);
        }
        
        /// <summary>
        /// Delete directory
        /// </summary>
        public static void DeleteDirectory(string path, bool recursive = false)
        {
            Directory.Delete(path, recursive);
        }
        
        /// <summary>
        /// Copy file
        /// </summary>
        public static void CopyFile(string source, string destination, bool overwrite = false)
        {
            File.Copy(source, destination, overwrite);
        }
        
        /// <summary>
        /// Move file
        /// </summary>
        public static void MoveFile(string source, string destination)
        {
            File.Move(source, destination);
        }
        
        /// <summary>
        /// Get file info
        /// </summary>
        public static FileInfo GetFileInfo(string path)
        {
            return new FileInfo(path);
        }
        
        /// <summary>
        /// Get directory info
        /// </summary>
        public static DirectoryInfo GetDirectoryInfo(string path)
        {
            return new DirectoryInfo(path);
        }
        
        /// <summary>
        /// List files in directory
        /// </summary>
        public static string[] ListFiles(string path, string pattern = "*", bool recursive = false)
        {
            var searchOption = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
            return Directory.GetFiles(path, pattern, searchOption);
        }
        
        /// <summary>
        /// List directories
        /// </summary>
        public static string[] ListDirectories(string path, string pattern = "*", bool recursive = false)
        {
            var searchOption = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
            return Directory.GetDirectories(path, pattern, searchOption);
        }
        
        /// <summary>
        /// Get current directory
        /// </summary>
        public static string GetCurrentDirectory()
        {
            return Directory.GetCurrentDirectory();
        }
        
        /// <summary>
        /// Set current directory
        /// </summary>
        public static void SetCurrentDirectory(string path)
        {
            Directory.SetCurrentDirectory(path);
        }
        
        /// <summary>
        /// Get temp file path
        /// </summary>
        public static string GetTempFileName()
        {
            return Path.GetTempFileName();
        }
        
        /// <summary>
        /// Get temp directory path
        /// </summary>
        public static string GetTempPath()
        {
            return Path.GetTempPath();
        }
        
        /// <summary>
        /// Join path parts
        /// </summary>
        public static string JoinPath(params string[] paths)
        {
            return Path.Combine(paths);
        }
        
        /// <summary>
        /// Get file name from path
        /// </summary>
        public static string GetFileName(string path)
        {
            return Path.GetFileName(path);
        }
        
        /// <summary>
        /// Get file name without extension
        /// </summary>
        public static string GetFileNameWithoutExtension(string path)
        {
            return Path.GetFileNameWithoutExtension(path);
        }
        
        /// <summary>
        /// Get file extension
        /// </summary>
        public static string GetExtension(string path)
        {
            return Path.GetExtension(path);
        }
        
        /// <summary>
        /// Get directory name from path
        /// </summary>
        public static string GetDirectoryName(string path)
        {
            return Path.GetDirectoryName(path) ?? string.Empty;
        }
        
        /// <summary>
        /// Get absolute path
        /// </summary>
        public static string GetAbsolutePath(string path)
        {
            return Path.GetFullPath(path);
        }
    }
    
    /// <summary>
    /// File watcher for monitoring file system changes
    /// </summary>
    public class FileWatcher : IDisposable
    {
        private FileSystemWatcher watcher;
        
        public event EventHandler<FileSystemEventArgs>? Changed;
        public event EventHandler<FileSystemEventArgs>? Created;
        public event EventHandler<FileSystemEventArgs>? Deleted;
        public event EventHandler<RenamedEventArgs>? Renamed;
        public event EventHandler<ErrorEventArgs>? Error;
        
        public FileWatcher(string path, string filter = "*")
        {
            watcher = new FileSystemWatcher(path, filter);
            
            watcher.Changed += (s, e) => Changed?.Invoke(this, e);
            watcher.Created += (s, e) => Created?.Invoke(this, e);
            watcher.Deleted += (s, e) => Deleted?.Invoke(this, e);
            watcher.Renamed += (s, e) => Renamed?.Invoke(this, e);
            watcher.Error += (s, e) => Error?.Invoke(this, e);
        }
        
        public bool EnableRaisingEvents
        {
            get => watcher.EnableRaisingEvents;
            set => watcher.EnableRaisingEvents = value;
        }
        
        public bool IncludeSubdirectories
        {
            get => watcher.IncludeSubdirectories;
            set => watcher.IncludeSubdirectories = value;
        }
        
        public NotifyFilters NotifyFilter
        {
            get => watcher.NotifyFilter;
            set => watcher.NotifyFilter = value;
        }
        
        public void Start()
        {
            watcher.EnableRaisingEvents = true;
        }
        
        public void Stop()
        {
            watcher.EnableRaisingEvents = false;
        }
        
        public void Dispose()
        {
            watcher?.Dispose();
        }
    }
    
    /// <summary>
    /// Stream utilities
    /// </summary>
    public static class StreamUtils
    {
        /// <summary>
        /// Copy stream to another stream
        /// </summary>
        public static void CopyTo(Stream source, Stream destination, int bufferSize = 81920)
        {
            byte[] buffer = new byte[bufferSize];
            int read;
            while ((read = source.Read(buffer, 0, buffer.Length)) != 0)
            {
                destination.Write(buffer, 0, read);
            }
        }
        
        /// <summary>
        /// Copy stream to another stream asynchronously
        /// </summary>
        public static async Task CopyToAsync(Stream source, Stream destination, int bufferSize = 81920)
        {
            byte[] buffer = new byte[bufferSize];
            int read;
            while ((read = await source.ReadAsync(buffer, 0, buffer.Length)) != 0)
            {
                await destination.WriteAsync(buffer, 0, read);
            }
        }
        
        /// <summary>
        /// Read all bytes from stream
        /// </summary>
        public static byte[] ReadAllBytes(Stream stream)
        {
            using (var ms = new MemoryStream())
            {
                stream.CopyTo(ms);
                return ms.ToArray();
            }
        }
        
        /// <summary>
        /// Read all bytes from stream asynchronously
        /// </summary>
        public static async Task<byte[]> ReadAllBytesAsync(Stream stream)
        {
            using (var ms = new MemoryStream())
            {
                await stream.CopyToAsync(ms);
                return ms.ToArray();
            }
        }
    }
}