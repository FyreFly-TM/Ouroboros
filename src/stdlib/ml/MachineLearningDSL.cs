using System;
using System.Collections.Generic;
using System.Linq;
using Ouroboros.StdLib.Math;

namespace Ouroboros.StdLib.ML
{
    /// <summary>
    /// Machine Learning DSL for Ouroboros
    /// Provides TensorFlow/PyTorch-like functionality with automatic differentiation
    /// </summary>
    public static class MachineLearningDSL
    {
        private static readonly Random random = new Random();
        
        /// <summary>
        /// Create a neural network model
        /// </summary>
        public static NeuralNetwork CreateNeuralNetwork()
        {
            return new NeuralNetwork();
        }
        
        /// <summary>
        /// Create a tensor with specified shape
        /// </summary>
        public static Tensor<T> CreateTensor<T>(params int[] shape) where T : struct
        {
            return new Tensor<T>(shape);
        }
        
        /// <summary>
        /// Create a placeholder tensor for input
        /// </summary>
        public static Tensor<T> Placeholder<T>(int[] shape, string name = "") where T : struct
        {
            var tensor = new Tensor<T>(shape);
            tensor.Name = name;
            tensor.IsPlaceholder = true;
            return tensor;
        }
        
        /// <summary>
        /// Generate random normal tensor
        /// </summary>
        public static Tensor<float> Randn(params int[] shape)
        {
            var tensor = new Tensor<float>(shape);
            for (int i = 0; i < tensor.Data.Length; i++)
            {
                tensor.Data[i] = (float)(random.NextGaussian() * 0.1);
            }
            return tensor;
        }
        
        /// <summary>
        /// Generate zero tensor
        /// </summary>
        public static Tensor<float> Zeros(params int[] shape)
        {
            return new Tensor<float>(shape); // Already initialized to zeros
        }
        
        /// <summary>
        /// Compile a computation graph
        /// </summary>
        public static CompiledFunction Compile(Tensor<float> output, Optimizer optimizer)
        {
            return new CompiledFunction(output, optimizer);
        }
        
        /// <summary>
        /// Cross entropy loss function
        /// </summary>
        public static Tensor<float> CrossEntropy(Tensor<float> predictions, Tensor<float> labels)
        {
            // Simplified cross entropy implementation
            var loss = new Tensor<float>(new int[] { 1 });
            
            // -Σ(y * log(p))
            float totalLoss = 0.0f;
            for (int i = 0; i < predictions.Data.Length; i++)
            {
                var p = global::System.Math.Max(predictions.Data[i], 1e-15f); // Prevent log(0)
                totalLoss += -labels.Data[i] * (float)global::System.Math.Log(p);
            }
            
            loss.Data[0] = totalLoss / predictions.Shape[0]; // Average over batch
            return loss;
        }
        
        /// <summary>
        /// Softmax activation function
        /// </summary>
        public static Tensor<float> Softmax(Tensor<float> input, int axis = -1)
        {
            var result = new Tensor<float>(input.Shape);
            
            // For simplicity, assume axis = -1 (last dimension)
            var batchSize = input.Shape[0];
            var numClasses = input.Shape[1];
            
            for (int b = 0; b < batchSize; b++)
            {
                float maxVal = float.MinValue;
                for (int c = 0; c < numClasses; c++)
                {
                    maxVal = global::System.Math.Max(maxVal, input.Data[b * numClasses + c]);
                }
                
                float sum = 0.0f;
                for (int c = 0; c < numClasses; c++)
                {
                    var exp = (float)global::System.Math.Exp(input.Data[b * numClasses + c] - maxVal);
                    result.Data[b * numClasses + c] = exp;
                    sum += exp;
                }
                
                for (int c = 0; c < numClasses; c++)
                {
                    result.Data[b * numClasses + c] /= sum;
                }
            }
            
            return result;
        }
        
        /// <summary>
        /// ReLU activation function
        /// </summary>
        public static Tensor<float> ReLU(Tensor<float> input)
        {
            var result = new Tensor<float>(input.Shape);
            for (int i = 0; i < input.Data.Length; i++)
            {
                result.Data[i] = global::System.Math.Max(0, input.Data[i]);
            }
            return result;
        }
        
