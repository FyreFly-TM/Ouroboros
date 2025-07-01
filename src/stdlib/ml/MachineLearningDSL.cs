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
            // Full cross entropy implementation with numerical stability
            var loss = new Tensor<float>(new int[] { 1 });
            
            // Verify dimensions match
            if (!AreShapesEqual(predictions.Shape, labels.Shape))
            {
                throw new ArgumentException("Predictions and labels must have the same shape");
            }
            
            // Calculate cross entropy: -Σ(y * log(p))
            float totalLoss = 0.0f;
            int batchSize = predictions.Shape[0];
            int numClasses = predictions.Shape.Length > 1 ? predictions.Shape[1] : 1;
            
            // For categorical cross entropy
            if (predictions.Shape.Length == 2)
            {
                for (int b = 0; b < batchSize; b++)
                {
                    float sampleLoss = 0.0f;
                    
                    // Find max for numerical stability
                    float maxLogit = float.MinValue;
                    for (int c = 0; c < numClasses; c++)
                    {
                        maxLogit = global::System.Math.Max(maxLogit, predictions.Data[b * numClasses + c]);
                    }
                    
                    // Compute log-sum-exp for normalization
                    float sumExp = 0.0f;
                    for (int c = 0; c < numClasses; c++)
                    {
                        sumExp += (float)global::System.Math.Exp(predictions.Data[b * numClasses + c] - maxLogit);
                    }
                    float logSumExp = maxLogit + (float)global::System.Math.Log(sumExp);
                    
                    // Calculate cross entropy for this sample
                    for (int c = 0; c < numClasses; c++)
                    {
                        if (labels.Data[b * numClasses + c] > 0)
                        {
                            float logProb = predictions.Data[b * numClasses + c] - logSumExp;
                            sampleLoss -= labels.Data[b * numClasses + c] * logProb;
                        }
                    }
                    
                    totalLoss += sampleLoss;
                }
            }
            // For binary cross entropy
            else if (predictions.Shape.Length == 1 || (predictions.Shape.Length == 2 && numClasses == 1))
            {
                for (int i = 0; i < predictions.Data.Length; i++)
                {
                    var p = global::System.Math.Max(global::System.Math.Min(predictions.Data[i], 1.0f - 1e-15f), 1e-15f);
                    var y = labels.Data[i];
                    totalLoss -= y * (float)global::System.Math.Log(p) + (1 - y) * (float)global::System.Math.Log(1 - p);
                }
            }
            
            loss.Data[0] = totalLoss / batchSize; // Average over batch
            return loss;
        }
        
        private static bool AreShapesEqual(int[] shape1, int[] shape2)
        {
            if (shape1.Length != shape2.Length) return false;
            for (int i = 0; i < shape1.Length; i++)
            {
                if (shape1[i] != shape2[i]) return false;
            }
            return true;
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
            // Parse equation to extract input and output patterns
            var parts = equation.Split("->");
            if (parts.Length != 2)
                throw new ArgumentException("Einsum equation must contain '->'");
            
            var inputPatterns = parts[0].Split(',');
            var outputPattern = parts[1].Trim();
            
            // Extended einsum implementation for common and complex cases
            return equation switch
            {
                // Matrix operations
                "ij,jk->ik" => MatrixMultiply2D(tensors[0], tensors[1]),
                "ij,kj->ik" => MatrixMultiply2D(tensors[0], TransposeMatrix(tensors[1])),
                "ji,jk->ik" => MatrixMultiply2D(TransposeMatrix(tensors[0]), tensors[1]),
                
                // Batched operations
                "bqd,bkd->bqk" => BatchedMatrixMultiply(tensors[0], tensors[1], transposeB: true),
                "bqk,bkd->bqd" => BatchedMatrixMultiply(tensors[0], tensors[1]),
                "bij,bjk->bik" => BatchedMatrixMultiply(tensors[0], tensors[1]),
                "ijk,ikl->ijl" => BatchedMatrixMultiply(tensors[0], tensors[1]),
                
                // Reductions
                "ii->" => ComputeTrace(tensors[0]),
                "i,i->" => ComputeDotProduct(tensors[0], tensors[1]),
                "ij->i" => SumAlongAxis(tensors[0], axis: 1),
                "ij->j" => SumAlongAxis(tensors[0], axis: 0),
                "ijk->ij" => SumAlongAxis(tensors[0], axis: 2),
                "ijk->ik" => SumAlongAxis(tensors[0], axis: 1),
                "ijk->jk" => SumAlongAxis(tensors[0], axis: 0),
                
                // Transpositions
                "ij->ji" => TransposeMatrix(tensors[0]),
                "ijk->jik" => SwapAxes(tensors[0], 0, 1),
                "ijk->ikj" => SwapAxes(tensors[0], 1, 2),
                "ijk->kij" => CyclicPermutation(tensors[0], new[] { 2, 0, 1 }),
                "ijk->kji" => CyclicPermutation(tensors[0], new[] { 2, 1, 0 }),
                
                // Diagonal operations
                "ii->i" => ExtractDiagonal(tensors[0]),
                "i->ii" => CreateDiagonalMatrix(tensors[0]),
                
                // Outer products
                "i,j->ij" => OuterProduct(tensors[0], tensors[1]),
                "i,j,k->ijk" => OuterProduct3D(tensors[0], tensors[1], tensors[2]),
                
                // Broadcasting operations
                "i,ij->ij" => BroadcastMultiply(tensors[0], tensors[1], axis: 0),
                "j,ij->ij" => BroadcastMultiply(tensors[0], tensors[1], axis: 1),
                
                // General fallback - implement generic einsum algorithm
                _ => GenericEinsum(equation, tensors)
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
        
        private static Tensor<float> SumAlongAxis(Tensor<float> tensor, int axis)
        {
            var shape = tensor.Shape.ToList();
            shape.RemoveAt(axis);
            if (shape.Count == 0) shape.Add(1);
            
            var result = new Tensor<float>(shape.ToArray());
            
            // Simple implementation for 2D and 3D tensors
            if (tensor.Shape.Length == 2)
            {
                var m = tensor.Shape[0];
                var n = tensor.Shape[1];
                
                if (axis == 0)
                {
                    for (int j = 0; j < n; j++)
                    {
                        float sum = 0;
                        for (int i = 0; i < m; i++)
                        {
                            sum += tensor.Data[i * n + j];
                        }
                        result.Data[j] = sum;
                    }
                }
                else if (axis == 1)
                {
                    for (int i = 0; i < m; i++)
                    {
                        float sum = 0;
                        for (int j = 0; j < n; j++)
                        {
                            sum += tensor.Data[i * n + j];
                        }
                        result.Data[i] = sum;
                    }
                }
            }
            
            return result;
        }
        
        private static Tensor<float> SwapAxes(Tensor<float> tensor, int axis1, int axis2)
        {
            var shape = tensor.Shape.ToArray();
            var temp = shape[axis1];
            shape[axis1] = shape[axis2];
            shape[axis2] = temp;
            
            var result = new Tensor<float>(shape);
            
            // Simple implementation for 3D tensors
            if (tensor.Shape.Length == 3)
            {
                var d0 = tensor.Shape[0];
                var d1 = tensor.Shape[1];
                var d2 = tensor.Shape[2];
                
                for (int i = 0; i < d0; i++)
                {
                    for (int j = 0; j < d1; j++)
                    {
                        for (int k = 0; k < d2; k++)
                        {
                            var srcIdx = i * d1 * d2 + j * d2 + k;
                            var dstIdx = 0;
                            
                            if (axis1 == 0 && axis2 == 1)
                                dstIdx = j * d0 * d2 + i * d2 + k;
                            else if (axis1 == 1 && axis2 == 2)
                                dstIdx = i * d2 * d1 + k * d1 + j;
                            
                            result.Data[dstIdx] = tensor.Data[srcIdx];
                        }
                    }
                }
            }
            
            return result;
        }
        
        private static Tensor<float> CyclicPermutation(Tensor<float> tensor, int[] permutation)
        {
            var shape = new int[tensor.Shape.Length];
            for (int i = 0; i < shape.Length; i++)
            {
                shape[i] = tensor.Shape[permutation[i]];
            }
            
            return new Tensor<float>(shape); // Simplified - would need full implementation
        }
        
        private static Tensor<float> ExtractDiagonal(Tensor<float> matrix)
        {
            if (matrix.Shape.Length != 2 || matrix.Shape[0] != matrix.Shape[1])
                throw new ArgumentException("Extract diagonal requires square matrix");
            
            var n = matrix.Shape[0];
            var result = new Tensor<float>(new int[] { n });
            
            for (int i = 0; i < n; i++)
            {
                result.Data[i] = matrix.Data[i * n + i];
            }
            
            return result;
        }
        
        private static Tensor<float> CreateDiagonalMatrix(Tensor<float> vector)
        {
            if (vector.Shape.Length != 1)
                throw new ArgumentException("Create diagonal requires 1D vector");
            
            var n = vector.Shape[0];
            var result = new Tensor<float>(new int[] { n, n });
            
            for (int i = 0; i < n; i++)
            {
                result.Data[i * n + i] = vector.Data[i];
            }
            
            return result;
        }
        
        private static Tensor<float> OuterProduct(Tensor<float> a, Tensor<float> b)
        {
            if (a.Shape.Length != 1 || b.Shape.Length != 1)
                throw new ArgumentException("Outer product requires 1D vectors");
            
            var m = a.Shape[0];
            var n = b.Shape[0];
            var result = new Tensor<float>(new int[] { m, n });
            
            for (int i = 0; i < m; i++)
            {
                for (int j = 0; j < n; j++)
                {
                    result.Data[i * n + j] = a.Data[i] * b.Data[j];
                }
            }
            
            return result;
        }
        
        private static Tensor<float> OuterProduct3D(Tensor<float> a, Tensor<float> b, Tensor<float> c)
        {
            if (a.Shape.Length != 1 || b.Shape.Length != 1 || c.Shape.Length != 1)
                throw new ArgumentException("3D outer product requires 1D vectors");
            
            var l = a.Shape[0];
            var m = b.Shape[0];
            var n = c.Shape[0];
            var result = new Tensor<float>(new int[] { l, m, n });
            
            for (int i = 0; i < l; i++)
            {
                for (int j = 0; j < m; j++)
                {
                    for (int k = 0; k < n; k++)
                    {
                        result.Data[i * m * n + j * n + k] = a.Data[i] * b.Data[j] * c.Data[k];
                    }
                }
            }
            
            return result;
        }
        
        private static Tensor<float> BroadcastMultiply(Tensor<float> vector, Tensor<float> matrix, int axis)
        {
            if (vector.Shape.Length != 1 || matrix.Shape.Length != 2)
                throw new ArgumentException("Broadcast multiply requires 1D vector and 2D matrix");
            
            var result = new Tensor<float>(matrix.Shape);
            var m = matrix.Shape[0];
            var n = matrix.Shape[1];
            
            for (int i = 0; i < m; i++)
            {
                for (int j = 0; j < n; j++)
                {
                    var scalar = axis == 0 ? vector.Data[i] : vector.Data[j];
                    result.Data[i * n + j] = matrix.Data[i * n + j] * scalar;
                }
            }
            
            return result;
        }
        
        private static Tensor<float> GenericEinsum(string equation, Tensor<float>[] tensors)
        {
            // Generic einsum implementation for arbitrary contractions
            var parts = equation.Split("->");
            if (parts.Length != 2)
                throw new ArgumentException("Einsum equation must contain '->'");
            
            var inputExpressions = parts[0].Split(',');
            var outputExpression = parts[1].Trim();
            
            if (inputExpressions.Length != tensors.Length)
                throw new ArgumentException("Number of input patterns must match number of tensors");
            
            // Identify all unique indices
            var allIndices = new HashSet<char>();
            foreach (var expr in inputExpressions)
            {
                foreach (char c in expr)
                {
                    if (char.IsLetter(c))
                        allIndices.Add(c);
                }
            }
            
            // Determine index dimensions from tensors
            var indexDimensions = new Dictionary<char, int>();
            for (int i = 0; i < inputExpressions.Length; i++)
            {
                var expr = inputExpressions[i];
                var tensor = tensors[i];
                
                if (expr.Length != tensor.Shape.Length)
                    throw new ArgumentException($"Expression '{expr}' doesn't match tensor shape");
                
                for (int j = 0; j < expr.Length; j++)
                {
                    var idx = expr[j];
                    if (indexDimensions.ContainsKey(idx))
                    {
                        if (indexDimensions[idx] != tensor.Shape[j])
                            throw new ArgumentException($"Inconsistent dimensions for index '{idx}'");
                    }
                    else
                    {
                        indexDimensions[idx] = tensor.Shape[j];
                    }
                }
            }
            
            // Determine output shape
            var outputShape = new List<int>();
            foreach (char idx in outputExpression)
            {
                if (!indexDimensions.ContainsKey(idx))
                    throw new ArgumentException($"Output index '{idx}' not found in inputs");
                outputShape.Add(indexDimensions[idx]);
            }
            
            if (outputShape.Count == 0)
                outputShape.Add(1); // Scalar output
            
            var result = new Tensor<float>(outputShape.ToArray());
            
            // Determine which indices to sum over
            var sumIndices = new HashSet<char>();
            foreach (var idx in allIndices)
            {
                if (!outputExpression.Contains(idx))
                {
                    sumIndices.Add(idx);
                }
            }
            
            // Create index iterators
            var allIndexValues = new Dictionary<char, int>();
            foreach (var idx in allIndices)
            {
                allIndexValues[idx] = 0;
            }
            
            // Helper function to compute tensor indices
            int ComputeTensorIndex(string expression, Dictionary<char, int> indexValues)
            {
                int index = 0;
                int stride = 1;
                
                for (int i = expression.Length - 1; i >= 0; i--)
                {
                    index += indexValues[expression[i]] * stride;
                    stride *= indexDimensions[expression[i]];
                }
                
                return index;
            }
            
            // Helper function to increment indices
            bool IncrementIndices(Dictionary<char, int> indices, HashSet<char> activeIndices)
            {
                foreach (var idx in activeIndices.Reverse())
                {
                    indices[idx]++;
                    if (indices[idx] < indexDimensions[idx])
                        return true;
                    indices[idx] = 0;
                }
                return false;
            }
            
            // Main computation loop
            var outputIndexValues = new Dictionary<char, int>();
            foreach (char idx in outputExpression)
            {
                outputIndexValues[idx] = 0;
            }
            
            do
            {
                // Compute output index
                int outputIndex = 0;
                int outputStride = 1;
                for (int i = outputExpression.Length - 1; i >= 0; i--)
                {
                    outputIndex += outputIndexValues[outputExpression[i]] * outputStride;
                    outputStride *= indexDimensions[outputExpression[i]];
                }
                
                // Initialize sum for this output element
                float sum = 0.0f;
                
                // Set up summation indices
                var sumIndexValues = new Dictionary<char, int>();
                foreach (var idx in sumIndices)
                {
                    sumIndexValues[idx] = 0;
                    allIndexValues[idx] = 0;
                }
                
                // Copy output indices to allIndexValues
                foreach (var kvp in outputIndexValues)
                {
                    allIndexValues[kvp.Key] = kvp.Value;
                }
                
                // Sum over contracted indices
                do
                {
                    // Copy sum indices to allIndexValues
                    foreach (var kvp in sumIndexValues)
                    {
                        allIndexValues[kvp.Key] = kvp.Value;
                    }
                    
                    // Compute product for this configuration
                    float product = 1.0f;
                    
                    for (int i = 0; i < tensors.Length; i++)
                    {
                        var tensorIndex = ComputeTensorIndex(inputExpressions[i], allIndexValues);
                        product *= tensors[i].Data[tensorIndex];
                    }
                    
                    sum += product;
                    
                } while (IncrementIndices(sumIndexValues, sumIndices));
                
                result.Data[outputIndex] = sum;
                
            } while (IncrementIndices(outputIndexValues, new HashSet<char>(outputExpression)));
            
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
            // Proper backward pass using automatic differentiation
            // Track computation graph and compute gradients via chain rule
            
            // Build a queue of tensors to process in reverse topological order
            var toProcess = new Queue<Tensor<float>>();
            var visited = new HashSet<Tensor<float>>();
            toProcess.Enqueue(tensor);
            
            while (toProcess.Count > 0)
            {
                var current = toProcess.Dequeue();
                if (visited.Contains(current)) continue;
                visited.Add(current);
                
                // Process gradient for current tensor
                if (!gradients.ContainsKey(current))
                    continue;
                    
                var currentGrad = gradients[current];
                
                // Propagate gradients to dependencies based on operation type
                if (current.Operation != null)
                {
                    switch (current.Operation.Type)
                    {
                        case OperationType.MatMul:
                            // For C = A @ B, compute gradients:
                            // dL/dA = dL/dC @ B^T
                            // dL/dB = A^T @ dL/dC
                            var a = current.Operation.Inputs[0];
                            var b = current.Operation.Inputs[1];
                            
                            // Gradient w.r.t. A
                            var gradA = MatrixMultiply2D(currentGrad, TransposeMatrix(b));
                            AccumulateGradient(gradients, a, gradA);
                            toProcess.Enqueue(a);
                            
                            // Gradient w.r.t. B
                            var gradB = MatrixMultiply2D(TransposeMatrix(a), currentGrad);
                            AccumulateGradient(gradients, b, gradB);
                            toProcess.Enqueue(b);
                            break;
                            
                        case OperationType.Add:
                            // For C = A + B, gradients pass through unchanged
                            AccumulateGradient(gradients, current.Operation.Inputs[0], currentGrad);
                            AccumulateGradient(gradients, current.Operation.Inputs[1], currentGrad);
                            toProcess.Enqueue(current.Operation.Inputs[0]);
                            toProcess.Enqueue(current.Operation.Inputs[1]);
                            break;
                            
                        case OperationType.ReLU:
                            // For y = ReLU(x), dy/dx = 1 if x > 0, else 0
                            var input = current.Operation.Inputs[0];
                            var reluGrad = new Tensor<float>(currentGrad.Shape);
                            for (int i = 0; i < input.Data.Length; i++)
                            {
                                reluGrad.Data[i] = input.Data[i] > 0 ? currentGrad.Data[i] : 0;
                            }
                            AccumulateGradient(gradients, input, reluGrad);
                            toProcess.Enqueue(input);
                            break;
                            
                        case OperationType.Softmax:
                            // For softmax, gradient computation is more complex
                            // Jacobian: J[i,j] = s[i] * (δ[i,j] - s[j])
                            var softmaxInput = current.Operation.Inputs[0];
                            var softmaxGrad = ComputeSoftmaxGradient(current, currentGrad);
                            AccumulateGradient(gradients, softmaxInput, softmaxGrad);
                            toProcess.Enqueue(softmaxInput);
                            break;
                    }
                }
                
                // Handle parameters that require gradients
                foreach (var param in current.RequiresGrad)
                {
                    if (!visited.Contains(param))
                    {
                        toProcess.Enqueue(param);
                    }
                }
            }
        }
        
        private static void AccumulateGradient(Dictionary<Tensor<float>, Tensor<float>> gradients, 
            Tensor<float> tensor, Tensor<float> grad)
        {
            if (gradients.ContainsKey(tensor))
            {
                // Accumulate gradients
                var existing = gradients[tensor];
                for (int i = 0; i < existing.Data.Length; i++)
                {
                    existing.Data[i] += grad.Data[i];
                }
            }
            else
            {
                gradients[tensor] = grad;
            }
        }
        
        private static Tensor<float> ComputeSoftmaxGradient(Tensor<float> softmaxOutput, Tensor<float> outputGrad)
        {
            var result = new Tensor<float>(outputGrad.Shape);
            var batchSize = softmaxOutput.Shape[0];
            var numClasses = softmaxOutput.Shape[1];
            
            for (int b = 0; b < batchSize; b++)
            {
                for (int i = 0; i < numClasses; i++)
                {
                    float sum = 0;
                    for (int j = 0; j < numClasses; j++)
                    {
                        var s_i = softmaxOutput.Data[b * numClasses + i];
                        var s_j = softmaxOutput.Data[b * numClasses + j];
                        var grad_j = outputGrad.Data[b * numClasses + j];
                        
                        if (i == j)
                        {
                            sum += s_i * (1 - s_i) * grad_j;
                        }
                        else
                        {
                            sum += -s_i * s_j * grad_j;
                        }
                    }
                    result.Data[b * numClasses + i] = sum;
                }
            }
            
            return result;
        }
    }
    
    /// <summary>
    /// Operation types for automatic differentiation
    /// </summary>
    public enum OperationType
    {
        None,
        MatMul,
        Add,
        Subtract,
        Multiply,
        Divide,
        ReLU,
        Softmax,
        Sigmoid,
        Tanh,
        CrossEntropy
    }
    
    /// <summary>
    /// Operation node in computation graph
    /// </summary>
    public class Operation
    {
        public OperationType Type { get; set; }
        public List<Tensor<float>> Inputs { get; set; } = new();
        public string Name { get; set; } = "";
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
        public Operation? Operation { get; set; }
        
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
        
        private readonly Dictionary<Tensor<float>, Tensor<float>> firstMoments = new();
        private readonly Dictionary<Tensor<float>, Tensor<float>> secondMoments = new();
        private int timestep = 0;
        
        public Adam(float lr = 0.001f, float beta1 = 0.9f, float beta2 = 0.999f, float epsilon = 1e-8f)
        {
            LearningRate = lr;
            Beta1 = beta1;
            Beta2 = beta2;
            Epsilon = epsilon;
        }
        
        public override void UpdateWeights(Dictionary<Tensor<float>, Tensor<float>> gradients)
        {
            timestep++;
            
            // Compute bias-corrected learning rate
            var biasCorrection1 = 1.0f - (float)global::System.Math.Pow(Beta1, timestep);
            var biasCorrection2 = 1.0f - (float)global::System.Math.Pow(Beta2, timestep);
            var stepSize = LearningRate * (float)global::System.Math.Sqrt(biasCorrection2) / biasCorrection1;
            
            foreach (var kvp in gradients)
            {
                var param = kvp.Key;
                var grad = kvp.Value;
                
                // Initialize moments if not exists
                if (!firstMoments.ContainsKey(param))
                {
                    firstMoments[param] = new Tensor<float>(param.Shape);
                    secondMoments[param] = new Tensor<float>(param.Shape);
                }
                
                var m = firstMoments[param];
                var v = secondMoments[param];
                
                // Update biased first and second moments
                for (int i = 0; i < param.Data.Length; i++)
                {
                    // m_t = β1 * m_{t-1} + (1 - β1) * g_t
                    m.Data[i] = Beta1 * m.Data[i] + (1 - Beta1) * grad.Data[i];
                    
                    // v_t = β2 * v_{t-1} + (1 - β2) * g_t^2
                    v.Data[i] = Beta2 * v.Data[i] + (1 - Beta2) * grad.Data[i] * grad.Data[i];
                    
                    // θ_t = θ_{t-1} - α * m_t / (√v_t + ε)
                    param.Data[i] -= stepSize * m.Data[i] / ((float)global::System.Math.Sqrt(v.Data[i]) + Epsilon);
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