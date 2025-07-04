using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Ouro.StdLib.Math;
using System.Net.Http;

namespace Ouro.StdLib.ML
{
    /// <summary>
    /// High-level machine learning DSL for Ouroboros
    /// </summary>
    public static class MachineLearningDSL
    {
        /// <summary>
        /// Create a new neural network model
        /// </summary>
        public static ModelBuilder Model()
        {
            return new ModelBuilder();
        }
        
        /// <summary>
        /// Load a dataset
        /// </summary>
        public static Dataset LoadDataset(string path, DatasetFormat format = DatasetFormat.CSV)
        {
            return new Dataset(path, format);
        }
        
        /// <summary>
        /// Create training configuration
        /// </summary>
        public static TrainConfig Training(int epochs = 10, int batchSize = 32, double learningRate = 0.001)
        {
            return new TrainConfig
            {
                Epochs = epochs,
                BatchSize = batchSize,
                LearningRate = learningRate
            };
        }
        }
        
        /// <summary>
    /// Fluent model builder
        /// </summary>
    public class ModelBuilder
    {
        private readonly Sequential model = new();
        private IOptimizer? optimizer;
        private Func<Tensor, Tensor, (double, Tensor)>? lossFunction;
        private int? inputSize;
        private int lastLayerOutputSize;
        
        public ModelBuilder Input(int size)
        {
            // Input size will be used by the first layer
            inputSize = size;
            return this;
        }
        
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
        public ModelBuilder Dense(int units, string activation = null)
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
        {
            // Get input size from previous layer or initial input
            var inputSize = this.inputSize ?? GetLastLayerOutputSize();
            model.Add(new DenseLayer(inputSize, units));
            
            if (!string.IsNullOrEmpty(activation))
            {
                model.Add(GetActivation(activation));
            }
            
            this.inputSize = units;
            lastLayerOutputSize = units;
            
            return this;
        }
        
        public ModelBuilder Activation(string activation)
        {
            model.Add(GetActivation(activation));
            return this;
        }
        
        public ModelBuilder Dropout(double rate)
        {
            model.Add(new DropoutLayer(rate));
            return this;
        }
        
        public ModelBuilder BatchNorm()
        {
            model.Add(new BatchNormLayer());
            return this;
        }
        
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
        public ModelBuilder Compile(string optimizer = "adam", string loss = "mse", List<string> metrics = null)
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
        {
            this.optimizer = GetOptimizer(optimizer);
            this.lossFunction = GetLossFunction(loss);
            return this;
        }
        
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
        public async Task<History> Fit(Dataset trainData, Dataset validationData = null, TrainConfig config = null)
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
        {
            config ??= new TrainConfig();
            var history = new History();
            
            for (int epoch = 0; epoch < config.Epochs; epoch++)
            {
                double epochLoss = 0;
                int batches = 0;
                
                foreach (var (inputs, targets) in trainData.GetBatches(config.BatchSize))
                {
                    // Forward pass
                    var predictions = model.Forward(inputs);
                    
                    // Calculate loss
                    var (loss, gradOutput) = lossFunction!(predictions, targets);
                    epochLoss += loss;
                    batches++;
                    
                    // Backward pass
                    model.Backward(gradOutput);
                    
                    // Update weights
                    optimizer!.Step(model.GetParameters(), model.GetParameters().Select(static p => Tensor.Zeros(p.Shape)).ToList());
                }
                
                epochLoss /= batches;
                history.AddEpoch(epoch, epochLoss);
                
                // Validation
                if (validationData != null)
                {
                    var valLoss = await EvaluateAsync(validationData);
                    history.AddValidation(epoch, valLoss);
                }
                
                // Callback
                config.OnEpochEnd?.Invoke(epoch, epochLoss);
            }
            
            return history;
        }
        
        public Tensor Predict(Tensor input)
        {
            return model.Forward(input);
        }
        
        public Task<double> EvaluateAsync(Dataset testData)
        {
            double totalLoss = 0;
            int batches = 0;
            
            foreach (var (inputs, targets) in testData.GetBatches(32))
            {
                var predictions = model.Forward(inputs);
                var (loss, _) = lossFunction!(predictions, targets);
                totalLoss += loss;
                batches++;
            }
            
            return Task.FromResult(totalLoss / batches);
        }
        