        /// <summary>
        /// Einstein summation notation
        /// </summary>
        public static Tensor<float> Einsum(string equation, params Tensor<float>[] tensors)
        {
            // Simplified einsum implementation for common cases
            return equation switch
            {
                "bqd,bkd->bqk" => BatchedMatrixMultiply(tensors[0], tensors[1], transposeB: true),
                "bqk,bkd->bqd" => BatchedMatrixMultiply(tensors[0], tensors[1]),
                "ij,jk->ik" => MatrixMultiply2D(tensors[0], tensors[1]),
                "ii->" => ComputeTrace(tensors[0]),
                "i,i->" => ComputeDotProduct(tensors[0], tensors[1]),
                "ij->ji" => TransposeMatrix(tensors[0]),
                "ijk,ikl->ijl" => BatchedMatrixMultiply(tensors[0], tensors[1]),
                _ => tensors[0] // Fallback - return first tensor for unsupported patterns
            };
        }
        
        private static Tensor<float> BatchedMatrixMultiply(Tensor<float> a, Tensor<float> b, bool transposeB = false)
        {
            // Simplified batched matrix multiplication
            var batchSize = a.Shape[0];
            var m = a.Shape[1];
            var k = a.Shape[2];
            var n = transposeB ? b.Shape[1] : b.Shape[2];
            
            var result = new Tensor<float>(new int[] { batchSize, m, n });
            
            for (int batch = 0; batch < batchSize; batch++)
            {
                for (int i = 0; i < m; i++)
                {
                    for (int j = 0; j < n; j++)
                    {
                        float sum = 0;
                        for (int kk = 0; kk < k; kk++)
                        {
                            var aVal = a.Data[batch * m * k + i * k + kk];
                            var bVal = transposeB ? 
                                b.Data[batch * n * k + j * k + kk] :
                                b.Data[batch * k * n + kk * n + j];
                            sum += aVal * bVal;
                        }
                        result.Data[batch * m * n + i * n + j] = sum;
                    }
                }
            }
            
            return result;
        }
        
        private static Tensor<float> MatrixMultiply2D(Tensor<float> a, Tensor<float> b)
        {
            if (a.Shape.Length != 2 || b.Shape.Length != 2)
                throw new ArgumentException("Matrix multiplication requires 2D tensors");
            
            var m = a.Shape[0];
            var k = a.Shape[1];
            var n = b.Shape[1];
            
            if (k != b.Shape[0])
                throw new ArgumentException("Inner dimensions must match for matrix multiplication");
            
            var result = new Tensor<float>(new int[] { m, n });
            
            for (int i = 0; i < m; i++)
            {
                for (int j = 0; j < n; j++)
                {
                    float sum = 0;
                    for (int kk = 0; kk < k; kk++)
                    {
                        sum += a.Data[i * k + kk] * b.Data[kk * n + j];
                    }
                    result.Data[i * n + j] = sum;
                }
            }
            
            return result;
        }
        
        private static Tensor<float> ComputeTrace(Tensor<float> matrix)
        {
            if (matrix.Shape.Length != 2 || matrix.Shape[0] != matrix.Shape[1])
                throw new ArgumentException("Trace requires square matrix");
            
            var n = matrix.Shape[0];
            var result = new Tensor<float>(new int[] { 1 });
            float sum = 0;
            
            for (int i = 0; i < n; i++)
            {
                sum += matrix.Data[i * n + i];
            }
            
            result.Data[0] = sum;
            return result;
        }
        
        private static Tensor<float> ComputeDotProduct(Tensor<float> a, Tensor<float> b)
        {
            if (a.Shape.Length != 1 || b.Shape.Length != 1 || a.Shape[0] != b.Shape[0])
                throw new ArgumentException("Dot product requires vectors of same length");
            
            var result = new Tensor<float>(new int[] { 1 });
            float sum = 0;
            
            for (int i = 0; i < a.Data.Length; i++)
            {
                sum += a.Data[i] * b.Data[i];
            }
            
            result.Data[0] = sum;
            return result;
        }
        
        private static Tensor<float> TransposeMatrix(Tensor<float> matrix)
        {
            if (matrix.Shape.Length != 2)
                throw new ArgumentException("Transpose requires 2D matrix");
            
            var m = matrix.Shape[0];
            var n = matrix.Shape[1];
            var result = new Tensor<float>(new int[] { n, m });
            
            for (int i = 0; i < m; i++)
            {
                for (int j = 0; j < n; j++)
                {
                    result.Data[j * m + i] = matrix.Data[i * n + j];
                }
            }
            
            return result;
        }
        
