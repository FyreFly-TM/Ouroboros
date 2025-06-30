import React from 'react';
import CodeBlock from '../components/CodeBlock';
import Callout from '../components/Callout';

const FileIOPage = () => {
  return (
    <div className="space-y-8">
      <div>
        <h1 className="text-4xl font-bold mb-4">File I/O Operations</h1>
        <p className="text-lg text-gray-600 dark:text-gray-400">
          Comprehensive file handling and I/O operations in Ouroboros.
        </p>
      </div>

      <section className="space-y-6">
        <h2 className="text-3xl font-semibold mb-4">Overview</h2>
        <p>
          Ouroboros provides powerful and intuitive file I/O operations through the IO module. 
          The language supports both synchronous and asynchronous operations, with built-in 
          safety features and automatic resource management.
        </p>
      </section>

      <section className="space-y-6">
        <h2 className="text-3xl font-semibold mb-4">File Operations</h2>
        
        <div className="space-y-4">
          <h3 className="text-2xl font-semibold">Reading Files</h3>
          <p>
            File reading in Ouroboros is straightforward and handles encoding automatically.
          </p>
          
          <CodeBlock 
            code={`// Read entire file content
content := IO.ReadAllText("test.txt")
print(content)

// Read file line by line
lines := IO.ReadAllLines("data.txt")
for line in lines {
    process(line)
}

// Streaming large files
using stream := IO.OpenRead("large.log") {
    while !stream.EndOfStream {
        line := stream.ReadLine()
        if line contains "ERROR" {
            print(line)
        }
    }
}`}
            title="File Reading Examples"
          />
        </div>

        <div className="space-y-4">
          <h3 className="text-2xl font-semibold">Writing Files</h3>
          <p>
            Writing to files supports various modes and automatic encoding handling.
          </p>
          
          <CodeBlock 
            code={`// Write text to file (overwrites existing)
IO.WriteAllText("output.txt", "Hello, Ouroboros!")

// Append to file
IO.AppendAllText("log.txt", "New log entry\\n")

// Write multiple lines
lines := ["Line 1", "Line 2", "Line 3"]
IO.WriteAllLines("data.txt", lines)

// Streaming write for large data
using writer := IO.CreateText("large_output.txt") {
    for i in 0..1000000 {
        writer.WriteLine($"Line {i}: Generated data")
    }
}`}
            title="File Writing Examples"
          />
        </div>

        <div className="space-y-4">
          <h3 className="text-2xl font-semibold">Binary File Operations</h3>
          <p>
            For binary data, Ouroboros provides specialized readers and writers.
          </p>
          
          <CodeBlock 
            code={`// Read binary data
bytes := IO.ReadAllBytes("image.png")
print($"File size: {bytes.Length} bytes")

// Write binary data
data := [0x48, 0x65, 0x6C, 0x6C, 0x6F]  // "Hello" in hex
IO.WriteAllBytes("binary.dat", data)

// Binary streaming
using reader := IO.OpenBinaryRead("data.bin") {
    magic := reader.ReadInt32()
    version := reader.ReadByte()
    if magic = 0x12345678 and version >= 2 {
        count := reader.ReadInt32()
        for i in 0..count {
            value := reader.ReadDouble()
            process(value)
        }
    }
}`}
            title="Binary File Operations"
          />
        </div>
      </section>

      <section className="space-y-6">
        <h2 className="text-3xl font-semibold mb-4">Directory Operations</h2>
        
        <CodeBlock 
          code={`// Check if directory exists
if IO.DirectoryExists("./data") {
    print("Data directory found")
}

// Create directory (including parent directories)
IO.CreateDirectory("./output/results/2024")

// List directory contents
files := IO.GetFiles("./src", "*.ouro")
for file in files {
    print($"Found source file: {file}")
}

// Recursive directory search
allFiles := IO.GetFiles("./project", "*.*", SearchOption.AllDirectories)
print($"Total files: {allFiles.Count}")

// Delete directory
if IO.DirectoryExists("./temp") {
    IO.DeleteDirectory("./temp", recursive: true)
}`}
          title="Directory Management"
        />
      </section>

      <section className="space-y-6">
        <h2 className="text-3xl font-semibold mb-4">Path Operations</h2>
        
        <CodeBlock 
          code={`// Path manipulation
fullPath := IO.GetFullPath("../data/file.txt")
directory := IO.GetDirectoryName(fullPath)
filename := IO.GetFileName(fullPath)
extension := IO.GetExtension(fullPath)

print($"Full path: {fullPath}")
print($"Directory: {directory}")
print($"Filename: {filename}")
print($"Extension: {extension}")

// Combine paths (platform-independent)
configPath := IO.CombinePath(IO.GetCurrentDirectory(), "config", "settings.json")

// Get special folders
desktop := IO.GetFolderPath(Environment.SpecialFolder.Desktop)
documents := IO.GetFolderPath(Environment.SpecialFolder.MyDocuments)

// Temporary files
tempFile := IO.GetTempFileName()
IO.WriteAllText(tempFile, "Temporary data")
// ... use temp file ...
IO.Delete(tempFile)`}
          title="Path Manipulation"
        />
      </section>

      <section className="space-y-6">
        <h2 className="text-3xl font-semibold mb-4">File Monitoring</h2>
        
        <CodeBlock 
          code={`// Watch for file changes
watcher := new FileSystemWatcher("./data") {
    Filter = "*.json",
    NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName
}

watcher.Changed += (sender, e) => {
    print($"File changed: {e.FullPath}")
    reloadConfiguration(e.FullPath)
}

watcher.Created += (sender, e) => {
    print($"New file: {e.FullPath}")
}

watcher.Deleted += (sender, e) => {
    print($"File deleted: {e.FullPath}")
}

watcher.EnableRaisingEvents = true

// Keep watching until user stops
print("Watching for file changes. Press any key to stop...")
Console.ReadKey()
watcher.Dispose()`}
          title="File System Watcher"
        />
      </section>

      <section className="space-y-6">
        <h2 className="text-3xl font-semibold mb-4">Advanced I/O</h2>
        
        <div className="space-y-4">
          <h3 className="text-2xl font-semibold">Memory-Mapped Files</h3>
          
          <CodeBlock 
            code={`// Create memory-mapped file for large data processing
using mmf := IO.CreateMemoryMappedFile("large_data.bin", 1_000_000_000) {
    using view := mmf.CreateViewAccessor(0, 1000) {
        // Write data directly to memory
        view.Write(0, 42)
        view.Write(4, 3.14159)
        
        // Read data
        intValue := view.ReadInt32(0)
        doubleValue := view.ReadDouble(4)
    }
}`}
            title="Memory-Mapped Files"
          />
        </div>

        <div className="space-y-4">
          <h3 className="text-2xl font-semibold">Compression</h3>
          
          <CodeBlock 
            code={`// Compress file
using input := IO.OpenRead("large_file.txt")
using output := IO.Create("compressed.gz")
using gzip := new GZipStream(output, CompressionMode.Compress) {
    input.CopyTo(gzip)
}

// Decompress file
using compressed := IO.OpenRead("compressed.gz")
using gzip := new GZipStream(compressed, CompressionMode.Decompress)
using output := IO.Create("decompressed.txt") {
    gzip.CopyTo(output)
}`}
            title="File Compression"
          />
        </div>
      </section>

      <section className="space-y-6">
        <h2 className="text-3xl font-semibold mb-4">Error Handling</h2>
        
        <Callout variant="warning" title="Important">
          Always handle I/O errors appropriately as file operations can fail for various reasons.
        </Callout>
        
        <CodeBlock 
          code={`// Safe file reading with error handling
try {
    content := IO.ReadAllText("config.json")
    config := JSON.Parse(content)
} catch FileNotFoundException {
    print("Configuration file not found, using defaults")
    config := getDefaultConfig()
} catch UnauthorizedAccessException {
    print("Permission denied reading config file")
    exit(1)
} catch IOException as e {
    print($"I/O error: {e.Message}")
    exit(1)
}

// Check permissions before writing
path := "./output/results.txt"
if IO.CanWrite(IO.GetDirectoryName(path)) {
    IO.WriteAllText(path, results)
} else {
    print("Cannot write to output directory")
}`}
          title="Error Handling"
        />
      </section>

      <section className="space-y-6">
        <h2 className="text-3xl font-semibold mb-4">Async I/O</h2>
        
        <CodeBlock 
          code={`// Asynchronous file operations
async processFiles() {
    // Read multiple files concurrently
    tasks := []
    for file in IO.GetFiles("./data", "*.json") {
        tasks.Add(IO.ReadAllTextAsync(file))
    }
    
    // Wait for all reads to complete
    contents := await Task.WhenAll(tasks)
    
    // Process results
    for i, content in contents {
        data := JSON.Parse(content)
        await processDataAsync(data)
    }
}

// Async streaming
async streamLargeFile() {
    using reader := IO.OpenTextAsync("huge_log.txt") {
        lineNumber := 0
        while line := await reader.ReadLineAsync() {
            lineNumber++
            if lineNumber % 1000 = 0 {
                print($"Processed {lineNumber} lines...")
            }
            await processLineAsync(line)
        }
    }
}`}
          title="Asynchronous I/O"
        />
      </section>

      <section className="space-y-6">
        <h2 className="text-3xl font-semibold mb-4">Best Practices</h2>
        
        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
          <Callout variant="tip" title="Use 'using' statements">
            Always use 'using' statements for file streams to ensure proper resource cleanup.
          </Callout>
          
          <Callout variant="tip" title="Check permissions">
            Verify file/directory permissions before attempting operations.
          </Callout>
          
          <Callout variant="tip" title="Handle large files">
            Use streaming for large files instead of loading entire content into memory.
          </Callout>
          
          <Callout variant="tip" title="Platform independence">
            Use IO.CombinePath() for platform-independent path construction.
          </Callout>
        </div>
      </section>

      <section className="space-y-6">
        <h2 className="text-3xl font-semibold mb-4">Performance Considerations</h2>
        
        <div className="bg-gray-50 dark:bg-gray-800 p-6 rounded-lg">
          <h3 className="text-xl font-semibold mb-4">I/O Performance Tips</h3>
          <ul className="space-y-2">
            <li className="flex items-start">
              <span className="text-green-500 mr-2">✓</span>
              <span>Use buffered streams for better performance with many small operations</span>
            </li>
            <li className="flex items-start">
              <span className="text-green-500 mr-2">✓</span>
              <span>Prefer async I/O for UI applications to avoid blocking</span>
            </li>
            <li className="flex items-start">
              <span className="text-green-500 mr-2">✓</span>
              <span>Consider memory-mapped files for very large files</span>
            </li>
            <li className="flex items-start">
              <span className="text-green-500 mr-2">✓</span>
              <span>Batch small write operations to reduce system calls</span>
            </li>
            <li className="flex items-start">
              <span className="text-green-500 mr-2">✓</span>
              <span>Use appropriate buffer sizes (typically 4KB-64KB)</span>
            </li>
          </ul>
        </div>
      </section>
    </div>
  );
};

export default FileIOPage; 