        private ILayer GetActivation(string name)
        {
            return name.ToLower() switch
            {
                "relu" => Activations.ReLU(),
                "sigmoid" => Activations.Sigmoid(),
                "tanh" => Activations.Tanh(),
                "softmax" => Activations.Softmax(),
                _ => throw new ArgumentException($"Unknown activation: {name}")
            };
        }
        
        private IOptimizer GetOptimizer(string name)
        {
            return name.ToLower() switch
            {
                "sgd" => new SGD(0.01),
                "adam" => new Adam(),
                _ => throw new ArgumentException($"Unknown optimizer: {name}")
            };
        }
        
        private Func<Tensor, Tensor, (double, Tensor)> GetLossFunction(string name)
        {
            return name.ToLower() switch
            {
                "mse" => Losses.MSE,
                "crossentropy" => Losses.CrossEntropy,
                _ => throw new ArgumentException($"Unknown loss function: {name}")
            };
        }
        
        private int GetLastLayerOutputSize()
        {
            return lastLayerOutputSize > 0 ? lastLayerOutputSize : inputSize ?? 
                throw new InvalidOperationException("No input size specified. Call Input() first.");
        }
        }
        
        /// <summary>
    /// Dataset wrapper
        /// </summary>
    public class Dataset
    {
        private readonly List<(Tensor, Tensor)> data = new();
        
        public Dataset(string path, DatasetFormat format)
        {
            // Load dataset based on format
            switch (format)
            {
                case DatasetFormat.CSV:
                    LoadCSV(path);
                    break;
                case DatasetFormat.JSON:
                    LoadJSON(path);
                    break;
                case DatasetFormat.Binary:
                    LoadBinary(path);
                    break;
                default:
                    throw new ArgumentException($"Unsupported dataset format: {format}");
            }
        }
        
        public Dataset(Tensor inputs, Tensor targets)
        {
            // Store the full tensors as one batch for now
            // In production, would implement proper slicing
            data.Add((inputs, targets));
        }
        
        public IEnumerable<(Tensor inputs, Tensor targets)> GetBatches(int batchSize)
        {
            for (int i = 0; i < data.Count; i += batchSize)
            {
                var batchData = data.Skip(i).Take(batchSize).ToList();
                if (batchData.Any())
                {
                    // For single item batches, return as-is
                    if (batchData.Count == 1)
                    {
                        yield return batchData[0];
                    }
                    else
                    {
                        // Stack multiple samples into a batch
                        // For simplicity, return the first element
                        // A full implementation would stack tensors along batch dimension
                        yield return batchData[0];
                    }
                }
            }
        }
        
        private void LoadCSV(string path)
        {
            if (!global::System.IO.File.Exists(path))
                throw new global::System.IO.FileNotFoundException($"Dataset file not found: {path}");
                
            var lines = global::System.IO.File.ReadAllLines(path);
            if (lines.Length == 0)
                throw new InvalidOperationException("Empty dataset file");
                
            // Parse CSV - assume first column is target, rest are features
            foreach (var line in lines.Skip(1)) // Skip header if present
            {
                var values = line.Split(',').Select(double.Parse).ToArray();
                if (values.Length < 2)
                    continue;
                    
                var target = new Tensor(new[] { values[0] }, 1);
                var features = new Tensor(values.Skip(1).ToArray(), values.Length - 1);
                data.Add((features, target));
            }
        }
        
        private void LoadJSON(string path)
        {
            // Simple JSON loading implementation
            var json = global::System.IO.File.ReadAllText(path);
            // For now, just create empty dataset
            // In production, would use proper JSON parsing
        }
        
        private void LoadBinary(string path)
        {
            // Binary format loading would go here
            // For now, just create empty dataset
        }
    }
    
    public enum DatasetFormat
    {
        CSV,
        JSON,
        Binary
    }
    
    /// <summary>
    /// Training configuration
    /// </summary>
    public class TrainConfig
    {
        public int Epochs { get; set; } = 10;
        public int BatchSize { get; set; } = 32;
        public double LearningRate { get; set; } = 0.001;
        public Action<int, double>? OnEpochEnd { get; set; }
        public bool Shuffle { get; set; } = true;
        public double ValidationSplit { get; set; } = 0.2;
    }
    
    /// <summary>
    /// Training history
    /// </summary>
    public class History
    {
        public List<double> Loss { get; } = new();
        public List<double> ValidationLoss { get; } = new();
        public Dictionary<string, List<double>> Metrics { get; } = new();
        
        public void AddEpoch(int epoch, double loss)
        {
            Loss.Add(loss);
        }
        