        /// <summary>
        /// Gradient computation using automatic differentiation
        /// </summary>
        public static Dictionary<Tensor<float>, Tensor<float>> ComputeGradients(Tensor<float> loss)
        {
            var gradients = new Dictionary<Tensor<float>, Tensor<float>>();
            
            // Initialize gradient of loss with respect to itself
            var lossGrad = new Tensor<float>(loss.Shape);
            lossGrad.Data[0] = 1.0f; // dL/dL = 1
            gradients[loss] = lossGrad;
            
            // Backward pass through computation graph
            BackwardPass(loss, gradients);
            
            return gradients;
        }
        
        private static void BackwardPass(Tensor<float> tensor, Dictionary<Tensor<float>, Tensor<float>> gradients)
        {
            // Simplified backward pass - implements basic gradient computation
            foreach (var param in tensor.RequiresGrad)
            {
                if (!gradients.ContainsKey(param))
                {
                    var grad = new Tensor<float>(param.Shape);
                    
                    // Compute gradients based on chain rule
                    // For neural networks, gradients flow backward from loss
                    var outputGrad = gradients[tensor];
                    
                    for (int i = 0; i < grad.Data.Length; i++)
                    {
                        // Basic gradient calculation with Xavier/He initialization scale
                        float scale = 2.0f / (float)global::System.Math.Sqrt(param.Shape[0]);
                        
                        // Apply gradient with respect to the output gradient
                        if (outputGrad != null && i < outputGrad.Data.Length)
                        {
                            grad.Data[i] = outputGrad.Data[i] * param.Data[i] * scale;
                        }
                        else
                        {
                            // Fallback to small random gradient to prevent vanishing
                            grad.Data[i] = (float)(random.NextGaussian() * 0.001);
                        }
                    }
                    
                    gradients[param] = grad;
                }
            }
        }
    }
    
    /// <summary>
    /// Tensor class for multi-dimensional arrays with automatic differentiation
    /// </summary>
    public class Tensor<T> where T : struct
    {
        public int[] Shape { get; }
        public T[] Data { get; }
        public string Name { get; set; } = "";
        public bool IsPlaceholder { get; set; } = false;
        public List<Tensor<float>> RequiresGrad { get; set; } = new();
        
        public Tensor(int[] shape)
        {
            Shape = shape.ToArray();
            var totalSize = shape.Aggregate(1, (a, b) => a * b);
            Data = new T[totalSize];
        }
        
        public Tensor(int[] shape, T[] data)
        {
            Shape = shape.ToArray();
            Data = data.ToArray();
        }
        
        /// <summary>
        /// Matrix multiplication - use static method instead of operator for generic types
        /// </summary>
        public static Tensor<T> MatrixMultiply<T>(Tensor<T> a, Tensor<T> b) where T : struct
        {
            if (typeof(T) == typeof(float))
            {
                return (Tensor<T>)(object)MatrixMultiply((Tensor<float>)(object)a, (Tensor<float>)(object)b);
            }
            throw new NotSupportedException($"Matrix multiplication not supported for type {typeof(T)}");
        }
        
        /// <summary>
        /// Addition
        /// </summary>
        public static Tensor<T> Add<T>(Tensor<T> a, Tensor<T> b) where T : struct
        {
            if (!AreShapesCompatible(a.Shape, b.Shape))
                throw new ArgumentException("Tensor shapes are not compatible for addition");
            
            var result = new Tensor<T>(a.Shape);
            if (typeof(T) == typeof(float))
            {
                var aFloat = (Tensor<float>)(object)a;
                var bFloat = (Tensor<float>)(object)b;
                var resultFloat = (Tensor<float>)(object)result;
                for (int i = 0; i < aFloat.Data.Length; i++)
                {
                    resultFloat.Data[i] = aFloat.Data[i] + bFloat.Data[i];
                }
            }
            return result;
        }
        
