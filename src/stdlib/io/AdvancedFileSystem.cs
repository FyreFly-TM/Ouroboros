using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Text;

namespace Ouro.StdLib.IO
{
    /// <summary>
    /// Advanced file system operations
    /// </summary>
    public static class AdvancedFileSystem
    {
        /// <summary>
        /// Read file async with retry logic
        /// </summary>
        public static async Task<string> ReadFileAsync(string path, int maxRetries = 3, int delayMs = 100)
        {
            for (int i = 0; i <= maxRetries; i++)
            {
                try
                {
                    return await File.ReadAllTextAsync(path);
                }
                catch (IOException) when (i < maxRetries)
                {
                    await Task.Delay(delayMs * (i + 1));
                }
            }
            throw new IOException($"Failed to read file after {maxRetries} attempts: {path}");
        }

        /// <summary>
        /// Write file async with retry logic
        /// </summary>
        public static async Task WriteFileAsync(string path, string content, int maxRetries = 3, int delayMs = 100)
        {
            for (int i = 0; i <= maxRetries; i++)
            {
                try
                {
                    await File.WriteAllTextAsync(path, content);
                    return;
                }
                catch (IOException) when (i < maxRetries)
                {
                    await Task.Delay(delayMs * (i + 1));
                }
            }
            throw new IOException($"Failed to write file after {maxRetries} attempts: {path}");
        }

        /// <summary>
        /// Read file in chunks
        /// </summary>
        public static async IAsyncEnumerable<string> ReadLinesAsync(string path)
        {
            using var reader = new StreamReader(path);
            string? line;
            while ((line = await reader.ReadLineAsync()) != null)
            {
                yield return line;
            }
        }

        /// <summary>
        /// Read binary file in chunks
        /// </summary>
        public static async IAsyncEnumerable<byte[]> ReadChunksAsync(string path, int chunkSize = 4096)
        {
            using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: chunkSize, useAsync: true);
            var buffer = new byte[chunkSize];
            int bytesRead;
            
            while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
            {
                if (bytesRead == chunkSize)
                {
                    yield return buffer;
                }
                else
                {
                    var partial = new byte[bytesRead];
                    Array.Copy(buffer, partial, bytesRead);
                    yield return partial;
                }
            }
        }