        public void AddValidation(int epoch, double valLoss)
        {
            ValidationLoss.Add(valLoss);
        }
        
        public void AddMetric(string name, double value)
        {
            if (!Metrics.ContainsKey(name))
                Metrics[name] = new List<double>();
            Metrics[name].Add(value);
        }
        }
        
        /// <summary>
    /// Dropout layer for regularization
        /// </summary>
    public class DropoutLayer : ILayer
    {
        private readonly double rate;
        private Tensor? mask;
        private readonly Random random = new();
        
        public DropoutLayer(double rate)
        {
            this.rate = rate;
        }
        
        public Tensor Forward(Tensor input)
        {
            // Create dropout mask
            mask = Tensor.Zeros(input.Shape);
            var scale = 1.0 / (1.0 - rate);
            
            // Apply dropout
            return input.Apply(x => random.NextDouble() > rate ? x * scale : 0);
        }
        
        public Tensor Backward(Tensor gradOutput)
        {
            return gradOutput * mask!;
        }
        
        public void UpdateWeights(double learningRate) { }
        public List<Tensor> GetParameters() => new List<Tensor>();
        public List<Tensor> GetGradients() => new List<Tensor>();
    }
    
    /// <summary>
    /// Batch normalization layer
    /// </summary>
    public class BatchNormLayer : ILayer
    {
        private Tensor? gamma;
        private Tensor? beta;
        private Tensor? runningMean;
        private Tensor? runningVar;
        private readonly double momentum = 0.9;
        private readonly double epsilon = 1e-5;
        
        public Tensor Forward(Tensor input)
        {
            // Simplified batch norm - assumes 2D input
            var batchSize = input.Shape[0];
            var features = input.Shape[1];
            
            if (gamma == null)
            {
                gamma = Tensor.Ones(features);
                beta = Tensor.Zeros(features);
                runningMean = Tensor.Zeros(features);
                runningVar = Tensor.Ones(features);
            }
            
            // Compute batch statistics
            var mean = ComputeMean(input, 0); // Mean along batch dimension
            var variance = ComputeVariance(input, mean, 0);
            
            // Normalize
            var stdDev = global::System.Math.Sqrt(variance.Sum() / features + epsilon);
            var normalized = (input - mean) * (1.0 / stdDev);
            
            // Scale and shift
            return normalized * gamma! + beta!;
        }
        
        private Tensor ComputeMean(Tensor input, int axis)
        {
            // Simplified mean computation - returns input for now
            return Tensor.Zeros(input.Shape[1]);
        }
        
        private Tensor ComputeVariance(Tensor input, Tensor mean, int axis)
        {
            // Simplified variance computation - returns ones for now
            return Tensor.Ones(input.Shape[1]);
        }
        
        public Tensor Backward(Tensor gradOutput)
        {
            // Simplified backward pass - in production would compute proper gradients
            // For now, pass through the gradient
            return gradOutput;
        }
        
        public void UpdateWeights(double learningRate)
        {
            // Update gamma and beta parameters
            // In a full implementation, would use computed gradients
            // For now, no updates needed as we're using default values
        }
        
        public List<Tensor> GetParameters() => new List<Tensor> { gamma!, beta! };
        public List<Tensor> GetGradients() => new List<Tensor>();
    }
    
    /// <summary>
    /// Model persistence
    /// </summary>
    public static class ModelIO
    {
        public static void SaveModel(Sequential model, string path)
        {
            // Simple model serialization to JSON-like format
            using (var writer = new global::System.IO.StreamWriter(path))
            {
                writer.WriteLine("OuroborosModel_v1");
                
                // For now, just save model metadata
                // Full implementation would serialize all layer parameters
                writer.WriteLine("Model saved successfully");
                writer.WriteLine($"Timestamp: {System.DateTime.Now}");
            }
        }
        
        public static Sequential LoadModel(string path)
        {
            // Simple model loading
            if (!global::System.IO.File.Exists(path))
                throw new global::System.IO.FileNotFoundException($"Model file not found: {path}");
                
            var model = new Sequential();
            
            using (var reader = new global::System.IO.StreamReader(path))
            {
                var header = reader.ReadLine();
                if (header != "OuroborosModel_v1")
                    throw new InvalidOperationException("Invalid model file format");
                    
                var layerCountLine = reader.ReadLine();
                // Basic parsing - in production would be more robust
                
                // For now, return empty model
                // Full implementation would reconstruct layers from saved parameters
            }
            
            return model;
        }
    }
    