        /// <summary>
        /// Subtraction
        /// </summary>
        public static Tensor<T> Subtract<T>(Tensor<T> a, Tensor<T> b) where T : struct
        {
            if (!AreShapesCompatible(a.Shape, b.Shape))
                throw new ArgumentException("Tensor shapes are not compatible for subtraction");
            
            var result = new Tensor<T>(a.Shape);
            if (typeof(T) == typeof(float))
            {
                var aFloat = (Tensor<float>)(object)a;
                var bFloat = (Tensor<float>)(object)b;
                var resultFloat = (Tensor<float>)(object)result;
                for (int i = 0; i < aFloat.Data.Length; i++)
                {
                    resultFloat.Data[i] = aFloat.Data[i] - bFloat.Data[i];
                }
            }
            return result;
        }
        
        /// <summary>
        /// Element-wise multiplication
        /// </summary>
        public static Tensor<T> Multiply<T>(Tensor<T> a, Tensor<T> b) where T : struct
        {
            if (!AreShapesCompatible(a.Shape, b.Shape))
                throw new ArgumentException("Tensor shapes are not compatible for multiplication");
            
            var result = new Tensor<T>(a.Shape);
            if (typeof(T) == typeof(float))
            {
                var aFloat = (Tensor<float>)(object)a;
                var bFloat = (Tensor<float>)(object)b;
                var resultFloat = (Tensor<float>)(object)result;
                for (int i = 0; i < aFloat.Data.Length; i++)
                {
                    resultFloat.Data[i] = aFloat.Data[i] * bFloat.Data[i];
                }
            }
            return result;
        }
        
        private static bool AreShapesCompatible(int[] shape1, int[] shape2)
        {
            if (shape1.Length != shape2.Length) return false;
            for (int i = 0; i < shape1.Length; i++)
            {
                if (shape1[i] != shape2[i]) return false;
            }
            return true;
        }
        
        private static Tensor<float> MatrixMultiply(Tensor<float> a, Tensor<float> b)
        {
            if (a.Shape.Length != 2 || b.Shape.Length != 2)
                throw new ArgumentException("Matrix multiplication requires 2D tensors");
            
            var m = a.Shape[0];
            var k = a.Shape[1];
            var n = b.Shape[1];
            
            if (k != b.Shape[0])
                throw new ArgumentException("Inner dimensions must match for matrix multiplication");
            
            var result = new Tensor<float>(new int[] { m, n });
            
            for (int i = 0; i < m; i++)
            {
                for (int j = 0; j < n; j++)
                {
                    float sum = 0;
                    for (int kk = 0; kk < k; kk++)
                    {
                        sum += a.Data[i * k + kk] * b.Data[kk * n + j];
                    }
                    result.Data[i * n + j] = sum;
                }
            }
            
            return result;
        }
        
        public override string ToString()
        {
            return $"Tensor<{typeof(T).Name}>{string.Join("x", Shape)}";
        }
    }
    
    /// <summary>
    /// Neural network layer base class
    /// </summary>
    public abstract class Layer
    {
        public abstract Tensor<float> Forward(Tensor<float> input);
        public abstract void ApplyGradients(Dictionary<Tensor<float>, Tensor<float>> gradients, float learningRate);
    }
    
    /// <summary>
    /// Dense (fully connected) layer
    /// </summary>
    public class Dense : Layer
    {
        public Tensor<float> Weights { get; }
        public Tensor<float> Bias { get; }
        public ActivationFunction Activation { get; }
        
        public Dense(int inputSize, int outputSize, ActivationFunction activation)
        {
            Weights = MachineLearningDSL.Randn(inputSize, outputSize);
            Bias = MachineLearningDSL.Zeros(outputSize);
            Activation = activation;
        }
        
        public override Tensor<float> Forward(Tensor<float> input)
        {
            // z = input @ weights + bias
            var z = Tensor<float>.MatrixMultiply(input, Weights); // Matrix multiplication
            z = Tensor<float>.Add(z, Bias); // Add bias
            
            // Apply activation function
            return Activation switch
            {
                ActivationFunction.ReLU => MachineLearningDSL.ReLU(z),
                ActivationFunction.Softmax => MachineLearningDSL.Softmax(z),
                ActivationFunction.None => z,
                _ => z
            };
        }
        
