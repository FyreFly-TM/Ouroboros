using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Ouroboros.StdLib.Math;

namespace Ouroboros.StdLib.ML
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
        private IOptimizer optimizer;
        private Func<Tensor, Tensor, (double, Tensor)> lossFunction;
        private int? inputSize;
        private int lastLayerOutputSize;
        
        public ModelBuilder Input(int size)
        {
            // Input size will be used by the first layer
            inputSize = size;
            return this;
        }
        
        public ModelBuilder Dense(int units, string activation = null)
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
        
        public ModelBuilder Compile(string optimizer = "adam", string loss = "mse", List<string> metrics = null)
        {
            this.optimizer = GetOptimizer(optimizer);
            this.lossFunction = GetLossFunction(loss);
            return this;
        }
        
        public async Task<History> Fit(Dataset trainData, Dataset validationData = null, TrainConfig config = null)
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
                    var (loss, gradOutput) = lossFunction(predictions, targets);
                    epochLoss += loss;
                    batches++;
                    
                    // Backward pass
                    model.Backward(gradOutput);
                    
                    // Update weights
                    optimizer.Step(model.GetParameters(), model.GetParameters().Select(p => Tensor.Zeros(p.Shape)).ToList());
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
        
        public async Task<double> EvaluateAsync(Dataset testData)
        {
            double totalLoss = 0;
            int batches = 0;
            
            foreach (var (inputs, targets) in testData.GetBatches(32))
            {
                var predictions = model.Forward(inputs);
                var (loss, _) = lossFunction(predictions, targets);
                totalLoss += loss;
                batches++;
            }
            
            return totalLoss / batches;
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
        public Action<int, double> OnEpochEnd { get; set; }
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
        private Tensor mask;
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
            return gradOutput * mask;
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
        private Tensor gamma;
        private Tensor beta;
        private Tensor runningMean;
        private Tensor runningVar;
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
            return normalized * gamma + beta;
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
        
        public List<Tensor> GetParameters() => new List<Tensor> { gamma, beta };
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
        public static Dataset MNIST()
        {
            // Placeholder for MNIST dataset loading
            // In production, would download from official source if not cached
            var cachePath = global::System.IO.Path.Combine(
                global::System.Environment.GetFolderPath(global::System.Environment.SpecialFolder.ApplicationData),
                "Ouroboros", "datasets", "mnist.bin"
            );
            
            if (global::System.IO.File.Exists(cachePath))
            {
                return new Dataset(cachePath, DatasetFormat.Binary);
            }
            
            // For now, return empty dataset
            // Full implementation would download and cache the dataset
            var dummyInputs = Tensor.Random(60000, 784); // 60k samples, 28x28 images
            var dummyLabels = Tensor.Zeros(60000, 10); // 10 classes
            return new Dataset(dummyInputs, dummyLabels);
        }
        
        public static Dataset CIFAR10()
        {
            // Placeholder for CIFAR-10 dataset loading
            // In production, would download from official source if not cached
            var cachePath = global::System.IO.Path.Combine(
                global::System.Environment.GetFolderPath(global::System.Environment.SpecialFolder.ApplicationData),
                "Ouroboros", "datasets", "cifar10.bin"
            );
            
            if (global::System.IO.File.Exists(cachePath))
            {
                return new Dataset(cachePath, DatasetFormat.Binary);
            }
            
            // For now, return empty dataset
            // Full implementation would download and cache the dataset
            var dummyInputs = Tensor.Random(50000, 3072); // 50k samples, 32x32x3 images
            var dummyLabels = Tensor.Zeros(50000, 10); // 10 classes
            return new Dataset(dummyInputs, dummyLabels);
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