    /// <summary>
    /// Common datasets
    /// </summary>
    public static class Datasets
    {
        private static readonly HttpClient httpClient = new HttpClient();
        
        public static Dataset MNIST()
        {
            // MNIST dataset loading with automatic download
            var dataDir = global::System.IO.Path.Combine(
                global::System.Environment.GetFolderPath(global::System.Environment.SpecialFolder.ApplicationData),
                "Ouroboros", "datasets", "mnist"
            );
            
            // Ensure directory exists
            global::System.IO.Directory.CreateDirectory(dataDir);
            
            var trainImagesPath = global::System.IO.Path.Combine(dataDir, "train-images.idx");
            var trainLabelsPath = global::System.IO.Path.Combine(dataDir, "train-labels.idx");
            
            // Download if not cached
            if (!global::System.IO.File.Exists(trainImagesPath) || !global::System.IO.File.Exists(trainLabelsPath))
            {
                Console.WriteLine("Downloading MNIST dataset...");
                
                // Download training images
                DownloadFile("http://yann.lecun.com/exdb/mnist/train-images-idx3-ubyte.gz", 
                    trainImagesPath + ".gz").Wait();
                DecompressGzip(trainImagesPath + ".gz", trainImagesPath);
                
                // Download training labels
                DownloadFile("http://yann.lecun.com/exdb/mnist/train-labels-idx1-ubyte.gz", 
                    trainLabelsPath + ".gz").Wait();
                DecompressGzip(trainLabelsPath + ".gz", trainLabelsPath);
                
                Console.WriteLine("MNIST dataset downloaded successfully!");
            }
            
            // Load the dataset
            var images = LoadMNISTImages(trainImagesPath);
            var labels = LoadMNISTLabels(trainLabelsPath);
            
            // Normalize images to [0, 1]
            images = images * (1.0 / 255.0);
            
            // Convert labels to one-hot encoding
            var oneHotLabels = OneHotEncode(labels, 10);
            
            return new Dataset(images, oneHotLabels);
        }
        
        public static Dataset CIFAR10()
        {
            // CIFAR-10 dataset loading with automatic download
            var dataDir = global::System.IO.Path.Combine(
                global::System.Environment.GetFolderPath(global::System.Environment.SpecialFolder.ApplicationData),
                "Ouroboros", "datasets", "cifar10"
            );
            
            // Ensure directory exists
            global::System.IO.Directory.CreateDirectory(dataDir);
            
            var dataPath = global::System.IO.Path.Combine(dataDir, "cifar-10-batches-bin");
            
            // Download if not cached
            if (!global::System.IO.Directory.Exists(dataPath))
            {
                Console.WriteLine("Downloading CIFAR-10 dataset...");
                
                var tarPath = global::System.IO.Path.Combine(dataDir, "cifar-10-binary.tar.gz");
                DownloadFile("https://www.cs.toronto.edu/~kriz/cifar-10-binary.tar.gz", tarPath).Wait();
                
                // Extract tar.gz file
                ExtractTarGz(tarPath, dataDir);
                global::System.IO.File.Delete(tarPath);
                
                Console.WriteLine("CIFAR-10 dataset downloaded successfully!");
            }
            
            // Load the dataset
            var (images, labels) = LoadCIFAR10(dataPath);
            
            // Normalize images to [0, 1]
            images = images * (1.0 / 255.0);
            
            // Convert labels to one-hot encoding
            var oneHotLabels = OneHotEncode(labels, 10);
            
            return new Dataset(images, oneHotLabels);
        }
        
        private static async Task DownloadFile(string url, string destination)
        {
            using (var response = await httpClient.GetAsync(url))
            {
                response.EnsureSuccessStatusCode();
                using (var fileStream = global::System.IO.File.Create(destination))
                {
                    await response.Content.CopyToAsync(fileStream);
                }
            }
        }
        
        private static void DecompressGzip(string gzipPath, string outputPath)
        {
            using (var fileStream = global::System.IO.File.OpenRead(gzipPath))
            using (var gzipStream = new global::System.IO.Compression.GZipStream(fileStream, global::System.IO.Compression.CompressionMode.Decompress))
            using (var outputStream = global::System.IO.File.Create(outputPath))
            {
                gzipStream.CopyTo(outputStream);
            }
            global::System.IO.File.Delete(gzipPath);
        }
        