        /// <summary>
        /// Copy file with progress reporting
        /// </summary>
        public static async Task CopyFileAsync(string source, string destination, IProgress<long>? progress = null)
        {
            const int bufferSize = 81920; // 80KB
            
            using var sourceStream = new FileStream(source, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize, useAsync: true);
            using var destStream = new FileStream(destination, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize, useAsync: true);
            
            var buffer = new byte[bufferSize];
            long totalBytes = 0;
            int bytesRead;
            
            while ((bytesRead = await sourceStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
            {
                await destStream.WriteAsync(buffer, 0, bytesRead);
                totalBytes += bytesRead;
                progress?.Report(totalBytes);
            }
        }

        /// <summary>
        /// Move file with atomic operation
        /// </summary>
        public static void MoveFileAtomic(string source, string destination)
        {
            // Try atomic move first
            try
            {
                File.Move(source, destination, overwrite: true);
            }
            catch
            {
                // Fallback to copy and delete
                File.Copy(source, destination, overwrite: true);
                File.Delete(source);
            }
        }

        /// <summary>
        /// Create directory recursively
        /// </summary>
        public static void CreateDirectory(string path)
        {
            Directory.CreateDirectory(path);
        }

        /// <summary>
        /// Delete directory recursively with retry
        /// </summary>
        public static void DeleteDirectory(string path, bool recursive = true, int maxRetries = 3)
        {
            for (int i = 0; i <= maxRetries; i++)
            {
                try
                {
                    if (Directory.Exists(path))
                    {
                        Directory.Delete(path, recursive);
                    }
                    return;
                }
                catch (IOException) when (i < maxRetries)
                {
                    System.Threading.Thread.Sleep(100 * (i + 1));
                }
            }
        }

        /// <summary>
        /// Get directory size
        /// </summary>
        public static long GetDirectorySize(string path)
        {
            var dir = new DirectoryInfo(path);
            return dir.EnumerateFiles("*", SearchOption.AllDirectories).Sum(file => file.Length);
        }

        /// <summary>
        /// Find files matching pattern
        /// </summary>
        public static IEnumerable<string> FindFiles(string path, string pattern, bool recursive = true)
        {
            var option = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
            return Directory.EnumerateFiles(path, pattern, option);
        }

        /// <summary>
        /// Watch directory for changes
        /// </summary>
        public static FileSystemWatcher WatchDirectory(string path, Action<FileSystemEventArgs> onChange)
        {
            var watcher = new FileSystemWatcher(path)
            {
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName,
                EnableRaisingEvents = true
            };
            
            watcher.Changed += (sender, e) => onChange(e);
            watcher.Created += (sender, e) => onChange(e);
            watcher.Deleted += (sender, e) => onChange(e);
            watcher.Renamed += (sender, e) => onChange(e);
            
            return watcher;
        }

        /// <summary>
        /// Create temporary file
        /// </summary>
        public static string CreateTempFile(string? extension = null)
        {
            var tempPath = Path.GetTempFileName();
            
            if (!string.IsNullOrEmpty(extension))
            {
                var newPath = Path.ChangeExtension(tempPath, extension);
                File.Move(tempPath, newPath);
                return newPath;
            }
            
            return tempPath;
        }

        /// <summary>
        /// Create temporary directory
        /// </summary>
        public static string CreateTempDirectory()
        {
            var tempPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(tempPath);
            return tempPath;
        }

        /// <summary>
        /// Get file metadata
        /// </summary>
        public static FileMetadata GetFileMetadata(string path)
        {
            var info = new FileInfo(path);
            return new FileMetadata
            {
                Path = path,
                Name = info.Name,
                Extension = info.Extension,
                Size = info.Length,
                CreatedTime = info.CreationTimeUtc,
                ModifiedTime = info.LastWriteTimeUtc,
                AccessedTime = info.LastAccessTimeUtc,
                IsReadOnly = info.IsReadOnly,
                Attributes = info.Attributes
            };
        }

        /// <summary>
        /// Compare files for equality
        /// </summary>
        public static async Task<bool> AreFilesEqualAsync(string path1, string path2)
        {
            var info1 = new FileInfo(path1);
            var info2 = new FileInfo(path2);
            
            // Quick checks
            if (info1.Length != info2.Length)
                return false;
                
            const int bufferSize = 4096;
            using var stream1 = new FileStream(path1, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize, useAsync: true);
            using var stream2 = new FileStream(path2, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize, useAsync: true);
            
            var buffer1 = new byte[bufferSize];
            var buffer2 = new byte[bufferSize];
            
            while (true)
            {
                var bytes1 = await stream1.ReadAsync(buffer1, 0, bufferSize);
                var bytes2 = await stream2.ReadAsync(buffer2, 0, bufferSize);
                
                if (bytes1 != bytes2)
                    return false;
                    
                if (bytes1 == 0)
                    return true;
                    
                if (!buffer1.Take(bytes1).SequenceEqual(buffer2.Take(bytes2)))
                    return false;
            }
        }

        /// <summary>
        /// Lock file for exclusive access
        /// </summary>
        public static FileLock LockFile(string path, bool shared = false)
        {
            return new FileLock(path, shared);
        }
    }

    /// <summary>
    /// File metadata
    /// </summary>
    public class FileMetadata
    {
        public string Path { get; set; } = "";
        public string Name { get; set; } = "";
        public string Extension { get; set; } = "";
        public long Size { get; set; }
        public DateTime CreatedTime { get; set; }
        public DateTime ModifiedTime { get; set; }
        public DateTime AccessedTime { get; set; }
        public bool IsReadOnly { get; set; }
        public FileAttributes Attributes { get; set; }
    }

    /// <summary>
    /// File lock for exclusive access
    /// </summary>
    public class FileLock : IDisposable
    {
        private readonly FileStream stream;
        private bool disposed = false;

        public FileLock(string path, bool shared)
        {
            var share = shared ? FileShare.Read : FileShare.None;
            stream = new FileStream(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, share);
        }

        public void Dispose()
        {
            if (!disposed)
            {
                stream?.Dispose();
                disposed = true;
            }
        }
    }

    /// <summary>
    /// Memory-mapped file operations
    /// </summary>
    public static class MemoryMappedFiles
    {
        /// <summary>
        /// Read large file using memory mapping
        /// </summary>
        public static byte[] ReadLargeFile(string path)
        {
            using var mmf = System.IO.MemoryMappedFiles.MemoryMappedFile.CreateFromFile(path, FileMode.Open);
            using var accessor = mmf.CreateViewAccessor();
            
            var bytes = new byte[accessor.Capacity];
            accessor.ReadArray(0, bytes, 0, bytes.Length);
            return bytes;
        }

        /// <summary>
        /// Create shared memory region
        /// </summary>
        public static System.IO.MemoryMappedFiles.MemoryMappedFile CreateSharedMemory(string name, long size)
        {
            return System.IO.MemoryMappedFiles.MemoryMappedFile.CreateNew(name, size);
        }
    }

    /// <summary>
    /// CSV file operations
    /// </summary>
    public static class CsvOperations
    {
        /// <summary>
        /// Read CSV file
        /// </summary>
        public static async IAsyncEnumerable<string[]> ReadCsvAsync(string path, char delimiter = ',')
        {
            await foreach (var line in AdvancedFileSystem.ReadLinesAsync(path))
            {
                yield return ParseCsvLine(line, delimiter);
            }
        }

        /// <summary>
        /// Write CSV file
        /// </summary>
        public static async Task WriteCsvAsync(string path, IEnumerable<string[]> rows, char delimiter = ',')
        {
            using var writer = new StreamWriter(path);
            foreach (var row in rows)
            {
                await writer.WriteLineAsync(string.Join(delimiter, row.Select(EscapeCsvField)));
            }
        }

        private static string[] ParseCsvLine(string line, char delimiter)
        {
            var fields = new List<string>();
            var currentField = new StringBuilder();
            bool inQuotes = false;
            
            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                
                if (c == '"')
                {
                    if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                    {
                        currentField.Append('"');
                        i++; // Skip next quote
                    }
                    else
                    {
                        inQuotes = !inQuotes;
                    }
                }
                else if (c == delimiter && !inQuotes)
                {
                    fields.Add(currentField.ToString());
                    currentField.Clear();
                }
                else
                {
                    currentField.Append(c);
                }
            }
            
            fields.Add(currentField.ToString());
            return fields.ToArray();
        }

        private static string EscapeCsvField(string field)
        {
            if (field.Contains(',') || field.Contains('"') || field.Contains('\n'))
            {
                return $"\"{field.Replace("\"", "\"\"")}\"";
            }
            return field;
        }
    }

    /// <summary>
    /// JSON file operations
    /// </summary>
    public static class JsonOperations
    {
        /// <summary>
        /// Read JSON file
        /// </summary>
        public static async Task<T?> ReadJsonAsync<T>(string path)
        {
            var json = await File.ReadAllTextAsync(path);
            return System.Text.Json.JsonSerializer.Deserialize<T>(json);
        }

        /// <summary>
        /// Write JSON file
        /// </summary>
        public static async Task WriteJsonAsync<T>(string path, T obj, bool indented = true)
        {
            var options = new System.Text.Json.JsonSerializerOptions
            {
                WriteIndented = indented
            };
            var json = System.Text.Json.JsonSerializer.Serialize(obj, options);
            await File.WriteAllTextAsync(path, json);
        }
    }
} 