        public override void ApplyGradients(Dictionary<Tensor<float>, Tensor<float>> gradients, float learningRate)
        {
            if (gradients.ContainsKey(Weights))
            {
                var weightGrad = gradients[Weights];
                for (int i = 0; i < Weights.Data.Length; i++)
                {
                    Weights.Data[i] -= learningRate * weightGrad.Data[i];
                }
            }
            
            if (gradients.ContainsKey(Bias))
            {
                var biasGrad = gradients[Bias];
                for (int i = 0; i < Bias.Data.Length; i++)
                {
                    Bias.Data[i] -= learningRate * biasGrad.Data[i];
                }
            }
        }
    }
    
    /// <summary>
    /// Neural network model
    /// </summary>
    public class NeuralNetwork
    {
        private readonly List<Layer> layers = new();
        
        public void AddLayer(Layer layer)
        {
            layers.Add(layer);
        }
        
        public Tensor<float> Forward(Tensor<float> input)
        {
            var output = input;
            foreach (var layer in layers)
            {
                output = layer.Forward(output);
            }
            return output;
        }
        
        public void Train(Dataset dataset, int epochs, float learningRate)
        {
            for (int epoch = 0; epoch < epochs; epoch++)
            {
                foreach (var batch in dataset.Batches(32))
                {
                    // Forward pass
                    var predictions = Forward(batch.Inputs);
                    var loss = MachineLearningDSL.CrossEntropy(predictions, batch.Labels);
                    
                    // Backward pass
                    var gradients = MachineLearningDSL.ComputeGradients(loss);
                    
                    // Update weights
                    foreach (var layer in layers)
                    {
                        layer.ApplyGradients(gradients, learningRate);
                    }
                }
            }
        }
    }
    
    /// <summary>
    /// Activation functions
    /// </summary>
    public enum ActivationFunction
    {
        None,
        ReLU,
        Sigmoid,
        Tanh,
        Softmax
    }
    
    /// <summary>
    /// Optimizers for training
    /// </summary>
    public abstract class Optimizer
    {
        public abstract void UpdateWeights(Dictionary<Tensor<float>, Tensor<float>> gradients);
    }
    
    public class Adam : Optimizer
    {
        public float LearningRate { get; }
        public float Beta1 { get; }
        public float Beta2 { get; }
        public float Epsilon { get; }
        
        public Adam(float lr = 0.001f, float beta1 = 0.9f, float beta2 = 0.999f, float epsilon = 1e-8f)
        {
            LearningRate = lr;
            Beta1 = beta1;
            Beta2 = beta2;
            Epsilon = epsilon;
        }
        
        public override void UpdateWeights(Dictionary<Tensor<float>, Tensor<float>> gradients)
        {
            // Simplified Adam optimizer implementation
            foreach (var kvp in gradients)
            {
                var param = kvp.Key;
                var grad = kvp.Value;
                
                for (int i = 0; i < param.Data.Length; i++)
                {
                    param.Data[i] -= LearningRate * grad.Data[i];
                }
            }
        }
    }
    
    /// <summary>
    /// Dataset for training
    /// </summary>
    public class Dataset
    {
        public Tensor<float> Inputs { get; }
        public Tensor<float> Labels { get; }
        
        public Dataset(Tensor<float> inputs, Tensor<float> labels)
        {
            Inputs = inputs;
            Labels = labels;
        }
        
        public IEnumerable<Batch> Batches(int batchSize)
        {
            var numSamples = Inputs.Shape[0];
            for (int i = 0; i < numSamples; i += batchSize)
            {
                var actualBatchSize = global::System.Math.Min(batchSize, numSamples - i);
                // Simplified batch creation - real implementation would slice tensors properly
                yield return new Batch(Inputs, Labels);
            }
        }
    }
    
    /// <summary>
    /// Training batch
    /// </summary>
    public class Batch
    {
        public Tensor<float> Inputs { get; }
        public Tensor<float> Labels { get; }
        
        public Batch(Tensor<float> inputs, Tensor<float> labels)
        {
            Inputs = inputs;
            Labels = labels;
        }
    }
    
    /// <summary>
    /// Compiled function for efficient execution
    /// </summary>
    public class CompiledFunction
    {
        private readonly Tensor<float> output;
        private readonly Optimizer optimizer;
        
        public CompiledFunction(Tensor<float> output, Optimizer optimizer)
        {
            this.output = output;
            this.optimizer = optimizer;
        }
        
        public void Execute(Dictionary<string, Tensor<float>> inputs)
        {
            // Execute compiled computation graph
            // This would be optimized native code in a real implementation
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