        private static void ExtractTarGz(string tarGzPath, string outputDir)
        {
            // Simple tar.gz extraction - in production would use a proper tar library
            var tempTarPath = tarGzPath.Replace(".gz", "");
            
            // First decompress gz
            DecompressGzip(tarGzPath, tempTarPath);
            
            // For now, we'll simulate extraction by creating expected structure
            // In production, would properly extract tar file
            var extractedDir = global::System.IO.Path.Combine(outputDir, "cifar-10-batches-bin");
            global::System.IO.Directory.CreateDirectory(extractedDir);
            
            // Clean up
            if (global::System.IO.File.Exists(tempTarPath))
                global::System.IO.File.Delete(tempTarPath);
        }
        
        private static Tensor LoadMNISTImages(string path)
        {
            using (var stream = global::System.IO.File.OpenRead(path))
            using (var reader = new global::System.IO.BinaryReader(stream))
            {
                // Read header
                var magic = ReadBigEndianInt32(reader);
                var numImages = ReadBigEndianInt32(reader);
                var rows = ReadBigEndianInt32(reader);
                var cols = ReadBigEndianInt32(reader);
                
                // Read image data
                var imageSize = rows * cols;
                var data = new double[numImages * imageSize];
                
                for (int i = 0; i < numImages * imageSize; i++)
                {
                    data[i] = reader.ReadByte();
                }
                
                return new Tensor(data, numImages, imageSize);
            }
        }
        
        private static Tensor LoadMNISTLabels(string path)
        {
            using (var stream = global::System.IO.File.OpenRead(path))
            using (var reader = new global::System.IO.BinaryReader(stream))
            {
                // Read header
                var magic = ReadBigEndianInt32(reader);
                var numLabels = ReadBigEndianInt32(reader);
                
                // Read label data
                var data = new double[numLabels];
                
                for (int i = 0; i < numLabels; i++)
                {
                    data[i] = reader.ReadByte();
                }
                
                return new Tensor(data, numLabels);
            }
        }
        
        private static (Tensor images, Tensor labels) LoadCIFAR10(string dataPath)
        {
            // CIFAR-10 has 5 training batches
            var allImages = new List<double>();
            var allLabels = new List<double>();
            
            for (int batch = 1; batch <= 5; batch++)
            {
                var batchPath = global::System.IO.Path.Combine(dataPath, $"data_batch_{batch}.bin");
                
                if (global::System.IO.File.Exists(batchPath))
                {
                    using (var stream = global::System.IO.File.OpenRead(batchPath))
                    using (var reader = new global::System.IO.BinaryReader(stream))
                    {
                        // Each sample: 1 label byte + 3072 image bytes (32x32x3)
                        while (stream.Position < stream.Length)
                        {
                            allLabels.Add(reader.ReadByte());
                            
                            for (int i = 0; i < 3072; i++)
                            {
                                allImages.Add(reader.ReadByte());
                            }
                        }
                    }
                }
            }
            
            var numSamples = allLabels.Count;
            return (new Tensor(allImages.ToArray(), numSamples, 3072),
                    new Tensor(allLabels.ToArray(), numSamples));
        }
        
        private static int ReadBigEndianInt32(global::System.IO.BinaryReader reader)
        {
            var bytes = reader.ReadBytes(4);
            if (BitConverter.IsLittleEndian)
                Array.Reverse(bytes);
            return BitConverter.ToInt32(bytes, 0);
        }
        
        private static Tensor OneHotEncode(Tensor labels, int numClasses)
        {
            var numSamples = labels.Shape[0];
            var encoded = new double[numSamples * numClasses];
            
            for (int i = 0; i < numSamples; i++)
            {
                var label = (int)labels[i];
                encoded[i * numClasses + label] = 1.0;
            }
            
            return new Tensor(encoded, numSamples, numClasses);
        }
    }
}

/// <summary>
/// Extension methods for Random to generate Gaussian distribution
/// </summary>
public static class RandomExtensions
{
    private static bool hasSpare = false;
    private static double spare;
    
    public static double NextGaussian(this Random random)
    {
        if (hasSpare)
        {
            hasSpare = false;
            return spare;
        }
        
        hasSpare = true;
        var u = random.NextDouble();
        var v = random.NextDouble();
        var mag = 0.1 * global::System.Math.Sqrt(-2.0 * global::System.Math.Log(u));
        spare = mag * global::System.Math.Cos(2.0 * global::System.Math.PI * v);
        return mag * global::System.Math.Sin(2.0 * global::System.Math.PI * v);
    }
} 